// <copyright file="CashReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cash/reports")]
public class CashReportsController : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly ApDbContext _apContext;
    private readonly ArDbContext _arContext;
    private readonly ICashPositionJob _cashPositionJob;
    private readonly IOutstandingCheckAgingJob _agingJob;
    private readonly IPositivePayService _positivePayService;

    public CashReportsController(
        CashDbContext context,
        ApDbContext apContext,
        ArDbContext arContext,
        ICashPositionJob cashPositionJob,
        IOutstandingCheckAgingJob agingJob,
        IPositivePayService positivePayService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _cashPositionJob = cashPositionJob ?? throw new ArgumentNullException(nameof(cashPositionJob));
        _agingJob = agingJob ?? throw new ArgumentNullException(nameof(agingJob));
        _positivePayService = positivePayService ?? throw new ArgumentNullException(nameof(positivePayService));
    }

    [HttpGet("cash-position")]
    public async Task<ActionResult<IReadOnlyList<CashPositionResponse>>> GetCashPositionAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var positions = await _cashPositionJob.RunAsync(companyId, cancellationToken);

        return Ok(positions.Select(p => new CashPositionResponse(
            p.BankAccountId,
            p.AccountCode,
            p.AccountName,
            p.AccountNumber,
            p.CurrentBalance,
            p.CurrencyCode,
            p.OutstandingChecks,
            p.OutstandingDeposits)).ToList());
    }

    [HttpGet("outstanding-checks")]
    public async Task<ActionResult<OutstandingCheckAgingResponse>> GetOutstandingChecksAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var report = await _agingJob.RunAsync(companyId, bankAccountId, cancellationToken);

        return Ok(new OutstandingCheckAgingResponse(
            report.BankAccountId,
            report.AccountName,
            report.AsOfDate,
            report.Buckets.Select(b => new OutstandingCheckBucketResponse(b.Bucket, b.Amount, b.CheckCount)).ToList()));
    }

    [HttpGet("positive-pay")]
    public async Task<ActionResult<string>> GetPositivePayAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var csv = await _positivePayService.ExportCsvAsync(companyId, bankAccountId, DateTimeOffset.UtcNow, cancellationToken);
        return Content(csv, "text/csv", System.Text.Encoding.UTF8);
    }

    [HttpGet("reconciliation-summary")]
    public async Task<ActionResult<IReadOnlyList<ReconciliationSummaryResponse>>> GetReconciliationSummaryAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? bankAccountId,
        CancellationToken cancellationToken)
    {
        var accounts = await _context.BankAccounts
            .Where(a => a.CompanyId == companyId
                && (!bankAccountId.HasValue || a.Id == bankAccountId.Value))
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var responses = new List<ReconciliationSummaryResponse>();

        foreach (var account in accounts)
        {
            var session = await _context.ReconciliationSessions
                .Where(s => s.BankAccountId == account.Id)
                .OrderByDescending(s => s.StatementDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (session == null)
                continue;

            var lines = await _context.BankStatementLines
                .Where(l => l.BankStatementId == session.BankStatementId)
                .ToListAsync(cancellationToken);

            var clearedDeposits = lines.Where(l => l.Amount > 0 && l.Status != BankStatementLineStatus.Unreconciled).Sum(l => l.Amount);
            var clearedWithdrawals = lines.Where(l => l.Amount < 0 && l.Status != BankStatementLineStatus.Unreconciled).Sum(l => Math.Abs(l.Amount));
            var outstandingDeposits = lines.Where(l => l.Amount > 0 && l.Status == BankStatementLineStatus.Unreconciled).Sum(l => l.Amount);
            var outstandingChecks = lines.Where(l => l.Amount < 0 && l.Status == BankStatementLineStatus.Unreconciled).Sum(l => Math.Abs(l.Amount));

            responses.Add(new ReconciliationSummaryResponse(
                account.Id,
                account.AccountName,
                session.StatementDate,
                session.BeginningBalance,
                session.EndingBalance,
                clearedDeposits,
                clearedWithdrawals,
                outstandingChecks,
                outstandingDeposits,
                session.Variance ?? session.EndingBalance - (session.BeginningBalance + clearedDeposits - clearedWithdrawals),
                session.Status.ToString()));
        }

        return Ok(responses);
    }

    [HttpGet("reconciliation-detail")]
    public async Task<ActionResult<ReconciliationDetailResponse>> GetReconciliationDetailAsync(
        [FromQuery] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == session.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {session.BankAccountId} not found.");

        var statement = await _context.BankStatements
            .FirstOrDefaultAsync(s => s.Id == session.BankStatementId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank statement {session.BankStatementId} not found.");

        var lines = await _context.BankStatementLines
            .Where(l => l.BankStatementId == session.BankStatementId)
            .OrderBy(l => l.TransactionDate)
            .Select(l => new ReconciliationDetailLineResponse(
                l.TransactionDate,
                l.Amount,
                l.Description,
                l.CheckNumber,
                l.Status.ToString(),
                l.MatchedTransactionId,
                l.MatchedSource != null ? l.MatchedSource.ToString() : null))
            .ToListAsync(cancellationToken);

        return Ok(new ReconciliationDetailResponse(
            account.Id,
            account.AccountName,
            statement.StatementNumber,
            session.StatementDate,
            session.BeginningBalance,
            session.EndingBalance,
            session.Variance ?? 0,
            session.Status.ToString(),
            lines));
    }

    [HttpGet("bank-activity")]
    public async Task<ActionResult<IReadOnlyList<ReconciliationDetailLineResponse>>> GetBankActivityAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid bankAccountId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var statementIds = await _context.BankStatements
            .Where(s => s.CompanyId == companyId && s.BankAccountId == bankAccountId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var query = _context.BankStatementLines
            .Where(l => statementIds.Contains(l.BankStatementId));

        if (from.HasValue)
        {
            query = query.Where(l => l.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.TransactionDate <= to.Value);
        }

        var lines = await query
            .OrderBy(l => l.TransactionDate)
            .Select(l => new ReconciliationDetailLineResponse(
                l.TransactionDate,
                l.Amount,
                l.Description,
                l.CheckNumber,
                l.Status.ToString(),
                l.MatchedTransactionId,
                l.MatchedSource != null ? l.MatchedSource.ToString() : null))
            .ToListAsync(cancellationToken);

        return Ok(lines);
    }

    [HttpGet("nsf-report")]
    public async Task<ActionResult<IReadOnlyList<NsfResponse>>> GetNsfReportAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var records = await _context.NsfRecords
            .Where(n => n.CompanyId == companyId)
            .OrderByDescending(n => n.ReturnedDate)
            .Select(n => new NsfResponse(
                n.Id,
                n.CompanyId,
                n.BankAccountId,
                n.CashReceiptId,
                n.CustomerId,
                n.NsfNumber,
                n.Amount,
                n.ReturnedDate,
                n.BankReference,
                n.Reason,
                n.NsfFeeAmount,
                n.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpGet("cash-forecast")]
    public async Task<ActionResult<CashForecastResponse>> GetCashForecastAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var positions = await _cashPositionJob.RunAsync(companyId, cancellationToken);
        var totalCash = positions.Sum(p => p.CurrentBalance);

        var openPayables = await _apContext.Vouchers
            .Where(v => v.VoucherBatchId != Guid.Empty && !v.SelectedForPayment)
            .SumAsync(v => v.TotalAmount, cancellationToken);

        var openInvoiceIds = await _arContext.Invoices
            .Where(i => i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        var openInvoiced = await _arContext.InvoiceLines
            .Where(l => l.InvoiceId != null && openInvoiceIds.Contains(l.InvoiceId.Value))
            .SumAsync(l => (l.Quantity * l.UnitPrice) + l.TaxAmount - l.DiscountAmount, cancellationToken);

        var openApplied = await _arContext.CashReceiptApplications
            .Where(a => openInvoiceIds.Contains(a.InvoiceId))
            .SumAsync(a => a.AppliedAmount, cancellationToken);

        var openReceivables = decimal.Round(openInvoiced - openApplied, 2, MidpointRounding.AwayFromZero);

        var forecast = new CashForecastResponse(
            totalCash,
            openPayables,
            openReceivables,
            totalCash - openPayables,
            totalCash - openPayables + openReceivables);

        return Ok(forecast);
    }
}

public record CashForecastResponse(
    decimal CurrentCash,
    decimal OpenPayables,
    decimal OpenReceivables,
    decimal ProjectedCashAfterPayables,
    decimal ProjectedCashAfterCollections);
