// <copyright file="RevaluationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class RevaluationService : IRevaluationService
{
    private readonly GlDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public RevaluationService(GlDbContext context, IServiceProvider serviceProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<RevaluationResult> RevalueAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        DateTimeOffset revaluationDate,
        string revaluationReason,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewRevaluationAsync(companyId, fiscalPeriodId, revaluationDate, cancellationToken);

        if (!preview.Lines.Any())
        {
            return new RevaluationResult(null!, 0, 0);
        }

        var batchNumber = await GenerateBatchNumberAsync(companyId, cancellationToken);
        var batch = new JournalBatch(companyId, batchNumber, $"Revaluation: {revaluationReason}", revaluationDate, fiscalPeriodId);

        foreach (var linePreview in preview.Lines)
        {
            var account = await GetAccountAsync(linePreview.AccountId);
            if (account == null)
                continue;

            var gainLoss = linePreview.GainLoss;

            if (gainLoss > 0)
            {
                // Gain - credit to gain/loss account, debit to original account
                batch.AddLine(linePreview.AccountId, gainLoss, 0, $"Revaluation gain: {revaluationReason}");

                // Add corresponding credit to gain/loss account
                var gainLossAccount = await GetGainLossAccountAsync(companyId, true, cancellationToken);
                if (gainLossAccount != null)
                {
                    batch.AddLine(gainLossAccount.Id, 0, gainLoss, $"Revaluation gain offset: {revaluationReason}");
                }
            }
            else if (gainLoss < 0)
            {
                // Loss - debit to gain/loss account, credit to original account
                batch.AddLine(linePreview.AccountId, 0, Math.Abs(gainLoss), $"Revaluation loss: {revaluationReason}");
                var gainLossAccount = await GetGainLossAccountAsync(companyId, false, cancellationToken);
                if (gainLossAccount != null)
                {
                    batch.AddLine(gainLossAccount.Id, Math.Abs(gainLoss), 0, $"Revaluation loss offset: {revaluationReason}");
                }
            }
        }

        if (!batch.IsBalanced())
        {
            throw new InvalidOperationException("Revaluation batch is not balanced.");
        }

        batch.Release();
        batch.Post();

        _context.JournalBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        return new RevaluationResult(batch, preview.Lines.Count, preview.EstimatedGainLoss);
    }

    public async Task<RevaluationPreviewDto> PreviewRevaluationAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        DateTimeOffset revaluationDate,
        CancellationToken cancellationToken = default)
    {
        var postedLines = await _context.JournalEntryLines
            .Where(l => l.JournalBatch != null
                && l.JournalBatch.CompanyId == companyId
                && l.JournalBatch.FiscalPeriodId == fiscalPeriodId
                && l.JournalBatch.Status == JournalBatchStatus.Posted
                && l.CurrencyId.HasValue
                && l.CurrencyId != Guid.Empty)
            .Include(l => l.JournalBatch)
            .Include(l => l.Account)
            .ToListAsync(cancellationToken);

        var linesToRevalue = new List<RevaluationLinePreview>();
        decimal totalGainLoss = 0;

        foreach (var line in postedLines)
        {
            var currentRate = await GetExchangeRateAsync(companyId, line.CurrencyId!.Value, revaluationDate, cancellationToken);
            if (currentRate <= 0)
                continue;

            var originalRate = line.ExchangeRate;
            var revaluedDebit = line.Debit * currentRate / originalRate;
            var revaluedCredit = line.Credit * currentRate / originalRate;

            var gainLoss = (revaluedDebit - revaluedCredit) - (line.Debit - line.Credit);

            if (Math.Abs(gainLoss) > 0.005m)
            {
                linesToRevalue.Add(new RevaluationLinePreview(
                    line.AccountId,
                    line.Account?.AccountNumber ?? "Unknown",
                    line.Debit,
                    line.Credit,
                    revaluedDebit,
                    revaluedCredit,
                    gainLoss));

                totalGainLoss += gainLoss;
            }
        }

        return new RevaluationPreviewDto(
            linesToRevalue.Count,
            totalGainLoss,
            linesToRevalue);
    }

    private async Task<decimal> GetExchangeRateAsync(Guid companyId, Guid currencyId, DateTimeOffset date, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var platformContext = scope.ServiceProvider.GetRequiredService<ERP.Modules.Platform.Infrastructure.PlatformDbContext>();

        var rate = await platformContext.ExchangeRates
            .Where(er => er.CompanyId == companyId
                && er.ToCurrency == currencyId.ToString()
                && er.EffectiveDate <= date)
            .OrderByDescending(er => er.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        return rate?.Rate ?? 0;
    }

    private async Task<Account?> GetAccountAsync(Guid accountId)
    {
        using var scope = _serviceProvider.CreateScope();
        var platformContext = scope.ServiceProvider.GetRequiredService<ERP.Modules.Platform.Infrastructure.PlatformDbContext>();
        return await platformContext.Accounts.FindAsync(accountId);
    }

    private async Task<Account?> GetGainLossAccountAsync(Guid companyId, bool isGain, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var platformContext = scope.ServiceProvider.GetRequiredService<ERP.Modules.Platform.Infrastructure.PlatformDbContext>();

        // In a real implementation, this would be configurable per company
        var accountNumber = isGain ? "4990" : "5990"; // Example: Foreign Exchange Gain/Loss
        return await platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountNumber == accountNumber, cancellationToken);
    }

    private async Task<string> GenerateBatchNumberAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var count = await _context.JournalBatches
            .CountAsync(b => b.CompanyId == companyId, cancellationToken);
        return $"REV-{count + 1:D4}";
    }
}