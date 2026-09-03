// <copyright file="GlReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/reports")]
public class GlReportController : ControllerBase
{
    private readonly GlDbContext _glContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IRevaluationService _revaluationService;
    private readonly ICustomerReadService _customerRead;

    public GlReportController(GlDbContext glContext, PlatformDbContext platformContext, IRevaluationService revaluationService, ICustomerReadService customerRead)
    {
        _glContext = glContext ?? throw new ArgumentNullException(nameof(glContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _revaluationService = revaluationService ?? throw new ArgumentNullException(nameof(revaluationService));
        _customerRead = customerRead ?? throw new ArgumentNullException(nameof(customerRead));
    }

    private async Task<CompanyDocumentHeader> GetHeaderAsync(Guid companyId, string title, string number, CancellationToken ct)
    {
        return await _customerRead.GetDocumentHeaderAsync(companyId, null, title, number, null, ct);
    }

    // ---- Data-fetching helpers (shared by JSON + PDF endpoints) ----

    private async Task<TrialBalanceReportDto> GetTrialBalanceDataAsync(Guid companyId, Guid? fiscalPeriodId, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var accounts = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId && !a.DeletedOn.HasValue)
            .ToListAsync(ct);

        var postedLines = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
        {
            postedLines = postedLines.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        }

        var postedLinesList = await postedLines
            .Where(l => l.JournalBatch!.Status == JournalBatchStatus.Posted)
            .ToListAsync(ct);

        var grouped = postedLinesList
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => new
            {
                Debit = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit)
            });

        var lines = new List<TrialBalanceLineDto>();
        var totalDebit = 0m;
        var totalCredit = 0m;

        foreach (var account in accounts.OrderBy(a => a.AccountNumber))
        {
            var hasActivity = grouped.TryGetValue(account.Id, out var activity);

            var debit = hasActivity ? activity!.Debit : 0m;
            var credit = hasActivity ? activity!.Credit : 0m;

            var net = account.NormalBalance == NormalBalance.Debit
                ? debit - credit
                : credit - debit;

            totalDebit += debit;
            totalCredit += credit;

            lines.Add(new TrialBalanceLineDto(
                account.Id,
                account.AccountNumber,
                account.Description,
                account.AccountType,
                account.NormalBalance,
                0m,
                debit,
                credit,
                net));
        }

        return new TrialBalanceReportDto(
            companyId,
            company.Name,
            fiscalPeriodId,
            totalDebit,
            totalCredit,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<GeneralLedgerDetailReportDto> GetGeneralLedgerDetailDataAsync(Guid companyId, Guid? fiscalPeriodId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var query = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
            query = query.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.JournalBatch!.PostingDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.JournalBatch!.PostingDate <= toDate.Value);

        var lines = await query
            .Where(l => l.JournalBatch!.Status == JournalBatchStatus.Posted)
            .OrderBy(l => l.JournalBatch!.PostingDate)
            .ThenBy(l => l.JournalBatch!.BatchNumber)
            .Select(l => new
            {
                l.Id,
                l.JournalBatchId,
                l.JournalBatch!.BatchNumber,
                l.JournalBatch.PostingDate,
                l.Reference,
                l.AccountId,
                l.Debit,
                l.Credit,
                l.SegmentsJson
            })
            .ToListAsync(ct);

        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _platformContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => new { a.AccountNumber, a.Description }, ct);

        var detailLines = lines.Select(l => new GeneralLedgerDetailLineDto(
            l.JournalBatchId,
            l.BatchNumber,
            l.PostingDate,
            l.Reference,
            l.AccountId,
            accounts.TryGetValue(l.AccountId, out var a) ? a.AccountNumber : "Unknown",
            accounts.TryGetValue(l.AccountId, out var a2) ? a2.Description : "Unknown",
            l.Debit,
            l.Credit,
            l.SegmentsJson)).ToList();

        var totalDebit = detailLines.Sum(l => l.Debit);
        var totalCredit = detailLines.Sum(l => l.Credit);

        return new GeneralLedgerDetailReportDto(
            companyId,
            company.Name,
            fiscalPeriodId,
            fromDate,
            toDate,
            totalDebit,
            totalCredit,
            detailLines,
            DateTimeOffset.UtcNow);
    }

    private async Task<UnpostedTransactionsReportDto> GetUnpostedTransactionsDataAsync(Guid companyId, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var batches = await _glContext.JournalBatches
            .Where(b => b.CompanyId == companyId && b.Status != JournalBatchStatus.Posted && b.Status != JournalBatchStatus.Reversed)
            .OrderByDescending(b => b.CreatedOn)
            .Select(b => new UnpostedTransactionDto(
                b.Id,
                b.BatchNumber,
                b.Description,
                b.PostingDate,
                b.Status.ToString(),
                b.Lines.Count,
                b.Lines.Sum(l => l.Debit),
                b.Lines.Sum(l => l.Credit),
                b.CreatedOn))
            .ToListAsync(ct);

        return new UnpostedTransactionsReportDto(
            companyId,
            company.Name,
            batches,
            DateTimeOffset.UtcNow);
    }

    private async Task<FinancialStatementReportDto> GetFinancialStatementDataAsync(Guid companyId, string statementType, Guid? fiscalPeriodId, AccountType[] accountTypes, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var accounts = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId && accountTypes.Contains(a.AccountType) && !a.DeletedOn.HasValue)
            .ToListAsync(ct);

        var postedLines = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
        {
            postedLines = postedLines.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        }

        var postedLinesList = await postedLines
            .Where(l => l.JournalBatch!.Status == JournalBatchStatus.Posted)
            .ToListAsync(ct);

        var grouped = postedLinesList
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => new
            {
                Debit = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit)
            });

        var lines = new List<FinancialStatementLineDto>();
        var totalAmount = 0m;

        foreach (var account in accounts.OrderBy(a => a.AccountNumber))
        {
            var hasActivity = grouped.TryGetValue(account.Id, out var activity);
            var debit = hasActivity ? activity!.Debit : 0m;
            var credit = hasActivity ? activity!.Credit : 0m;

            var balance = account.NormalBalance == NormalBalance.Debit
                ? debit - credit
                : credit - debit;

            totalAmount += balance;

            lines.Add(new FinancialStatementLineDto(
                account.Id,
                account.AccountNumber,
                account.Description,
                balance));
        }

        return new FinancialStatementReportDto(
            companyId,
            company.Name,
            fiscalPeriodId,
            statementType,
            totalAmount,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<CashFlowReportDto> GetCashFlowDataAsync(Guid companyId, Guid? fiscalPeriodId, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var accounts = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId && !a.DeletedOn.HasValue)
            .ToListAsync(ct);

        var postedLines = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId && l.JournalBatch.Status == JournalBatchStatus.Posted)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
        {
            postedLines = postedLines.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        }

        var postedLinesList = await postedLines.ToListAsync(ct);

        var accountMap = accounts.ToDictionary(a => a.Id);
        var lines = new List<CashFlowLineDto>();
        var netOperating = 0m;
        var netInvesting = 0m;
        var netFinancing = 0m;

        foreach (var entry in postedLinesList)
        {
            if (!accountMap.TryGetValue(entry.AccountId, out var account))
                continue;

            var amount = entry.Debit - entry.Credit;
            var category = account.AccountType switch
            {
                AccountType.Asset => account.NormalBalance == NormalBalance.Debit ? "Operating" : "Investing",
                AccountType.Liability => "Operating",
                AccountType.Equity => "Financing",
                AccountType.Revenue => "Operating",
                AccountType.Expense => "Operating",
                _ => "Operating"
            };

            switch (category)
            {
                case "Operating":
                    netOperating += amount;
                    break;
                case "Investing":
                    netInvesting += amount;
                    break;
                case "Financing":
                    netFinancing += amount;
                    break;
            }

            lines.Add(new CashFlowLineDto(
                category,
                account.Id,
                account.AccountNumber,
                account.Description,
                amount));
        }

        var netChange = netOperating + netInvesting + netFinancing;

        return new CashFlowReportDto(
            companyId,
            company.Name,
            fiscalPeriodId,
            netOperating,
            netInvesting,
            netFinancing,
            netChange,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<BudgetVsActualReportDto> GetBudgetVsActualDataAsync(Guid companyId, Guid budgetId, Guid? fiscalPeriodId, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var budget = await _glContext.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.CompanyId == companyId && !b.DeletedOn.HasValue, ct);
        if (budget == null)
            return null!;

        var accountIds = budget.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _platformContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => new { a.AccountNumber, a.Description }, ct);

        var postedLines = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId && l.JournalBatch.Status == JournalBatchStatus.Posted)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
        {
            postedLines = postedLines.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        }

        var postedLinesList = await postedLines.ToListAsync(ct);

        var actualByAccount = postedLinesList
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Credit - l.Debit));

        var lines = new List<BudgetVsActualLineDto>();
        var totalBudget = 0m;
        var totalActual = 0m;

        int? targetPeriodNumber = null;
        if (fiscalPeriodId.HasValue)
        {
            var fiscalPeriod = await _platformContext.FiscalPeriods
                .FirstOrDefaultAsync(fp => fp.Id == fiscalPeriodId.Value && !fp.DeletedOn.HasValue, ct);
            if (fiscalPeriod != null)
                targetPeriodNumber = fiscalPeriod.PeriodNumber;
        }

        IReadOnlyList<BudgetLine> budgetLines = budget.Lines;
        foreach (var budgetLine in budgetLines)
        {
            if (targetPeriodNumber.HasValue && budgetLine.PeriodNumber != targetPeriodNumber.Value)
                continue;

            var actual = actualByAccount.GetValueOrDefault(budgetLine.AccountId, 0m);
            var variance = budgetLine.Amount - actual;
            var variancePct = budgetLine.Amount != 0m ? Math.Round((variance / budgetLine.Amount) * 100, 2) : 0m;

            totalBudget += budgetLine.Amount;
            totalActual += actual;

            var acct = accounts.GetValueOrDefault(budgetLine.AccountId);
            lines.Add(new BudgetVsActualLineDto(
                budgetLine.AccountId,
                acct?.AccountNumber ?? "Unknown",
                acct?.Description ?? "Unknown",
                budgetLine.Amount,
                actual,
                variance,
                variancePct));
        }

        var totalVariance = totalBudget - totalActual;

        return new BudgetVsActualReportDto(
            companyId,
            company.Name,
            budgetId,
            budget.Name,
            fiscalPeriodId,
            totalBudget,
            totalActual,
            totalVariance,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<AccountDistributionReportDto> GetAccountDistributionDataAsync(Guid companyId, Guid? fiscalPeriodId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var query = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId && l.JournalBatch.Status == JournalBatchStatus.Posted)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
            query = query.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.JournalBatch!.PostingDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.JournalBatch!.PostingDate <= toDate.Value);

        var grouped = await query
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Debit = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit),
                Count = g.Count()
            })
            .ToListAsync(ct);

        var accountIds = grouped.Select(g => g.AccountId).ToList();
        var accounts = await _platformContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => new { a.AccountNumber, a.Description, a.AccountType }, ct);

        var lines = grouped.Select(g =>
        {
            var acct = accounts.GetValueOrDefault(g.AccountId);
            return new AccountDistributionLineDto(
                g.AccountId,
                acct?.AccountNumber ?? "Unknown",
                acct?.Description ?? "Unknown",
                acct?.AccountType ?? AccountType.Expense,
                g.Debit,
                g.Credit,
                g.Debit - g.Credit,
                g.Count);
        }).OrderByDescending(l => Math.Abs(l.NetChange)).ToList();

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        return new AccountDistributionReportDto(
            companyId,
            company.Name,
            fiscalPeriodId,
            fromDate,
            toDate,
            totalDebit,
            totalCredit,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<ConsolidatedTrialBalanceReportDto> GetConsolidatedTrialBalanceDataAsync(Guid parentCompanyId, Guid? fiscalPeriodId, CancellationToken ct)
    {
        var parentCompany = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == parentCompanyId && !c.DeletedOn.HasValue, ct);
        if (parentCompany == null)
            return null!;

        var childCompanies = await _platformContext.Companies
            .Where(c => c.ParentCompanyId == parentCompanyId && !c.DeletedOn.HasValue)
            .ToListAsync(ct);

        var allCompanies = new List<Company> { parentCompany };
        allCompanies.AddRange(childCompanies);
        var companyIds = allCompanies.Select(c => c.Id).ToList();

        var accounts = await _platformContext.Accounts
            .Where(a => companyIds.Contains(a.CompanyId) && !a.DeletedOn.HasValue)
            .ToListAsync(ct);

        var postedLinesQuery = _glContext.JournalEntryLines
            .Include(l => l.JournalBatch)
            .Where(l => l.JournalBatch != null && companyIds.Contains(l.JournalBatch.CompanyId) && l.JournalBatch.Status == JournalBatchStatus.Posted);

        if (fiscalPeriodId.HasValue)
        {
            postedLinesQuery = postedLinesQuery.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        }

        var postedLinesList = await postedLinesQuery.ToListAsync(ct);

        var grouped = postedLinesList
            .GroupBy(l => new { AccountId = l.AccountId, CompanyId = l.JournalBatch!.CompanyId })
            .ToDictionary(g => g.Key, g => new
            {
                Debit = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit)
            });

        var lines = new List<ConsolidatedTrialBalanceLineDto>();
        var totalDebit = 0m;
        var totalCredit = 0m;

        foreach (var account in accounts.OrderBy(a => a.AccountNumber))
        {
            var hasActivity = grouped.TryGetValue(new { AccountId = account.Id, CompanyId = account.CompanyId }, out var activity);

            var debit = hasActivity ? activity!.Debit : 0m;
            var credit = hasActivity ? activity!.Credit : 0m;

            var net = account.NormalBalance == NormalBalance.Debit
                ? debit - credit
                : credit - debit;

            totalDebit += debit;
            totalCredit += credit;

            var company = allCompanies.FirstOrDefault(c => c.Id == account.CompanyId);
            lines.Add(new ConsolidatedTrialBalanceLineDto(
                account.Id,
                account.AccountNumber,
                account.Description,
                account.AccountType,
                account.NormalBalance,
                0m,
                debit,
                credit,
                net,
                account.CompanyId,
                company?.Name ?? "Unknown"));
        }

        return new ConsolidatedTrialBalanceReportDto(
            parentCompanyId,
            parentCompany.Name,
            fiscalPeriodId,
            totalDebit,
            totalCredit,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<IntercompanyBalanceReportDto> GetIntercompanyBalanceDataAsync(Guid parentCompanyId, Guid? fiscalPeriodId, CancellationToken ct)
    {
        var parentCompany = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == parentCompanyId && !c.DeletedOn.HasValue, ct);
        if (parentCompany == null)
            return null!;

        var childCompanyIds = await _platformContext.Companies
            .Where(c => c.ParentCompanyId == parentCompanyId && !c.DeletedOn.HasValue)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var mappings = await _glContext.IntercompanyMappings
            .Where(m => m.IsActive &&
                (m.FromCompanyId == parentCompanyId ||
                 childCompanyIds.Contains(m.FromCompanyId) ||
                 childCompanyIds.Contains(m.ToCompanyId)))
            .ToListAsync(ct);

        var allCompanyIds = new List<Guid> { parentCompanyId };
        allCompanyIds.AddRange(childCompanyIds);

        var postedLinesQuery = _glContext.JournalEntryLines
            .Include(l => l.JournalBatch)
            .Where(l => l.JournalBatch != null && allCompanyIds.Contains(l.JournalBatch.CompanyId) && l.JournalBatch.Status == JournalBatchStatus.Posted);

        if (fiscalPeriodId.HasValue)
        {
            postedLinesQuery = postedLinesQuery.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        }

        var postedLinesList = await postedLinesQuery.ToListAsync(ct);

        var accountIds = postedLinesList.Select(l => l.AccountId).Distinct().ToList();
        var accountsDict = await _platformContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.AccountNumber, ct);

        var intercompanyLines = postedLinesList
            .Where(l => mappings.Any(m =>
                (m.FromCompanyId == l.JournalBatch!.CompanyId && m.FromAccountNumber == (accountsDict.GetValueOrDefault(l.AccountId) ?? string.Empty)) ||
                (m.ToCompanyId == l.JournalBatch!.CompanyId && m.ToAccountNumber == (accountsDict.GetValueOrDefault(l.AccountId) ?? string.Empty))))
            .ToList();

        var lines = new List<IntercompanyBalanceLineDto>();

        foreach (var mapping in mappings)
        {
            var fromAccount = await _platformContext.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == mapping.FromCompanyId && a.AccountNumber == mapping.FromAccountNumber && !a.DeletedOn.HasValue, ct);
            var toAccount = await _platformContext.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == mapping.ToCompanyId && a.AccountNumber == mapping.ToAccountNumber && !a.DeletedOn.HasValue, ct);

            if (fromAccount == null || toAccount == null)
                continue;

            var fromLines = intercompanyLines.Where(x => x.JournalBatch!.CompanyId == mapping.FromCompanyId && x.AccountId == fromAccount.Id).ToList();
            var toLines = intercompanyLines.Where(x => x.JournalBatch!.CompanyId == mapping.ToCompanyId && x.AccountId == toAccount.Id).ToList();

            var fromBalance = fromLines.Sum(x => x.Credit - x.Debit);
            var toBalance = toLines.Sum(x => x.Credit - x.Debit);

            var fromCompany = await _platformContext.Companies.FirstOrDefaultAsync(c => c.Id == mapping.FromCompanyId && !c.DeletedOn.HasValue, ct);
            var toCompany = await _platformContext.Companies.FirstOrDefaultAsync(c => c.Id == mapping.ToCompanyId && !c.DeletedOn.HasValue, ct);

            lines.Add(new IntercompanyBalanceLineDto(
                mapping.FromCompanyId,
                fromCompany?.Name ?? "Unknown",
                mapping.ToCompanyId,
                toCompany?.Name ?? "Unknown",
                mapping.FromAccountNumber,
                mapping.ToAccountNumber,
                fromBalance - toBalance));
        }

        return new IntercompanyBalanceReportDto(
            parentCompanyId,
            parentCompany.Name,
            fiscalPeriodId,
            lines,
            DateTimeOffset.UtcNow);
    }

    private async Task<MultiCurrencyRevaluationReportDto> GetMultiCurrencyRevaluationDataAsync(Guid companyId, Guid fiscalPeriodId, DateTimeOffset revaluationDate, CancellationToken ct)
    {
        var company = await _platformContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var preview = await _revaluationService.PreviewRevaluationAsync(companyId, fiscalPeriodId, revaluationDate, ct);

        var lines = preview.Lines.Select(l => new MultiCurrencyRevaluationLineDto(
            l.AccountId,
            l.AccountNumber,
            l.AccountNumber,
            company.BaseCurrency,
            l.OriginalDebit - l.OriginalCredit,
            l.RevaluedDebit - l.RevaluedCredit,
            l.GainLoss)).ToList();

        return new MultiCurrencyRevaluationReportDto(
            companyId,
            company.Name,
            fiscalPeriodId,
            revaluationDate,
            preview.EstimatedGainLoss,
            lines,
            DateTimeOffset.UtcNow);
    }

    // ---- JSON endpoints ----

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReportDto>> GetTrialBalance(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetTrialBalanceDataAsync(companyId, fiscalPeriodId, cancellationToken));
    }

    [HttpGet("general-ledger-detail")]
    public async Task<ActionResult<GeneralLedgerDetailReportDto>> GetGeneralLedgerDetail(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetGeneralLedgerDetailDataAsync(companyId, fiscalPeriodId, fromDate, toDate, cancellationToken));
    }

    [HttpGet("unposted-transactions")]
    public async Task<ActionResult<UnpostedTransactionsReportDto>> GetUnpostedTransactions(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetUnpostedTransactionsDataAsync(companyId, cancellationToken));
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<FinancialStatementReportDto>> GetBalanceSheet(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetFinancialStatementDataAsync(companyId, "BalanceSheet", fiscalPeriodId, new[] { AccountType.Asset, AccountType.Liability, AccountType.Equity }, cancellationToken));
    }

    [HttpGet("income-statement")]
    public async Task<ActionResult<FinancialStatementReportDto>> GetIncomeStatement(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetFinancialStatementDataAsync(companyId, "IncomeStatement", fiscalPeriodId, new[] { AccountType.Revenue, AccountType.Expense }, cancellationToken));
    }

    [HttpGet("cash-flow")]
    public async Task<ActionResult<CashFlowReportDto>> GetCashFlow(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetCashFlowDataAsync(companyId, fiscalPeriodId, cancellationToken));
    }

    [HttpGet("budget-vs-actual")]
    public async Task<ActionResult<BudgetVsActualReportDto>> GetBudgetVsActual(
        [FromQuery] Guid companyId,
        [FromQuery] Guid budgetId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetBudgetVsActualDataAsync(companyId, budgetId, fiscalPeriodId, cancellationToken));
    }

    [HttpGet("account-distribution")]
    public async Task<ActionResult<AccountDistributionReportDto>> GetAccountDistribution(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetAccountDistributionDataAsync(companyId, fiscalPeriodId, fromDate, toDate, cancellationToken));
    }

    [HttpGet("consolidated-trial-balance")]
    public async Task<ActionResult<ConsolidatedTrialBalanceReportDto>> GetConsolidatedTrialBalance(
        [FromQuery] Guid parentCompanyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetConsolidatedTrialBalanceDataAsync(parentCompanyId, fiscalPeriodId, cancellationToken));
    }

    [HttpGet("intercompany-balance")]
    public async Task<ActionResult<IntercompanyBalanceReportDto>> GetIntercompanyBalance(
        [FromQuery] Guid parentCompanyId,
        [FromQuery] Guid? fiscalPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetIntercompanyBalanceDataAsync(parentCompanyId, fiscalPeriodId, cancellationToken));
    }

    [HttpGet("multi-currency-revaluation")]
    public async Task<ActionResult<MultiCurrencyRevaluationReportDto>> GetMultiCurrencyRevaluation(
        [FromQuery] Guid companyId,
        [FromQuery] Guid fiscalPeriodId,
        [FromQuery] DateTimeOffset revaluationDate,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetMultiCurrencyRevaluationDataAsync(companyId, fiscalPeriodId, revaluationDate, cancellationToken));
    }

    // ---- PDF endpoints ----

    [HttpGet("trial-balance/pdf")]
    public async Task<ActionResult> TrialBalancePdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetTrialBalanceDataAsync(companyId, fiscalPeriodId, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Trial Balance", "TB", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.AccountDescription, l.AccountType.ToString(),
            l.Debit.ToString("C", CultureInfo.InvariantCulture),
            l.Credit.ToString("C", CultureInfo.InvariantCulture),
            l.EndingBalance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Trial Balance", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Account", "Description", "Type", "Debit", "Credit", "Balance" }, tableRows) },
            data.TotalDebit, "Total Debits");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "TrialBalance.pdf");
    }

    [HttpGet("balance-sheet/pdf")]
    public async Task<ActionResult> BalanceSheetPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetFinancialStatementDataAsync(companyId, "BalanceSheet", fiscalPeriodId, new[] { AccountType.Asset, AccountType.Liability, AccountType.Equity }, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Balance Sheet", "BS", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.AccountDescription,
            l.Balance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Balance Sheet", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Account", "Description", "Balance" }, tableRows) },
            data.TotalAmount, "Total");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BalanceSheet.pdf");
    }

    [HttpGet("income-statement/pdf")]
    public async Task<ActionResult> IncomeStatementPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetFinancialStatementDataAsync(companyId, "IncomeStatement", fiscalPeriodId, new[] { AccountType.Revenue, AccountType.Expense }, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Income Statement", "IS", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.AccountDescription,
            l.Balance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Income Statement", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Account", "Description", "Amount" }, tableRows) },
            data.TotalAmount, "Total");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "IncomeStatement.pdf");
    }

    [HttpGet("cash-flow/pdf")]
    public async Task<ActionResult> CashFlowPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetCashFlowDataAsync(companyId, fiscalPeriodId, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Cash Flow Statement", "CF", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.Category, l.AccountNumber, l.AccountDescription,
            l.Amount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Cash Flow", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Category", "Account", "Description", "Amount" }, tableRows) },
            data.NetCashChange, "Net Change");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CashFlow.pdf");
    }

    [HttpGet("unposted-transactions/pdf")]
    public async Task<ActionResult> UnpostedTransactionsPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var data = await GetUnpostedTransactionsDataAsync(companyId, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Unposted Transactions", "Unposted", ct);
        var tableRows = data.Batches.Select(b => new[]
        {
            b.BatchNumber, b.Description, b.PostingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            b.Status, b.LineCount.ToString(),
            b.TotalDebits.ToString("C", CultureInfo.InvariantCulture),
            b.TotalCredits.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Unposted Transactions", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Batch #", "Description", "Date", "Status", "Lines", "Debits", "Credits" }, tableRows) },
            null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "UnpostedTransactions.pdf");
    }

    [HttpGet("general-ledger-detail/pdf")]
    public async Task<ActionResult> GeneralLedgerDetailPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var data = await GetGeneralLedgerDetailDataAsync(companyId, fiscalPeriodId, fromDate, toDate, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "General Ledger Detail", "GL Detail", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.PostingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), l.BatchNumber,
            l.AccountNumber, l.Reference ?? "—",
            l.Debit.ToString("C", CultureInfo.InvariantCulture),
            l.Credit.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "General Ledger Detail", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Date", "Batch", "Account", "Reference", "Debit", "Credit" }, tableRows) },
            data.TotalDebit, "Total Debits");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "GeneralLedgerDetail.pdf");
    }

    [HttpGet("account-distribution/pdf")]
    public async Task<ActionResult> AccountDistributionPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var data = await GetAccountDistributionDataAsync(companyId, fiscalPeriodId, fromDate, toDate, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Account Distribution", "Distribution", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.AccountDescription, l.TransactionCount.ToString(),
            l.Debit.ToString("C", CultureInfo.InvariantCulture),
            l.Credit.ToString("C", CultureInfo.InvariantCulture),
            l.NetChange.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Account Distribution", $"{data.CompanyName} — As of {data.GeneratedOn:yyyy-MM-dd}",
            new[] { (new[] { "Account", "Description", "Txns", "Debit", "Credit", "Net" }, tableRows) },
            data.TotalDebit, "Total Debits");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "AccountDistribution.pdf");
    }

    [HttpGet("budget-vs-actual/pdf")]
    public async Task<ActionResult> BudgetVsActualPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid budgetId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetBudgetVsActualDataAsync(companyId, budgetId, fiscalPeriodId, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Budget vs Actual", "BvA", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.AccountDescription,
            l.BudgetAmount.ToString("C", CultureInfo.InvariantCulture),
            l.ActualAmount.ToString("C", CultureInfo.InvariantCulture),
            l.Variance.ToString("C", CultureInfo.InvariantCulture),
            $"{l.VariancePercent}%",
        }).ToArray();
        var doc = new ReportDocument(header, "Budget vs Actual",
            $"{data.CompanyName} — {data.BudgetName}",
            new[] { (new[] { "Account", "Description", "Budget", "Actual", "Variance", "Var %" }, tableRows) },
            data.TotalBudget, "Total Budget");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BudgetVsActual.pdf");
    }

    [HttpGet("consolidated-trial-balance/pdf")]
    public async Task<ActionResult> ConsolidatedTrialBalancePdfAsync([FromQuery] Guid parentCompanyId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetConsolidatedTrialBalanceDataAsync(parentCompanyId, fiscalPeriodId, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(parentCompanyId, "Consolidated Trial Balance", "CTB", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.CompanyName, l.AccountNumber, l.AccountDescription,
            l.Debit.ToString("C", CultureInfo.InvariantCulture),
            l.Credit.ToString("C", CultureInfo.InvariantCulture),
            l.EndingBalance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var companyCount = data.Lines.Select(l => l.CompanyId).Distinct().Count();
        var doc = new ReportDocument(header, "Consolidated Trial Balance",
            $"{companyCount} companies",
            new[] { (new[] { "Company", "Account", "Description", "Debit", "Credit", "Balance" }, tableRows) },
            data.TotalDebit, "Total Debits");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ConsolidatedTrialBalance.pdf");
    }

    [HttpGet("intercompany-balance/pdf")]
    public async Task<ActionResult> IntercompanyBalancePdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? fiscalPeriodId, CancellationToken ct)
    {
        var data = await GetIntercompanyBalanceDataAsync(companyId, fiscalPeriodId, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Intercompany Balance", "IC", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.FromCompanyName, l.ToCompanyName, $"{l.FromAccountNumber} → {l.ToAccountNumber}",
            l.Balance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Intercompany Balance",
            $"{data.Lines.Count} balances",
            new[] { (new[] { "From", "To", "IC Accounts", "Balance" }, tableRows) },
            data.Lines.Sum(l => Math.Abs(l.Balance)), "Total Balance");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "IntercompanyBalance.pdf");
    }

    [HttpGet("multi-currency-revaluation/pdf")]
    public async Task<ActionResult> MultiCurrencyRevaluationPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid fiscalPeriodId, [FromQuery] DateTimeOffset revaluationDate, CancellationToken ct)
    {
        var data = await GetMultiCurrencyRevaluationDataAsync(companyId, fiscalPeriodId, revaluationDate, ct);
        if (data == null) return NotFound();
        var header = await GetHeaderAsync(companyId, "Multi-Currency Revaluation", "Reval", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.Currency,
            l.OriginalBalance.ToString("C", CultureInfo.InvariantCulture),
            l.RevaluedBalance.ToString("C", CultureInfo.InvariantCulture),
            l.GainLoss.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Multi-Currency Revaluation",
            $"{data.Lines.Count} accounts",
            new[] { (new[] { "Account", "Currency", "Original", "Revalued", "Gain/Loss" }, tableRows) },
            data.TotalGainLoss, "Estimated Gain/Loss");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "MultiCurrencyRevaluation.pdf");
    }
}
