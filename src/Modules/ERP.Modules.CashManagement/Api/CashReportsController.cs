// <copyright file="CashReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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
    private readonly ICustomerReadService _customerRead;

    public CashReportsController(
        CashDbContext context,
        ApDbContext apContext,
        ArDbContext arContext,
        ICashPositionJob cashPositionJob,
        IOutstandingCheckAgingJob agingJob,
        IPositivePayService positivePayService,
        ICustomerReadService customerRead)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _cashPositionJob = cashPositionJob ?? throw new ArgumentNullException(nameof(cashPositionJob));
        _agingJob = agingJob ?? throw new ArgumentNullException(nameof(agingJob));
        _positivePayService = positivePayService ?? throw new ArgumentNullException(nameof(positivePayService));
        _customerRead = customerRead ?? throw new ArgumentNullException(nameof(customerRead));
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

    // ---- PDF endpoints ----

    private async Task<CompanyDocumentHeader> GetHeaderAsync(Guid companyId, string title, string number, CancellationToken ct)
    {
        return await _customerRead.GetDocumentHeaderAsync(companyId, null, title, number, null, ct);
    }

    [HttpGet("cash-position/pdf")]
    public async Task<ActionResult> CashPositionPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var positions = await _cashPositionJob.RunAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Cash Position", "Cash Position", ct);
        var tableRows = positions.Select(p => new[]
        {
            p.AccountCode, p.AccountName, p.AccountNumber,
            p.CurrentBalance.ToString("C", CultureInfo.InvariantCulture),
            p.CurrencyCode, p.OutstandingChecks.ToString("C", CultureInfo.InvariantCulture),
            p.OutstandingDeposits.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var totalCash = positions.Sum(p => p.CurrentBalance);
        var doc = new ReportDocument(header, "Cash Position", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Code", "Name", "Account #", "Balance", "Currency", "Out. Checks", "Out. Deposits" }, tableRows) },
            totalCash, "Total Cash");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CashPosition.pdf");
    }

    [HttpGet("cash-forecast/pdf")]
    public async Task<ActionResult> CashForecastPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var positions = await _cashPositionJob.RunAsync(companyId, ct);
        var totalCash = positions.Sum(p => p.CurrentBalance);
        var openPayables = await _apContext.Vouchers
            .Where(v => v.VoucherBatchId != Guid.Empty && !v.SelectedForPayment)
            .SumAsync(v => v.TotalAmount, ct);
        var openInvoiceIds = await _arContext.Invoices
            .Where(i => i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid)
            .Select(i => i.Id)
            .ToListAsync(ct);
        var openInvoiced = await _arContext.InvoiceLines
            .Where(l => l.InvoiceId != null && openInvoiceIds.Contains(l.InvoiceId.Value))
            .SumAsync(l => (l.Quantity * l.UnitPrice) + l.TaxAmount - l.DiscountAmount, ct);
        var openApplied = await _arContext.CashReceiptApplications
            .Where(a => openInvoiceIds.Contains(a.InvoiceId))
            .SumAsync(a => a.AppliedAmount, ct);
        var openReceivables = decimal.Round(openInvoiced - openApplied, 2, MidpointRounding.AwayFromZero);
        var header = await GetHeaderAsync(companyId, "Cash Forecast", "Forecast", ct);
        var tableRows = new[] {
            new[] { "Current Cash", totalCash.ToString("C", CultureInfo.InvariantCulture) },
            new[] { "Open Payables", openPayables.ToString("C", CultureInfo.InvariantCulture) },
            new[] { "Open Receivables", openReceivables.ToString("C", CultureInfo.InvariantCulture) },
            new[] { "After Payables", (totalCash - openPayables).ToString("C", CultureInfo.InvariantCulture) },
            new[] { "After Collections", (totalCash - openPayables + openReceivables).ToString("C", CultureInfo.InvariantCulture) },
        };
        var doc = new ReportDocument(header, "Cash Forecast", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Amount" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CashForecast.pdf");
    }

    [HttpGet("reconciliation-summary/pdf")]
    public async Task<ActionResult> ReconciliationSummaryPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? bankAccountId, CancellationToken ct)
    {
        var accounts = await _context.BankAccounts
            .Where(a => a.CompanyId == companyId && (!bankAccountId.HasValue || a.Id == bankAccountId.Value))
            .OrderBy(a => a.AccountCode)
            .ToListAsync(ct);
        var header = await GetHeaderAsync(companyId, "Reconciliation Summary", "Recon Summary", ct);
        var allRows = new List<string[]>();
        foreach (var account in accounts)
        {
            var session = await _context.ReconciliationSessions
                .Where(s => s.BankAccountId == account.Id)
                .OrderByDescending(s => s.StatementDate)
                .FirstOrDefaultAsync(ct);
            if (session == null) continue;
            allRows.Add(new[]
            {
                account.AccountName, session.StatementDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                session.BeginningBalance.ToString("C", CultureInfo.InvariantCulture),
                session.EndingBalance.ToString("C", CultureInfo.InvariantCulture),
                (session.Variance ?? 0m).ToString("C", CultureInfo.InvariantCulture),
                session.Status.ToString(),
            });
        }
        var doc = new ReportDocument(header, "Reconciliation Summary", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Account", "Statement Date", "Beginning", "Ending", "Variance", "Status" }, allRows.ToArray()) },
            null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ReconciliationSummary.pdf");
    }

    [HttpGet("nsf-report/pdf")]
    public async Task<ActionResult> NsfReportPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var records = await _context.NsfRecords
            .Where(n => n.CompanyId == companyId)
            .OrderByDescending(n => n.ReturnedDate)
            .ToListAsync(ct);
        var header = await GetHeaderAsync(companyId, "NSF Report", "NSF", ct);
        var tableRows = records.Select(n => new[]
        {
            n.NsfNumber, n.Amount.ToString("C", CultureInfo.InvariantCulture),
            n.ReturnedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            n.BankReference ?? "—", n.Reason ?? "—",
            (n.NsfFeeAmount ?? 0m).ToString("C", CultureInfo.InvariantCulture), n.Status.ToString(),
        }).ToArray();
        var doc = new ReportDocument(header, "NSF Report", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "NSF #", "Amount", "Returned", "Bank Ref", "Reason", "Fee", "Status" }, tableRows) },
            records.Sum(n => n.Amount), "Total NSF");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "NsfReport.pdf");
    }

    [HttpGet("outstanding-checks/pdf")]
    public async Task<ActionResult> OutstandingChecksPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid bankAccountId, CancellationToken ct)
    {
        var report = await _agingJob.RunAsync(companyId, bankAccountId, ct);
        var header = await GetHeaderAsync(companyId, "Outstanding Checks", "Aging", ct);
        var tableRows = report.Buckets.Select(b => new[]
        {
            b.Bucket, b.CheckCount.ToString(CultureInfo.InvariantCulture),
            b.Amount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Outstanding Checks", $"{report.AccountName} — as of {report.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Bucket", "Checks", "Amount" }, tableRows) },
            report.Buckets.Sum(b => b.Amount), "Total Outstanding");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "OutstandingChecks.pdf");
    }

    [HttpGet("reconciliation-detail/pdf")]
    public async Task<ActionResult> ReconciliationDetailPdfAsync([FromQuery] Guid sessionId, CancellationToken ct)
    {
        var session = await _context.ReconciliationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) return NotFound();
        var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == session.BankAccountId, ct);
        var statement = await _context.BankStatements.FirstOrDefaultAsync(s => s.Id == session.BankStatementId, ct);
        var lines = await _context.BankStatementLines.Where(l => l.BankStatementId == session.BankStatementId).OrderBy(l => l.TransactionDate).ToListAsync(ct);
        var header = await GetHeaderAsync(account?.CompanyId ?? Guid.Empty, "Reconciliation Detail", "Recon", ct);
        var tableRows = lines.Select(l => new[]
        {
            l.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.Amount.ToString("C", CultureInfo.InvariantCulture),
            l.Description ?? "—",
            l.CheckNumber ?? "—",
            l.Status.ToString(),
        }).ToArray();
        var doc = new ReportDocument(header, "Reconciliation Detail",
            $"{account?.AccountName ?? "—"} — Statement {statement?.StatementNumber ?? "—"}",
            new[] { (new[] { "Date", "Amount", "Description", "Check #", "Status" }, tableRows) },
            session.EndingBalance, "Ending Balance");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ReconciliationDetail.pdf");
    }

    [HttpGet("bank-activity/pdf")]
    public async Task<ActionResult> BankActivityPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid bankAccountId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var statementIds = await _context.BankStatements
            .Where(s => s.CompanyId == companyId && s.BankAccountId == bankAccountId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var query = _context.BankStatementLines
            .Where(l => statementIds.Contains(l.BankStatementId));

        if (from.HasValue)
            query = query.Where(l => l.TransactionDate >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.TransactionDate <= to.Value);

        var data = await query
            .OrderBy(l => l.TransactionDate)
            .Select(l => new ReconciliationDetailLineResponse(
                l.TransactionDate,
                l.Amount,
                l.Description,
                l.CheckNumber,
                l.Status.ToString(),
                l.MatchedTransactionId,
                l.MatchedSource != null ? l.MatchedSource.ToString() : null))
            .ToListAsync(ct);

        var header = await GetHeaderAsync(companyId, "Bank Activity", "Activity", ct);
        var tableRows = data.Select(l => new[]
        {
            l.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.Amount.ToString("C", CultureInfo.InvariantCulture),
            l.Description ?? "—",
            l.CheckNumber ?? "—",
            l.Status,
        }).ToArray();
        var doc = new ReportDocument(header, "Bank Activity", $"{data.Count} transactions",
            new[] { (new[] { "Date", "Amount", "Description", "Check #", "Status" }, tableRows) },
            data.Sum(l => l.Amount), "Net Activity");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BankActivity.pdf");
    }
}

public record CashForecastResponse(
    decimal CurrentCash,
    decimal OpenPayables,
    decimal OpenReceivables,
    decimal ProjectedCashAfterPayables,
    decimal ProjectedCashAfterCollections);
