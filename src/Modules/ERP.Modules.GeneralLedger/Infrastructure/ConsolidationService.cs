// <copyright file="ConsolidationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class ConsolidationService : IConsolidationService
{
    private readonly GlDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public ConsolidationService(GlDbContext context, IServiceProvider serviceProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<ConsolidationRun> CreateConsolidationRunAsync(
        Guid parentCompanyId,
        DateTimeOffset consolidationDate,
        int fiscalYear,
        int fiscalPeriod,
        string description,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var platformContext = scope.ServiceProvider.GetRequiredService<ERP.Modules.Platform.Infrastructure.PlatformDbContext>();

        var fiscalPeriodId = await platformContext.FiscalPeriods
            .Where(fp => fp.FiscalYearId == platformContext.FiscalYears
                .Where(fy => fy.Year == fiscalYear && fy.CompanyId == parentCompanyId)
                .Select(fy => fy.Id)
                .FirstOrDefault()
                && fp.PeriodNumber == fiscalPeriod
                && fp.CompanyId == parentCompanyId)
            .Select(fp => fp.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (fiscalPeriodId == Guid.Empty)
        {
            throw new InvalidOperationException($"Fiscal period {fiscalYear}-{fiscalPeriod} not found for company {parentCompanyId}.");
        }

        var run = new ConsolidationRun(parentCompanyId, fiscalYear, fiscalPeriod, description, consolidationDate);
        run.SetFiscalPeriodId(fiscalPeriodId);
        _context.ConsolidationRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task<ConsolidationRun> ExecuteConsolidationAsync(
        Guid consolidationRunId,
        CancellationToken cancellationToken = default)
    {
        var run = await _context.ConsolidationRuns
            .FirstOrDefaultAsync(r => r.Id == consolidationRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Consolidation run {consolidationRunId} not found.");

        run.StartProcessing();

        try
        {
            var childCompanies = new List<ERP.Modules.Platform.Domain.Entities.Company>();
            var mappings = await _context.IntercompanyMappings
                .Where(m => m.IsActive)
                .ToListAsync(cancellationToken);

            var companyIds = new[] { run.ParentCompanyId }.Concat(childCompanies.Select(c => c.Id)).ToList();

            var batches = await _context.JournalBatches
                .Where(b => companyIds.Contains(b.CompanyId)
                    && b.FiscalPeriodId == run.FiscalPeriodId
                    && b.Status == JournalBatchStatus.Posted)
                .Include(b => b.Lines)
                .ToListAsync(cancellationToken);

            var consolidationBatch = new JournalBatch(
                run.ParentCompanyId,
                $"CONS-{run.ConsolidationDate:yyyyMMdd}-{run.Id.ToString()[..8]}",
                $"Consolidation for {run.Description}",
                run.ConsolidationDate,
                run.FiscalPeriodId);

            var accountBalances = new Dictionary<(Guid CompanyId, string AccountNumber), (decimal Debit, decimal Credit)>();

            foreach (var batch in batches)
            {
                foreach (var line in batch.Lines)
                {
                    var accountNumber = await GetAccountNumberAsync(line.AccountId, cancellationToken) ?? string.Empty;
                    (Guid CompanyId, string AccountNumber) key = (batch.CompanyId, accountNumber);

                    if (!accountBalances.ContainsKey(key))
                        accountBalances[key] = (0, 0);

                    accountBalances[key] = (
                        accountBalances[key].Debit + line.Debit,
                        accountBalances[key].Credit + line.Credit);
                }
            }

            foreach (var mapping in mappings)
            {
                (Guid CompanyId, string AccountNumber) fromKey = (mapping.FromCompanyId, mapping.FromAccountNumber);
                (Guid CompanyId, string AccountNumber) toKey = (mapping.ToCompanyId, mapping.ToAccountNumber);

                if (accountBalances.ContainsKey(fromKey) && accountBalances.ContainsKey(toKey))
                {
                    var fromBal = accountBalances[fromKey];
                    var toBal = accountBalances[toKey];

                    var eliminationAmount = Math.Min(fromBal.Debit - fromBal.Credit, toBal.Debit - toBal.Credit);
                    eliminationAmount = Math.Max(0, eliminationAmount);

                    if (eliminationAmount > 0)
                    {
                        accountBalances[fromKey] = (fromBal.Debit - eliminationAmount, fromBal.Credit);
                        accountBalances[toKey] = (toBal.Debit, toBal.Credit + eliminationAmount);
                    }
                }
            }

            foreach (var (key, balance) in accountBalances)
            {
                var account = await GetAccountByNumberAsync(key.CompanyId, key.AccountNumber, cancellationToken);
                if (account != null)
                {
                    var netAmount = balance.Debit - balance.Credit;
                    if (netAmount > 0)
                        consolidationBatch.AddLine(account.Id, netAmount, 0);
                    else if (netAmount < 0)
                        consolidationBatch.AddLine(account.Id, 0, Math.Abs(netAmount));
                }
            }

            if (!consolidationBatch.IsBalanced())
            {
                throw new InvalidOperationException("Consolidation batch is not balanced after intercompany eliminations.");
            }

            consolidationBatch.Release();
            consolidationBatch.Post();

            _context.JournalBatches.Add(consolidationBatch);
            run.Complete();
            await _context.SaveChangesAsync(cancellationToken);

            return run;
        }
        catch (Exception ex)
        {
            run.Fail(ex.Message);
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ConsolidationRun?> GetConsolidationRunAsync(
        Guid consolidationRunId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidationRuns
            .FirstOrDefaultAsync(r => r.Id == consolidationRunId, cancellationToken);
    }

    public async Task<IReadOnlyList<ConsolidationRun>> GetConsolidationRunsAsync(
        Guid parentCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidationRuns
            .Where(r => r.ParentCompanyId == parentCompanyId)
            .OrderByDescending(r => r.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IntercompanyMapping> CreateIntercompanyMappingAsync(
        Guid fromCompanyId,
        Guid toCompanyId,
        string fromAccountNumber,
        string toAccountNumber,
        string description,
        CancellationToken cancellationToken = default)
    {
        var mapping = new IntercompanyMapping(fromCompanyId, toCompanyId, fromAccountNumber, toAccountNumber, description);
        _context.IntercompanyMappings.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken);
        return mapping;
    }

    public async Task<IntercompanyMapping> UpdateIntercompanyMappingAsync(
        Guid mappingId,
        string fromAccountNumber,
        string toAccountNumber,
        string description,
        CancellationToken cancellationToken = default)
    {
        var mapping = await _context.IntercompanyMappings
            .FirstOrDefaultAsync(m => m.Id == mappingId, cancellationToken)
            ?? throw new InvalidOperationException($"Intercompany mapping {mappingId} not found.");

        mapping.Update(fromAccountNumber, toAccountNumber, description);
        await _context.SaveChangesAsync(cancellationToken);
        return mapping;
    }

    public async Task<IReadOnlyList<IntercompanyMapping>> GetIntercompanyMappingsAsync(
        Guid? fromCompanyId = null,
        Guid? toCompanyId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.IntercompanyMappings.AsQueryable();

        if (fromCompanyId.HasValue)
            query = query.Where(m => m.FromCompanyId == fromCompanyId.Value);

        if (toCompanyId.HasValue)
            query = query.Where(m => m.ToCompanyId == toCompanyId.Value);

        if (isActive.HasValue)
            query = query.Where(m => m.IsActive == isActive.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task DeleteIntercompanyMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        var mapping = await _context.IntercompanyMappings
            .FirstOrDefaultAsync(m => m.Id == mappingId, cancellationToken)
            ?? throw new InvalidOperationException($"Intercompany mapping {mappingId} not found.");

        _context.IntercompanyMappings.Remove(mapping);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetAccountNumberAsync(Guid accountId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var platformContext = scope.ServiceProvider.GetRequiredService<ERP.Modules.Platform.Infrastructure.PlatformDbContext>();

        return await platformContext.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => a.AccountNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    private async Task<ERP.Modules.Platform.Domain.Entities.Account?> GetAccountByNumberAsync(
        Guid companyId,
        string accountNumber,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var platformContext = scope.ServiceProvider.GetRequiredService<ERP.Modules.Platform.Infrastructure.PlatformDbContext>();

        return await platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountNumber == accountNumber, cancellationToken);
    }
}