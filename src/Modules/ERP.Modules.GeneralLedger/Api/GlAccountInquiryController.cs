// <copyright file="GlAccountInquiryController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/account-inquiry")]
public class GlAccountInquiryController : ControllerBase
{
    private readonly GlDbContext _glContext;
    private readonly PlatformDbContext _platformContext;

    public GlAccountInquiryController(GlDbContext glContext, PlatformDbContext platformContext)
    {
        _glContext = glContext ?? throw new ArgumentNullException(nameof(glContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
    }

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<AccountInquiryDto>> GetAccountInquiry(
        Guid accountId,
        [FromQuery] Guid? fiscalPeriodId = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var account = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.DeletedOn.HasValue, cancellationToken);
        if (account == null)
            return NotFound();

        var companyId = account.CompanyId;

        var query = _glContext.JournalEntryLines
            .Where(l => l.JournalBatch != null && l.JournalBatch.CompanyId == companyId && l.AccountId == accountId)
            .AsQueryable();

        if (fiscalPeriodId.HasValue)
            query = query.Where(l => l.JournalBatch!.FiscalPeriodId == fiscalPeriodId.Value);
        if (fromDate.HasValue)
            query = query.Where(l => l.JournalBatch!.PostingDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(l => l.JournalBatch!.PostingDate <= toDate.Value);

        var lines = await query
            .Where(l => l.JournalBatch!.Status == JournalBatchStatus.Posted)
            .Include(l => l.JournalBatch)
            .OrderBy(l => l.JournalBatch!.PostingDate)
            .ToListAsync(cancellationToken);

        var periodBalances = new List<AccountPeriodBalanceDto>();
        var detailLines = new List<AccountInquiryLineDto>();

        var byPeriod = lines
            .Where(l => l.JournalBatch != null)
            .GroupBy(l => l.JournalBatch!.FiscalPeriodId);

        foreach (var grp in byPeriod)
        {
            var period = await _platformContext.FiscalPeriods
                .FirstOrDefaultAsync(fp => fp.Id == grp.Key && !fp.DeletedOn.HasValue, cancellationToken);
            var debit = grp.Sum(l => l.Debit);
            var credit = grp.Sum(l => l.Credit);
            periodBalances.Add(new AccountPeriodBalanceDto(
                grp.Key,
                period?.PeriodNumber ?? 0,
                period?.Description ?? "Unknown",
                debit,
                credit,
                debit - credit));
        }

        foreach (var line in lines)
        {
            var batch = line.JournalBatch!;
            detailLines.Add(new AccountInquiryLineDto(
                batch.Id,
                batch.BatchNumber,
                batch.PostingDate,
                batch.Status.ToString(),
                line.Debit,
                line.Credit,
                line.Reference ?? string.Empty,
                InferSourceDocument(batch.Description),
                line.SegmentsJson ?? string.Empty));
        }

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        return Ok(new AccountInquiryDto(
            account.Id,
            account.AccountNumber,
            account.Description,
            account.AccountType.ToString(),
            account.NormalBalance.ToString(),
            account.IsActive,
            companyId,
            totalDebit,
            totalCredit,
            totalDebit - totalCredit,
            periodBalances,
            detailLines));
    }

    private static string InferSourceDocument(string batchDescription)
    {
        if (string.IsNullOrWhiteSpace(batchDescription))
            return "General Journal";

        var checks = new (string Token, string Source)[]
        {
            ("voucher", "AP Voucher"),
            ("ap ", "AP Voucher"),
            ("invoice", "AR Invoice"),
            ("ar ", "AR Invoice"),
            ("receipt", "Inventory Transaction"),
            ("inventory", "Inventory Transaction"),
            ("revaluation", "Revaluation"),
            ("consolidation", "Consolidation"),
            ("year-end", "Period/Year Close"),
            ("close", "Period/Year Close"),
            ("allocation", "Allocation"),
            ("payroll", "Payroll"),
            ("intercompany", "Intercompany"),
            ("ic-", "Intercompany"),
        };

        foreach (var (token, source) in checks)
        {
            if (batchDescription.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }
        }

        return "General Journal";
    }
}

public record AccountInquiryDto(
    Guid AccountId, string AccountNumber, string Description, string AccountType, string NormalBalance,
    bool IsActive, Guid CompanyId, decimal TotalDebit, decimal TotalCredit, decimal NetBalance,
    IReadOnlyList<AccountPeriodBalanceDto> PeriodBalances, IReadOnlyList<AccountInquiryLineDto> Lines);

public record AccountPeriodBalanceDto(
    Guid FiscalPeriodId, int PeriodNumber, string PeriodName, decimal Debit, decimal Credit, decimal Net);

public record AccountInquiryLineDto(
    Guid BatchId, string BatchNumber, DateTimeOffset PostingDate, string Status,
    decimal Debit, decimal Credit, string Reference, string SourceDocument, string SegmentsJson);
