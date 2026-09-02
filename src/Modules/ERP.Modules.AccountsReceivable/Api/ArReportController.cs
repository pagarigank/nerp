// <copyright file="ArReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/reports")]
public class ArReportController : ControllerBase
{
    private readonly ArDbContext _context;
    private readonly ICustomerReadService _customerRead;

    public ArReportController(ArDbContext context, ICustomerReadService customerRead)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _customerRead = customerRead ?? throw new ArgumentNullException(nameof(customerRead));
    }

    private async Task<CompanyDocumentHeader> GetHeaderAsync(Guid companyId, string title, string number, CancellationToken ct)
    {
        return await _customerRead.GetDocumentHeaderAsync(companyId, null, title, number, null, ct);
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ArAgingReportDto>> GetAgingAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var customers = await _context.Customers
            .Where(c => !c.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var lines = new List<ArAgingLineDto>();
        foreach (var customer in customers)
        {
            var invoices = await _context.Invoices
                .Where(i => i.CustomerId == customer.Id
                    && i.Status != InvoiceStatus.Voided
                    && i.Status != InvoiceStatus.Paid)
                .ToListAsync(cancellationToken);

            if (invoices.Count == 0)
                continue;

            var current = 0m;
            var days1To30 = 0m;
            var days31To60 = 0m;
            var days61To90 = 0m;
            var over90 = 0m;

            foreach (var inv in invoices)
            {
                var daysOverdue = (asOfDate - inv.DueDate).Days;
                var balance = inv.BalanceDue;

                if (daysOverdue <= 0)
                    current += balance;
                else if (daysOverdue <= 30)
                    days1To30 += balance;
                else if (daysOverdue <= 60)
                    days31To60 += balance;
                else if (daysOverdue <= 90)
                    days61To90 += balance;
                else
                    over90 += balance;
            }

            lines.Add(new ArAgingLineDto(
                customer.Id,
                customer.CustomerId,
                customer.Name,
                current,
                days1To30,
                days31To60,
                days61To90,
                over90,
                current + days1To30 + days31To60 + days61To90 + over90));
        }

        return Ok(new ArAgingReportDto(
            companyId,
            asOfDate,
            lines,
            lines.Sum(l => l.CurrentBalance),
            lines.Sum(l => l.TotalDue),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("finance-charge")]
    public async Task<ActionResult<FinanceChargeReportDto>> GetFinanceChargeReportAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTimeOffset.UtcNow;

        var charges = await _context.FinanceCharges
            .Where(f => f.CompanyId == companyId
                && f.ChargeDate >= from
                && f.ChargeDate <= to
                && !f.DeletedOn.HasValue)
            .OrderByDescending(f => f.ChargeDate)
            .ToListAsync(cancellationToken);

        var customerIds = charges.Select(f => f.CustomerId).Distinct().ToList();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var lines = charges.Select(f =>
        {
            var customer = customers.GetValueOrDefault(f.CustomerId);
            return new FinanceChargeReportLineDto(
                f.Id,
                f.ChargeNumber,
                f.CustomerId,
                customer?.Name ?? string.Empty,
                f.ChargeDate,
                f.ChargeAmount,
                f.AnnualRate,
                f.Status.ToString());
        }).ToList();

        return Ok(new FinanceChargeReportDto(
            companyId,
            DateTimeOffset.UtcNow,
            lines,
            lines.Sum(l => l.Amount),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("customer-trial-balance")]
    public async Task<ActionResult<CustomerTrialBalanceReportDto>> GetCustomerTrialBalanceAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var customers = await _context.Customers
            .Where(c => !c.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var lines = new List<CustomerTrialBalanceLineDto>();
        foreach (var customer in customers)
        {
            var postedInvoices = await _context.Invoices
                .Where(i => i.CustomerId == customer.Id
                    && i.Status != InvoiceStatus.Voided)
                .ToListAsync(cancellationToken);

            var totalInvoiced = postedInvoices.Sum(i => i.TotalAmount);
            var totalPaid = postedInvoices.Sum(i => i.TotalAmount - i.BalanceDue);

            lines.Add(new CustomerTrialBalanceLineDto(
                customer.Id,
                customer.CustomerId,
                customer.Name,
                0,
                totalInvoiced,
                totalPaid,
                totalInvoiced - totalPaid));
        }

        return Ok(new CustomerTrialBalanceReportDto(
            companyId,
            asOfDate,
            lines,
            lines.Sum(l => l.BeginningBalance),
            lines.Sum(l => l.EndingBalance),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("cash-receipts-journal")]
    public async Task<ActionResult<CashReceiptsJournalReportDto>> GetCashReceiptsJournalAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTimeOffset.UtcNow;

        var receipts = await _context.CashReceipts
            .Where(r => r.CompanyId == companyId
                && r.ReceiptDate >= from
                && r.ReceiptDate <= to
                && !r.DeletedOn.HasValue)
            .OrderByDescending(r => r.ReceiptDate)
            .ToListAsync(cancellationToken);

        var customerIds = receipts.Select(r => r.CustomerId).Distinct().ToList();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var lines = receipts.Select(r =>
        {
            var customer = customers.GetValueOrDefault(r.CustomerId);
            return new CashReceiptsJournalLineDto(
                r.Id,
                r.ReceiptReference,
                r.CustomerId,
                customer?.Name ?? string.Empty,
                r.ReceiptDate,
                r.TotalAmount,
                r.PaymentMethod,
                r.Status.ToString());
        }).ToList();

        return Ok(new CashReceiptsJournalReportDto(
            companyId,
            from,
            to,
            lines,
            lines.Sum(l => l.Amount),
            lines.Count,
            DateTimeOffset.UtcNow));
    }

    [HttpGet("sales-journal")]
    public async Task<ActionResult<SalesJournalReportDto>> GetSalesJournalAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTimeOffset.UtcNow;

        var invoices = await _context.Invoices
            .Where(i => i.InvoiceDate >= from
                && i.InvoiceDate <= to
                && i.Status != InvoiceStatus.Voided)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var lines = invoices.Select(i =>
        {
            var customer = customers.GetValueOrDefault(i.CustomerId);
            return new SalesJournalLineDto(
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                customer?.Name ?? string.Empty,
                i.InvoiceDate,
                i.TotalAmount,
                i.Status.ToString());
        }).ToList();

        return Ok(new SalesJournalReportDto(
            companyId,
            from,
            to,
            lines,
            lines.Sum(l => l.Amount),
            lines.Count,
            DateTimeOffset.UtcNow));
    }

    // ---- PDF endpoints ----

    [HttpGet("aging/pdf")]
    public async Task<ActionResult> AgingPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var report = await GetAgingAsync(companyId, ct);
        if (report.Value == null) return NotFound();
        var data = report.Value;
        var header = await GetHeaderAsync(companyId, "AR Aging Report", "AR Aging", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.CustomerName,
            l.CurrentBalance.ToString("C", CultureInfo.InvariantCulture),
            l.Days1To30.ToString("C", CultureInfo.InvariantCulture),
            l.Days31To60.ToString("C", CultureInfo.InvariantCulture),
            l.Days61To90.ToString("C", CultureInfo.InvariantCulture),
            l.Over90Days.ToString("C", CultureInfo.InvariantCulture),
            l.TotalDue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "AR Aging", $"As of {data.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Customer", "Current", "1-30", "31-60", "61-90", "90+", "Total" }, tableRows) },
            data.TotalDue, "Total Due");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ARAging.pdf");
    }

    [HttpGet("customer-trial-balance/pdf")]
    public async Task<ActionResult> CustomerTrialBalancePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var report = await GetCustomerTrialBalanceAsync(companyId, ct);
        if (report.Value == null) return NotFound();
        var data = report.Value;
        var header = await GetHeaderAsync(companyId, "Customer Trial Balance", "CTB", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.CustomerName,
            l.BeginningBalance.ToString("C", CultureInfo.InvariantCulture),
            l.Debits.ToString("C", CultureInfo.InvariantCulture),
            l.Credits.ToString("C", CultureInfo.InvariantCulture),
            l.EndingBalance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Customer Trial Balance", $"As of {data.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Customer", "Beginning", "Debits", "Credits", "Ending" }, tableRows) },
            data.TotalEndingBalance, "Total Ending Balance");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CustomerTrialBalance.pdf");
    }

    [HttpGet("cash-receipts-journal/pdf")]
    public async Task<ActionResult> CashReceiptsJournalPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var report = await GetCashReceiptsJournalAsync(companyId, fromDate, toDate, ct);
        if (report.Value == null) return NotFound();
        var data = report.Value;
        var header = await GetHeaderAsync(companyId, "Cash Receipts Journal", "CRJ", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.ReceiptReference, l.CustomerName, l.ReceiptDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.Amount.ToString("C", CultureInfo.InvariantCulture), l.PaymentMethod, l.Status,
        }).ToArray();
        var doc = new ReportDocument(header, "Cash Receipts Journal",
            $"{data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}",
            new[] { (new[] { "Reference", "Customer", "Date", "Amount", "Method", "Status" }, tableRows) },
            data.TotalAmount, "Total Receipts");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CashReceiptsJournal.pdf");
    }

    [HttpGet("sales-journal/pdf")]
    public async Task<ActionResult> SalesJournalPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var report = await GetSalesJournalAsync(companyId, fromDate, toDate, ct);
        if (report.Value == null) return NotFound();
        var data = report.Value;
        var header = await GetHeaderAsync(companyId, "Sales Journal", "SJ", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.InvoiceNumber, l.CustomerName, l.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.Amount.ToString("C", CultureInfo.InvariantCulture), l.Status,
        }).ToArray();
        var doc = new ReportDocument(header, "Sales Journal",
            $"{data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}",
            new[] { (new[] { "Invoice", "Customer", "Date", "Amount", "Status" }, tableRows) },
            data.TotalAmount, "Total Sales");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SalesJournal.pdf");
    }

    [HttpGet("finance-charge/pdf")]
    public async Task<ActionResult> FinanceChargePdfAsync([FromQuery] Guid companyId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var report = await GetFinanceChargeReportAsync(companyId, fromDate, toDate, ct);
        if (report.Value == null) return NotFound();
        var data = report.Value;
        var header = await GetHeaderAsync(companyId, "Finance Charge Report", "Finance Charges", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.ChargeNumber, l.CustomerName, l.ChargeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.Amount.ToString("C", CultureInfo.InvariantCulture),
            (l.AnnualRate * 100).ToString("F2") + "%", l.Status,
        }).ToArray();
        var doc = new ReportDocument(header, "Finance Charges", $"As of {data.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Charge #", "Customer", "Date", "Amount", "Rate", "Status" }, tableRows) },
            data.TotalCharges, "Total Charges");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "FinanceCharges.pdf");
    }
}
