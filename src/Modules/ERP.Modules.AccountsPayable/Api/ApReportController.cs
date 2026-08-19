// <copyright file="ApReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/reports")]
public class ApReportController : ControllerBase
{
    private readonly ApDbContext _context;

    public ApReportController(ApDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApAgingReportDto>> GetAgingAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var vendors = await _context.Vendors
            .Where(v => !v.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var lines = new List<ApVendorAgingLineDto>();
        foreach (var vendor in vendors)
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.VendorId == vendor.Id
                    && !v.SelectedForPayment
                    && v.VoucherBatch != null
                    && v.VoucherBatch.Status == VoucherBatchStatus.Posted)
                .ToListAsync(cancellationToken);

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

        return Ok(new ApAgingReportDto(
            companyId,
            asOfDate,
            lines,
            lines.Sum(l => l.CurrentBalance),
            lines.Sum(l => l.TotalDue),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("vendor-trial-balance")]
    public async Task<ActionResult<VendorTrialBalanceReportDto>> GetVendorTrialBalanceAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var vendors = await _context.Vendors
            .Where(v => !v.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var lines = new List<VendorTrialBalanceLineDto>();
        foreach (var vendor in vendors)
        {
            var postedVouchers = await _context.Vouchers
                .Where(v => v.VendorId == vendor.Id
                    && v.VoucherBatch != null
                    && v.VoucherBatch.Status == VoucherBatchStatus.Posted)
                .ToListAsync(cancellationToken);

            var debits = postedVouchers.Sum(v => v.TotalAmount);
            var credits = postedVouchers
                .Where(v => v.SelectedForPayment)
                .Sum(v => v.TotalAmount);

            var payments = await _context.Payments
                .Where(p => p.VendorId == vendor.Id && p.Status == PaymentStatus.Issued)
                .SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0;

            lines.Add(new VendorTrialBalanceLineDto(
                vendor.Id,
                vendor.VendorId,
                vendor.Name,
                0,
                debits,
                payments + credits,
                debits - payments - credits));
        }

        return Ok(new VendorTrialBalanceReportDto(
            companyId,
            asOfDate,
            lines,
            lines.Sum(l => l.BeginningBalance),
            lines.Sum(l => l.EndingBalance),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("batch-register")]
    public async Task<ActionResult<ApBatchRegisterReportDto>> GetBatchRegisterAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var batches = await _context.VoucherBatches
            .Where(b => b.CompanyId == companyId && !b.DeletedOn.HasValue)
            .Include(b => b.Vouchers)
            .OrderByDescending(b => b.CreatedOn)
            .ToListAsync(cancellationToken);

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

        return Ok(new ApBatchRegisterReportDto(
            companyId,
            asOfDate,
            lines,
            batches.Count,
            lines.Sum(l => l.TotalAmount),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("cash-requirements")]
    public async Task<ActionResult<CashRequirementsReportDto>> GetCashRequirementsAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken,
        [FromQuery] int daysAhead = 30)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var cutoffDate = asOfDate.AddDays(daysAhead);

        var vouchers = await _context.Vouchers
            .Where(v => !v.SelectedForPayment
                && v.VoucherBatch != null
                && v.VoucherBatch.Status == VoucherBatchStatus.Posted
                && v.DueDate <= cutoffDate)
            .Include(v => v.VoucherBatch)
            .ToListAsync(cancellationToken);

        var vendorIds = vouchers.Select(v => v.VendorId).Distinct().ToList();
        var vendors = await _context.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

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

        return Ok(new CashRequirementsReportDto(
            companyId,
            asOfDate,
            daysAhead,
            lines,
            lines.Where(l => !l.PastDue).Sum(l => l.NetDue),
            lines.Where(l => l.PastDue).Sum(l => l.NetDue),
            lines.Sum(l => l.NetDue),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("1099-summary")]
    public async Task<ActionResult<Form1099ReportDto>> Get1099SummaryAsync(
        [FromQuery] Guid companyId,
        [FromQuery] int taxYear,
        CancellationToken cancellationToken)
    {
        var yearStart = new DateTimeOffset(taxYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd = new DateTimeOffset(taxYear, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var vendors = await _context.Vendors
            .Where(v => !v.DeletedOn.HasValue && v.Form1099Category != null && v.Form1099Category != Vendor1099Category.None)
            .ToListAsync(cancellationToken);

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
                .SumAsync(v => (decimal?)v.Form1099Amount, cancellationToken) ?? 0;

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

        return Ok(new Form1099ReportDto(
            companyId,
            taxYear,
            lines,
            lines.Sum(l => l.TotalPayments),
            lines.Sum(l => l.BackupWithholding),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("check-register")]
    public async Task<ActionResult<CheckRegisterReportDto>> GetCheckRegisterAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTimeOffset.UtcNow;

        var payments = await _context.Payments
            .Where(p => p.CompanyId == companyId
                && p.PaymentDate >= from
                && p.PaymentDate <= to)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var vendorIds = payments.Select(p => p.VendorId).Distinct().ToList();
        var vendors = await _context.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

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

        return Ok(new CheckRegisterReportDto(
            companyId,
            from,
            to,
            lines,
            lines.Sum(l => l.Amount),
            lines.Count,
            DateTimeOffset.UtcNow));
    }

    [HttpGet("account-distribution")]
    public async Task<ActionResult<ApAccountDistributionReportDto>> GetAccountDistributionAsync(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken);

        return Ok(new ApAccountDistributionReportDto(
            companyId,
            from,
            to,
            distributions,
            distributions.Sum(l => l.Debit),
            distributions.Sum(l => l.Credit),
            DateTimeOffset.UtcNow));
    }
}
