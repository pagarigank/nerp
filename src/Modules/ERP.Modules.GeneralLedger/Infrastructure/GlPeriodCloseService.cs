// <copyright file="GlPeriodCloseService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public interface IGlPeriodCloseService
{
    Task<YearEndCloseRun> CloseYearEndAsync(
        Guid companyId,
        Guid fiscalYearId,
        Guid retainedEarningsAccountId,
        string closedBy,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PostingSuspenseItem>> GetSuspenseItemsAsync(
        Guid companyId,
        SuspenseStatus? status,
        CancellationToken cancellationToken);

    Task<Guid> ResolveSuspenseAsync(
        Guid suspenseItemId,
        Guid accountId,
        decimal debit,
        decimal credit,
        CancellationToken cancellationToken);

    Task DiscardSuspenseAsync(
        Guid suspenseItemId,
        string? note,
        CancellationToken cancellationToken);

    Task PostIntercompanyDueToFromAsync(
        Guid companyId,
        Guid fromCompanyId,
        Guid toCompanyId,
        decimal amount,
        Guid dueFromAccountId,
        Guid dueToAccountId,
        Guid offsetAccountId,
        string reason,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PrePostingEditLine>> GetPrePostingEditListAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PeriodEndChecklistItem>> GetPeriodEndChecklistAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken);

    Task<Guid> RollForwardBudgetAsync(
        Guid budgetId,
        Guid targetFiscalYearId,
        CancellationToken cancellationToken);

    Task<Guid> TransferBudgetAsync(
        Guid budgetId,
        Guid accountId,
        int fromPeriodNumber,
        int toPeriodNumber,
        decimal amount,
        string reason,
        CancellationToken cancellationToken);
}

public class GlPeriodCloseService : IGlPeriodCloseService
{
    private readonly GlDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public GlPeriodCloseService(GlDbContext context, IServiceProvider serviceProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<YearEndCloseRun> CloseYearEndAsync(
        Guid companyId,
        Guid fiscalYearId,
        Guid retainedEarningsAccountId,
        string closedBy,
        CancellationToken cancellationToken)
    {
        var run = new YearEndCloseRun(companyId, fiscalYearId, retainedEarningsAccountId, DateTimeOffset.UtcNow, closedBy, 0, 0, 0);
        _context.YearEndCloseRuns.Add(run);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

            var periodIds = await platform.FiscalPeriods
                .Where(fp => fp.FiscalYearId == fiscalYearId && fp.CompanyId == companyId && !fp.DeletedOn.HasValue)
                .Select(fp => fp.Id)
                .ToListAsync(cancellationToken);

            var postedLines = await _context.JournalEntryLines
                .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId
                    && periodIds.Contains(l.JournalBatch.FiscalPeriodId)
                    && l.JournalBatch.Status == JournalBatchStatus.Posted)
                .Include(l => l.JournalBatch)
                .Include(l => l.Account)
                .ToListAsync(cancellationToken);

            var revenueExpense = postedLines
                .Where(l => l.Account != null && (l.Account.AccountType == AccountType.Revenue || l.Account.AccountType == AccountType.Expense))
                .GroupBy(l => l.Account!)
                .Select(g => new
                {
                    g.Key,
                    Net = g.Key.AccountType == AccountType.Revenue
                        ? g.Sum(l => l.Credit - l.Debit)
                        : g.Sum(l => l.Debit - l.Credit),
                })
                .ToList();

            var totalRevenue = revenueExpense.Where(x => x.Key.AccountType == AccountType.Revenue).Sum(x => x.Net);
            var totalExpense = revenueExpense.Where(x => x.Key.AccountType == AccountType.Expense).Sum(x => x.Net);
            var retainedEarnings = Math.Round(totalRevenue - totalExpense, 2);

            var period = await platform.FiscalPeriods
                .Where(fp => fp.FiscalYearId == fiscalYearId && fp.CompanyId == companyId && !fp.DeletedOn.HasValue)
                .OrderBy(fp => fp.PeriodNumber)
                .FirstOrDefaultAsync(cancellationToken);
            var fiscalPeriodId = period?.Id ?? periodIds.FirstOrDefault();

            var batchNumber = $"YE-{DateTimeOffset.UtcNow:yyyy}-{await _context.YearEndCloseRuns.CountAsync(cancellationToken) + 1:D3}";
            var closingBatch = new JournalBatch(
                companyId,
                batchNumber,
                "Year-end close: income statement to retained earnings",
                DateTimeOffset.UtcNow,
                fiscalPeriodId);

            foreach (var line in revenueExpense)
            {
                if (Math.Abs(line.Net) < 0.005m)
                {
                    continue;
                }

                if (line.Key.AccountType == AccountType.Revenue)
                {
                    closingBatch.AddLine(line.Key.Id, line.Net, null, "Close revenue to retained earnings");
                }
                else
                {
                    closingBatch.AddLine(line.Key.Id, null, line.Net, "Close expense to retained earnings");
                }
            }

            if (Math.Abs(retainedEarnings) > 0.005m)
            {
                if (retainedEarnings > 0)
                {
                    closingBatch.AddLine(retainedEarningsAccountId, null, retainedEarnings, "Net income to retained earnings");
                }
                else
                {
                    closingBatch.AddLine(retainedEarningsAccountId, Math.Abs(retainedEarnings), null, "Net loss to retained earnings");
                }
            }

            if (!closingBatch.IsBalanced())
            {
                throw new InvalidOperationException("Year-end closing batch is not balanced.");
            }

            closingBatch.Release();
            closingBatch.Post();

            _context.JournalBatches.Add(closingBatch);
            run = new YearEndCloseRun(companyId, fiscalYearId, retainedEarningsAccountId, DateTimeOffset.UtcNow, closedBy, totalRevenue, totalExpense, retainedEarnings);
            _context.YearEndCloseRuns.Add(run);
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

    public async Task<IReadOnlyList<PostingSuspenseItem>> GetSuspenseItemsAsync(
        Guid companyId,
        SuspenseStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.PostingSuspenseItems.Where(s => s.CompanyId == companyId);
        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query.OrderByDescending(s => s.CreatedOn).ToListAsync(cancellationToken);
    }

    public async Task<Guid> ResolveSuspenseAsync(
        Guid suspenseItemId,
        Guid accountId,
        decimal debit,
        decimal credit,
        CancellationToken cancellationToken)
    {
        var item = await _context.PostingSuspenseItems.FirstOrDefaultAsync(s => s.Id == suspenseItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Suspense item {suspenseItemId} not found.");

        if (item.Status != SuspenseStatus.Pending)
        {
            throw new InvalidOperationException("Only pending suspense items can be resolved.");
        }

        using var scope = _serviceProvider.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var period = await platform.FiscalPeriods
            .Where(fp => fp.CompanyId == item.CompanyId && !fp.DeletedOn.HasValue)
            .OrderByDescending(fp => fp.PeriodNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var batchNumber = $"SUSP-{DateTimeOffset.UtcNow:yyyyMMdd}-{await _context.PostingSuspenseItems.CountAsync(cancellationToken) + 1:D4}";
        var batch = new JournalBatch(
            item.CompanyId,
            batchNumber,
            $"Suspense resolution: {item.SourceReference}",
            DateTimeOffset.UtcNow,
            period!.Id);
        batch.AddLine(accountId, debit > 0 ? debit : null, credit > 0 ? credit : null, item.SourceReference);
        batch.Release();
        batch.Post();

        _context.JournalBatches.Add(batch);
        item.Resolve(batch.Id);
        await _context.SaveChangesAsync(cancellationToken);
        return batch.Id;
    }

    public async Task DiscardSuspenseAsync(
        Guid suspenseItemId,
        string? note,
        CancellationToken cancellationToken)
    {
        var item = await _context.PostingSuspenseItems.FirstOrDefaultAsync(s => s.Id == suspenseItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Suspense item {suspenseItemId} not found.");

        item.Discard(note);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PostIntercompanyDueToFromAsync(
        Guid companyId,
        Guid fromCompanyId,
        Guid toCompanyId,
        decimal amount,
        Guid dueFromAccountId,
        Guid dueToAccountId,
        Guid offsetAccountId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        }

        using var scope = _serviceProvider.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var fromPeriod = await platform.FiscalPeriods.Where(fp => fp.CompanyId == fromCompanyId && !fp.DeletedOn.HasValue)
            .OrderByDescending(fp => fp.PeriodNumber).FirstOrDefaultAsync(cancellationToken);
        var toPeriod = await platform.FiscalPeriods.Where(fp => fp.CompanyId == toCompanyId && !fp.DeletedOn.HasValue)
            .OrderByDescending(fp => fp.PeriodNumber).FirstOrDefaultAsync(cancellationToken);

        if (fromPeriod == null || toPeriod == null)
        {
            throw new InvalidOperationException("Could not resolve a fiscal period for one of the companies.");
        }

        var fromBatch = new JournalBatch(
            fromCompanyId,
            $"IC-{DateTimeOffset.UtcNow:yyyyMMdd}-DF",
            $"Intercompany due-from: {reason}",
            DateTimeOffset.UtcNow,
            fromPeriod.Id);
        fromBatch.AddLine(dueFromAccountId, amount, null, reason);
        fromBatch.AddLine(offsetAccountId, null, amount, reason);
        fromBatch.Release();
        fromBatch.Post();
        _context.JournalBatches.Add(fromBatch);

        var toBatch = new JournalBatch(
            toCompanyId,
            $"IC-{DateTimeOffset.UtcNow:yyyyMMdd}-DT",
            $"Intercompany due-to: {reason}",
            DateTimeOffset.UtcNow,
            toPeriod.Id);
        toBatch.AddLine(offsetAccountId, amount, null, reason);
        toBatch.AddLine(dueToAccountId, null, amount, reason);
        toBatch.Release();
        toBatch.Post();
        _context.JournalBatches.Add(toBatch);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrePostingEditLine>> GetPrePostingEditListAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken)
    {
        var batches = await _context.JournalBatches
            .Where(b => b.CompanyId == companyId && b.FiscalPeriodId == fiscalPeriodId && b.Status != JournalBatchStatus.Posted)
            .Include(b => b.Lines)
            .OrderBy(b => b.BatchNumber)
            .ToListAsync(cancellationToken);

        var lines = new List<PrePostingEditLine>();
        foreach (var batch in batches)
        {
            foreach (var line in batch.Lines)
            {
                var accountNumber = await GetAccountNumberAsync(line.AccountId, cancellationToken);
                lines.Add(new PrePostingEditLine(
                    batch.Id,
                    batch.BatchNumber,
                    accountNumber,
                    line.AccountId,
                    line.Debit,
                    line.Credit,
                    line.Reference ?? string.Empty,
                    line.SegmentsJson ?? string.Empty,
                    batch.PostingDate,
                    batch.Status.ToString()));
            }
        }

        return lines;
    }

    public async Task<IReadOnlyList<PeriodEndChecklistItem>> GetPeriodEndChecklistAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken)
    {
        var items = new List<PeriodEndChecklistItem>();

        var unpostedGl = await _context.JournalBatches
            .CountAsync(b => b.CompanyId == companyId && b.FiscalPeriodId == fiscalPeriodId && b.Status != JournalBatchStatus.Posted, cancellationToken);
        items.Add(new PeriodEndChecklistItem(
            "GL unposted batches",
            unpostedGl == 0,
            unpostedGl == 0 ? "All GL batches posted" : $"{unpostedGl} unposted GL batch(es) must be posted before close"));

        var postedNet = await _context.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId && l.JournalBatch.FiscalPeriodId == fiscalPeriodId && l.JournalBatch.Status == JournalBatchStatus.Posted)
            .SumAsync(l => l.Debit - l.Credit, cancellationToken);
        items.Add(new PeriodEndChecklistItem(
            "Trial balance in balance",
            Math.Abs(postedNet) < 0.005m,
            Math.Abs(postedNet) < 0.005m ? "Posted debits equal credits" : $"Out of balance by {postedNet:C}"));

        await AddSubLedgerTieOut(items, companyId, fiscalPeriodId, cancellationToken);

        items.Add(new PeriodEndChecklistItem(
            "Year-end close run",
            true,
            "Run year-end close after all sub-ledgers are posted (closes income statement to retained earnings)"));

        return items;
    }

    private async Task AddSubLedgerTieOut(
        List<PeriodEndChecklistItem> items,
        Guid companyId,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken)
    {
        var apUnposted = await CountUnpostedViaSql("ap.VoucherBatches", companyId, fiscalPeriodId, cancellationToken);
        items.Add(new PeriodEndChecklistItem(
            "AP voucher batches posted",
            apUnposted == 0,
            apUnposted == 0 ? "AP tied out" : $"{apUnposted} unposted AP voucher batch(es)"));

        var arUnposted = await CountUnpostedViaSql("ar.InvoiceBatches", companyId, fiscalPeriodId, cancellationToken);
        items.Add(new PeriodEndChecklistItem(
            "AR invoice batches posted",
            arUnposted == 0,
            arUnposted == 0 ? "AR tied out" : $"{arUnposted} unposted AR invoice batch(es)"));

        items.Add(new PeriodEndChecklistItem(
            "Inventory transactions",
            true,
            "Inventory posts on receipt (no separate unposted batch state)"));
    }

    private async Task<int> CountUnpostedViaSql(
        string table,
        Guid companyId,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken)
    {
        const int postedStatus = 2;
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

#pragma warning disable CA2100
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table} WHERE CompanyId = @companyId AND FiscalPeriodId = @fiscalPeriodId AND Status <> @posted";
#pragma warning restore CA2100
        cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@companyId", companyId));
        cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@fiscalPeriodId", fiscalPeriodId));
        cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@posted", postedStatus));

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result == null ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task<Guid> RollForwardBudgetAsync(
        Guid budgetId,
        Guid targetFiscalYearId,
        CancellationToken cancellationToken)
    {
        var source = await _context.Budgets.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == budgetId, cancellationToken)
            ?? throw new InvalidOperationException($"Budget {budgetId} not found.");

        var target = new Budget(source.CompanyId, targetFiscalYearId, $"{source.Name} (Roll-forward)", source.Description, source.BudgetType);
        foreach (var line in source.Lines)
        {
            target.AddLine(line.AccountId, line.PeriodNumber, line.Amount, line.ProjectId);
        }

        _context.Budgets.Add(target);
        await _context.SaveChangesAsync(cancellationToken);
        return target.Id;
    }

    public async Task<Guid> TransferBudgetAsync(
        Guid budgetId,
        Guid accountId,
        int fromPeriodNumber,
        int toPeriodNumber,
        decimal amount,
        string reason,
        CancellationToken cancellationToken)
    {
        var budget = await _context.Budgets.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == budgetId, cancellationToken)
            ?? throw new InvalidOperationException($"Budget {budgetId} not found.");

        var fromLine = budget.Lines.FirstOrDefault(l => l.AccountId == accountId && l.PeriodNumber == fromPeriodNumber);
        if (fromLine == null)
        {
            throw new InvalidOperationException("No budget line to transfer from.");
        }

        if (fromLine.Amount < amount)
        {
            throw new InvalidOperationException("Transfer amount exceeds available budget.");
        }

        fromLine.AdjustAmount(fromLine.Amount - amount);

        var toLine = budget.Lines.FirstOrDefault(l => l.AccountId == accountId && l.PeriodNumber == toPeriodNumber);
        if (toLine == null)
        {
            budget.AddLine(accountId, toPeriodNumber, amount, fromLine.ProjectId);
        }
        else
        {
            toLine.AdjustAmount(toLine.Amount + amount);
        }

        _context.BudgetTransfers.Add(new BudgetTransfer(
            budget.CompanyId,
            budgetId,
            accountId,
            fromPeriodNumber,
            toPeriodNumber,
            amount,
            reason));
        await _context.SaveChangesAsync(cancellationToken);
        return budget.Id;
    }

    private async Task<string> GetAccountNumberAsync(Guid accountId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        return await platform.Accounts.Where(a => a.Id == accountId).Select(a => a.AccountNumber).FirstOrDefaultAsync(cancellationToken) ?? "Unknown";
    }
}

public record PrePostingEditLine(
    Guid BatchId,
    string BatchNumber,
    string AccountNumber,
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string Reference,
    string SegmentsJson,
    DateTimeOffset PostingDate,
    string Status);

public record PeriodEndChecklistItem(string Name, bool Passed, string Detail);
