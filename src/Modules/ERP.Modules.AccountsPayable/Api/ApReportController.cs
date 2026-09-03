// <copyright file="ApReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/reports")]
public class ApReportController : ControllerBase
{
    private readonly ApDbContext _context;
    private readonly ICustomerReadService _customerRead;

    public ApReportController(ApDbContext context, ICustomerReadService customerRead)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _customerRead = customerRead ?? throw new ArgumentNullException(nameof(customerRead));
    }

    private async Task<CompanyDocumentHeader> GetHeaderAsync(Guid companyId, string title, string number, CancellationToken ct)
    {
        return await _customerRead.GetDocumentHeaderAsync(companyId, null, title, number, null, ct);
    }

    // ---- Data-fetching helpers (shared by JSON + PDF endpoints) ----

    private async Task<ApAgingReportDto> GetAgingDataAsync(Guid companyId, CancellationToken ct)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var vendors = await _context.Vendors
            .Where(v => !v.DeletedOn.HasValue)
            .ToListAsync(ct);

        var lines = new List<ApVendorAgingLineDto>();
        foreach (var vendor in vendors)
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.VendorId == vendor.Id
                    && !v.SelectedForPayment
                    && v.VoucherBatch != null
                    && v.VoucherBatch.Status == VoucherBatchStatus.Posted)
                .ToListAsync(ct);

            if (vouchers.Count == 0)
                continue;

            var current = 0m;
            var days1To30 = 0m;
            var days31To60 = 0m;
            var days61To90 = 0m;
            var over90 = 0m;

            foreach (var v in vouchers)
            {
                var daysOverdue = (asOfDate - v.DueDate).Days;
                var amount = v.TotalAmount - v.DiscountAmount;

                if (daysOverdue <= 0)
                    current += amount;
                else if (daysOverdue <= 30)
                    days1To30 += amount;
                else if (daysOverdue <= 60)
                    days31To60 += amount;
                else if (daysOverdue <= 90)
                    days61To90 += amount;
                else
                    over90 += amount;
            }

            lines.Add(new ApVendorAgingLineDto(
                vendor.Id,
                vendor.VendorId,
                vendor.Name,
                current,
                days1To30,
                days31To60,
                days61To90,
                over90,
                current + days1To30 + days31To60 + days61To90 + over90));
        }

        return new ApAgingReportDto(
            companyId,
            asOfDate,
            lines,
            lines.Sum(l => l.CurrentBalance),
            lines.Sum(l => l.TotalDue),
            DateTimeOffset.UtcNow);
    }

    private async Task<VendorTrialBalanceReportDto> GetVendorTrialBalanceDataAsync(Guid companyId, CancellationToken ct)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var vendors = await _context.Vendors
            .Where(v => !v.DeletedOn.HasValue)
            .ToListAsync(ct);

        var lines = new List<VendorTrialBalanceLineDto>();
        foreach (var vendor in vendors)
        {
            var postedBatchIds = await _context.VoucherBatches
                .Where(b => b.Status == VoucherBatchStatus.Posted && !b.DeletedOn.HasValue)
                .Select(b => b.Id)
                .ToListAsync(ct);
            var postedVouchers = await _context.Vouchers
                .Where(v => v.VendorId == vendor.Id && postedBatchIds.Contains(v.VoucherBatchId))
                .ToListAsync(ct);

            var debits = postedVouchers.Sum(v => v.TotalAmount);
            var credits = postedVouchers
                .Where(v => v.SelectedForPayment)
                .Sum(v => v.TotalAmount);

            var paymentLines = await _context.Payments
                .Where(p => p.VendorId == vendor.Id && p.Status == PaymentStatus.Issued)
                .SelectMany(p => p.Lines)
                .ToListAsync(ct);
            var totalPayments = paymentLines.Sum(l => l.AppliedAmount);

            lines.Add(new VendorTrialBalanceLineDto(
                vendor.Id,
                vendor.VendorId,
                vendor.Name,
                0,
                debits,
                totalPayments + credits,
                debits - totalPayments - credits));
        }

        return new VendorTrialBalanceReportDto(
            companyId,
            asOfDate,
            lines,
            lines.Sum(l => l.BeginningBalance),
            lines.Sum(l => l.EndingBalance),
            DateTimeOffset.UtcNow);
    }

    private async Task<ApBatchRegisterReportDto> GetBatchRegisterDataAsync(Guid companyId, CancellationToken ct)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var batches = await _context.VoucherBatches
            .Where(b => b.CompanyId == companyId && !b.DeletedOn.HasValue)
            .Include(b => b.Vouchers)
            .OrderByDescending(b => b.CreatedOn)
            .ToListAsync(ct);

        var lines = batches.Select(b => new ApBatchRegisterLineDto(
            b.Id,
            b.BatchNumber,
            b.Description ?? string.Empty,
            b.PostingDate,
            b.Status.ToString(),
            b.Vouchers.Count,
            b.Vouchers.Sum(v => v.TotalAmount),
            b.Vouchers.Sum(v => v.DiscountAmount)))
            .ToList();

        return new ApBatchRegisterReportDto(
            companyId,
            asOfDate,
            lines,
            batches.Count,
            lines.Sum(l => l.TotalAmount),
            DateTimeOffset.UtcNow);
    }

    private async Task<CashRequirementsReportDto> GetCashRequirementsDataAsync(Guid companyId, CancellationToken ct, int daysAhead = 30)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var cutoffDate = asOfDate.AddDays(daysAhead);

        var vouchers = await _context.Vouchers
            .Where(v => !v.SelectedForPayment
                && v.VoucherBatch != null
                && v.VoucherBatch.Status == VoucherBatchStatus.Posted
                && v.DueDate <= cutoffDate)
            .Include(v => v.VoucherBatch)
            .ToListAsync(ct);

        var vendorIds = vouchers.Select(v => v.VendorId).Distinct().ToList();
        var vendors = await _context.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var lines = vouchers.Select(v =>
        {
            var vendor = vendors.GetValueOrDefault(v.VendorId);
            return new CashRequirementLineDto(
                v.VendorId,
                vendor?.VendorId ?? string.Empty,
                vendor?.Name ?? string.Empty,
                v.Id,
                v.InvoiceNumber,
                v.DueDate,
                v.TotalAmount,
                v.DiscountAmount,
                v.TotalAmount - v.DiscountAmount,
                v.DueDate < asOfDate);
        }).ToList();

        return new CashRequirementsReportDto(
            companyId,
            asOfDate,
            daysAhead,
            lines,
            lines.Where(l => !l.PastDue).Sum(l => l.NetDue),
            lines.Where(l => l.PastDue).Sum(l => l.NetDue),
            lines.Sum(l => l.NetDue),
            DateTimeOffset.UtcNow);
    }

    private async Task<Form1099ReportDto> Get1099SummaryDataAsync(Guid companyId, int taxYear, CancellationToken ct)
    {
        var yearStart = new DateTimeOffset(taxYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd = new DateTimeOffset(taxYear, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var vendors = await _context.Vendors
            .Where(v => !v.DeletedOn.HasValue && v.Form1099Category != null && v.Form1099Category != Vendor1099Category.None)
            .ToListAsync(ct);

        var lines = new List<Form1099ReportLineDto>();
        foreach (var vendor in vendors)
        {
            var payments = await _context.Vouchers
                .Where(v => v.VendorId == vendor.Id
                    && v.Form1099Amount > 0
                    && v.VoucherBatch != null
                    && v.VoucherBatch.Status == VoucherBatchStatus.Posted
                    && v.InvoiceDate >= yearStart
                    && v.InvoiceDate <= yearEnd)
                .SumAsync(v => (decimal?)v.Form1099Amount, ct) ?? 0;

            if (payments <= 0)
                continue;

            lines.Add(new Form1099ReportLineDto(
                vendor.Id,
                vendor.VendorId,
                vendor.Name,
                vendor.TaxId,
                vendor.Form1099Category?.ToString() ?? "None",
                payments,
                payments * vendor.BackupWithholdingRate));
        }

        return new Form1099ReportDto(
            companyId,
            taxYear,
            lines,
            lines.Sum(l => l.TotalPayments),
            lines.Sum(l => l.BackupWithholding),
            DateTimeOffset.UtcNow);
    }

    private async Task<CheckRegisterReportDto> GetCheckRegisterDataAsync(Guid companyId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTimeOffset.UtcNow;

        var payments = await _context.Payments
            .Where(p => p.CompanyId == companyId
                && p.PaymentDate >= from
                && p.PaymentDate <= to)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

        var vendorIds = payments.Select(p => p.VendorId).Distinct().ToList();
        var vendors = await _context.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var lines = payments.Select(p =>
        {
            var vendor = vendors.GetValueOrDefault(p.VendorId);
            return new CheckRegisterLineDto(
                p.Id,
                p.PaymentReference,
                p.VendorId,
                vendor?.Name ?? string.Empty,
                p.PaymentDate,
                p.PaymentMethod.ToString(),
                p.TotalAmount,
                p.Status.ToString());
        }).ToList();

        return new CheckRegisterReportDto(
            companyId,
            from,
            to,
            lines,
            lines.Sum(l => l.Amount),
            lines.Count,
            DateTimeOffset.UtcNow);
    }

    private async Task<ApAccountDistributionReportDto> GetAccountDistributionDataAsync(Guid companyId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTimeOffset.UtcNow;

        var distributions = await _context.VoucherDistributions
            .Where(d => d.Voucher != null
                && d.Voucher.VoucherBatch != null
                && d.Voucher.VoucherBatch.CompanyId == companyId
                && d.Voucher.VoucherBatch.Status == VoucherBatchStatus.Posted
                && d.Voucher.InvoiceDate >= from
                && d.Voucher.InvoiceDate <= to)
            .GroupBy(d => d.AccountId)
            .Select(g => new ApAccountDistributionLineDto(
                g.Key,
                string.Empty,
                string.Empty,
                g.Sum(d => d.Debit),
                g.Sum(d => d.Credit),
                g.Count()))
            .ToListAsync(ct);

        return new ApAccountDistributionReportDto(
            companyId,
            from,
            to,
            distributions,
            distributions.Sum(l => l.Debit),
            distributions.Sum(l => l.Credit),
            DateTimeOffset.UtcNow);
    }

    // ---- JSON endpoints ----

    [HttpGet("aging")]
    public async Task<ActionResult<ApAgingReportDto>> GetAgingAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        return Ok(await GetAgingDataAsync(companyId, cancellationToken));
    }

    [HttpGet("vendor-trial-balance")]
    public async Task<ActionResult<VendorTrialBalanceReportDto>> GetVendorTrialBalanceAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        return Ok(await GetVendorTrialBalanceDataAsync(companyId, cancellationToken));
    }

    [HttpGet("batch-register")]
    public async Task<ActionResult<ApBatchRegisterReportDto>> GetBatchRegisterAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        return Ok(await GetBatchRegisterDataAsync(companyId, cancellationToken));
    }

    [HttpGet("cash-requirements")]
    public async Task<ActionResult<CashRequirementsReportDto>> GetCashRequirementsAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken,
        [FromQuery] int daysAhead = 30)
    {
        return Ok(await GetCashRequirementsDataAsync(companyId, cancellationToken, daysAhead));
    }

    [HttpGet("1099-summary")]
    public async Task<ActionResult<Form1099ReportDto>> Get1099SummaryAsync(
        [FromQuery] Guid companyId,
        [FromQuery] int taxYear,
        CancellationToken cancellationToken)
    {
        return Ok(await Get1099SummaryDataAsync(companyId, taxYear, cancellationToken));
    }

    [HttpGet("check-register")]
    public async Task<ActionResult<CheckRegisterReportDto>> GetCheckRegisterAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        return Ok(await GetCheckRegisterDataAsync(companyId, fromDate, toDate, cancellationToken));
    }

    [HttpGet("account-distribution")]
    public async Task<ActionResult<ApAccountDistributionReportDto>> GetAccountDistributionAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        return Ok(await GetAccountDistributionDataAsync(companyId, fromDate, toDate, cancellationToken));
    }

    // ---- PDF endpoints ----

    [HttpGet("aging/pdf")]
    public async Task<ActionResult> AgingPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var data = await GetAgingDataAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "AP Aging Report", "AP Aging", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.VendorName, l.CurrentBalance.ToString("C", CultureInfo.InvariantCulture),
            l.Days1To30.ToString("C", CultureInfo.InvariantCulture), l.Days31To60.ToString("C", CultureInfo.InvariantCulture),
            l.Days61To90.ToString("C", CultureInfo.InvariantCulture), l.Over90Days.ToString("C", CultureInfo.InvariantCulture),
            l.TotalDue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "AP Aging", $"As of {data.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Vendor", "Current", "1-30", "31-60", "61-90", "90+", "Total" }, tableRows) },
            data.TotalDue, "Total Due");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "APAging.pdf");
    }

    [HttpGet("vendor-trial-balance/pdf")]
    public async Task<ActionResult> VendorTrialBalancePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var data = await GetVendorTrialBalanceDataAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Vendor Trial Balance", "VTB", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.VendorName, l.BeginningBalance.ToString("C", CultureInfo.InvariantCulture),
            l.Debits.ToString("C", CultureInfo.InvariantCulture), l.Credits.ToString("C", CultureInfo.InvariantCulture),
            l.EndingBalance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Vendor Trial Balance", $"As of {data.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Vendor", "Beginning", "Debits", "Credits", "Ending" }, tableRows) },
            data.TotalEndingBalance, "Total Ending Balance");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "VendorTrialBalance.pdf");
    }

    [HttpGet("batch-register/pdf")]
    public async Task<ActionResult> BatchRegisterPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var data = await GetBatchRegisterDataAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "AP Batch Register", "Batch Register", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.BatchNumber, l.Description, l.PostingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.Status, l.VoucherCount.ToString(),
            l.TotalAmount.ToString("C", CultureInfo.InvariantCulture),
            l.TotalDiscount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Batch Register", $"As of {data.AsOfDate:yyyy-MM-dd}",
            new[] { (new[] { "Batch #", "Description", "Date", "Status", "Vouchers", "Total", "Discount" }, tableRows) },
            data.GrandTotal, "Grand Total");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "APBatchRegister.pdf");
    }

    [HttpGet("cash-requirements/pdf")]
    public async Task<ActionResult> CashRequirementsPdfAsync([FromQuery] Guid companyId, [FromQuery] int daysAhead, CancellationToken ct)
    {
        var data = await GetCashRequirementsDataAsync(companyId, ct, daysAhead);
        var header = await GetHeaderAsync(companyId, "Cash Requirements", "Cash Req", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.VendorName, l.InvoiceNumber, l.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.OriginalAmount.ToString("C", CultureInfo.InvariantCulture),
            l.DiscountAmount.ToString("C", CultureInfo.InvariantCulture),
            l.NetDue.ToString("C", CultureInfo.InvariantCulture),
            l.PastDue ? "Yes" : "—",
        }).ToArray();
        var doc = new ReportDocument(header, "Cash Requirements", $"{data.DaysAhead} days ahead",
            new[] { (new[] { "Vendor", "Invoice", "Due Date", "Amount", "Discount", "Net Due", "Past Due" }, tableRows) },
            data.GrandTotal, "Grand Total");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CashRequirements.pdf");
    }

    [HttpGet("1099-summary/pdf")]
    public async Task<ActionResult> Form1099PdfAsync([FromQuery] Guid companyId, [FromQuery] int taxYear, CancellationToken ct)
    {
        var data = await Get1099SummaryDataAsync(companyId, taxYear, ct);
        var header = await GetHeaderAsync(companyId, "1099 Summary", $"1099-{data.TaxYear}", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.VendorName, l.Category,
            l.TotalPayments.ToString("C", CultureInfo.InvariantCulture),
            l.BackupWithholding.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "1099 Summary", $"Tax Year {data.TaxYear}",
            new[] { (new[] { "Vendor", "Category", "Payments", "Backup Withholding" }, tableRows) },
            data.TotalPayments, "Total Payments");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "Form1099Summary.pdf");
    }

    [HttpGet("check-register/pdf")]
    public async Task<ActionResult> CheckRegisterPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var data = await GetCheckRegisterDataAsync(companyId, fromDate, toDate, ct);
        var header = await GetHeaderAsync(companyId, "Check Register", "Checks", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.PaymentReference, l.VendorName, l.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            l.PaymentMethod, l.Status, l.Amount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Check Register",
            $"{data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}",
            new[] { (new[] { "Reference", "Vendor", "Date", "Method", "Status", "Amount" }, tableRows) },
            data.TotalAmount, "Total Payments");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CheckRegister.pdf");
    }

    [HttpGet("account-distribution/pdf")]
    public async Task<ActionResult> AccountDistributionPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var data = await GetAccountDistributionDataAsync(companyId, fromDate, toDate, ct);
        var header = await GetHeaderAsync(companyId, "AP Account Distribution", "AP Distribution", ct);
        var tableRows = data.Lines.Select(l => new[]
        {
            l.AccountNumber, l.AccountDescription, l.TransactionCount.ToString(),
            l.Debit.ToString("C", CultureInfo.InvariantCulture), l.Credit.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "AP Account Distribution",
            $"{data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}",
            new[] { (new[] { "Account", "Description", "Transactions", "Debit", "Credit" }, tableRows) },
            null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "APAccountDistribution.pdf");
    }
}
