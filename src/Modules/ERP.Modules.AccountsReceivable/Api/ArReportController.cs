// <copyright file="ArReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/reports")]
public class ArReportController : ControllerBase
{
    private readonly ArDbContext _context;

    public ArReportController(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
}
