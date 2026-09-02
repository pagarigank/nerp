// <copyright file="SalesReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using ERP.Modules.OrderManagement.Application.Services;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.OrderManagement.Reporting.Documents;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.OrderManagement.Api;

public sealed record CommissionRunRow(
    Guid Id,
    string RunNumber,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int RepCount,
    decimal TotalRevenue,
    decimal TotalCommission);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/reports")]
#pragma warning disable S6960
public class SalesReportsController : ControllerBase
#pragma warning restore S6960
{
    private readonly SalesReportService _reportService;
    private readonly OmDbContext _context;
    private readonly ICustomerReadService _customerRead;
    private readonly IVendorReadService _vendorRead;

    public SalesReportsController(
        SalesReportService reportService,
        OmDbContext context,
        ICustomerReadService customerRead,
        IVendorReadService vendorRead)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _customerRead = customerRead ?? throw new ArgumentNullException(nameof(customerRead));
        _vendorRead = vendorRead ?? throw new ArgumentNullException(nameof(vendorRead));
    }

    [HttpGet("open-orders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpenOrderRow>>>> OpenOrdersAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetOpenOrdersAsync(companyId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OpenOrderRow>>.Success(rows));
    }

    [HttpGet("backorders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BackorderRow>>>> BackordersAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetBackordersAsync(companyId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BackorderRow>>.Success(rows));
    }

    [HttpGet("shipment-register")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShipmentRegisterRow>>>> ShipmentRegisterAsync(
        [FromQuery] Guid? companyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetShipmentRegisterAsync(companyId.Value, from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentRegisterRow>>.Success(rows));
    }

    [HttpGet("sales-analysis")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesAnalysisRow>>>> SalesAnalysisAsync(
        [FromQuery] Guid? companyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetSalesAnalysisAsync(companyId.Value, from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalysisRow>>.Success(rows));
    }

    [HttpGet("credit-holds")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CreditHoldRow>>>> CreditHoldsAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetCreditHoldsAsync(companyId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CreditHoldRow>>.Success(rows));
    }

    [HttpGet("commission-runs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommissionRunRow>>>> CommissionRunsAsync(CancellationToken cancellationToken)
    {
        var runs = await _context.CommissionRuns.AsNoTracking()
            .OrderByDescending(r => r.PeriodStart)
            .Take(50)
            .ToListAsync(cancellationToken);
        var ids = runs.Select(r => r.Id).ToList();
        var lines = await _context.CommissionRunLines.AsNoTracking()
            .Where(l => ids.Contains(l.CommissionRunId))
            .ToListAsync(cancellationToken);
        var rows = runs.Select(r => new CommissionRunRow(
            r.Id, r.RunNumber, r.PeriodStart, r.PeriodEnd,
            lines.Count(l => l.CommissionRunId == r.Id),
            lines.Where(l => l.CommissionRunId == r.Id).Sum(l => l.RevenueBase),
            lines.Where(l => l.CommissionRunId == r.Id).Sum(l => l.CommissionAmount))).ToList();
        return Ok(ApiResponse<IReadOnlyList<CommissionRunRow>>.Success(rows));
    }

    [HttpGet("drop-ship-status")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DropShipStatusRow>>>> DropShipStatusAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetDropShipStatusAsync(companyId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DropShipStatusRow>>.Success(rows));
    }

    [HttpGet("sales-tax")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesTaxRow>>>> SalesTaxAsync(
        [FromQuery] Guid? companyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetSalesTaxAsync(companyId.Value, from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesTaxRow>>.Success(rows));
    }

    [HttpGet("sales-trend")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesTrendRow>>>> SalesTrendAsync(
        [FromQuery] Guid? companyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetSalesTrendAsync(companyId.Value, from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesTrendRow>>.Success(rows));
    }

    [HttpGet("customer-order-history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerOrderHistoryRow>>>> CustomerOrderHistoryAsync(
        [FromQuery] Guid? companyId, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        if (customerId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "customerId is required." }));
        var rows = await _reportService.GetCustomerOrderHistoryAsync(companyId.Value, customerId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerOrderHistoryRow>>.Success(rows));
    }

    [HttpGet("shipping-log")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShippingLogRow>>>> ShippingLogAsync(
        [FromQuery] Guid? companyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetShippingLogAsync(companyId.Value, from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShippingLogRow>>.Success(rows));
    }

    [HttpGet("freight-analysis")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FreightAnalysisRow>>>> FreightAnalysisAsync(
        [FromQuery] Guid? companyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetFreightAnalysisAsync(companyId.Value, from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FreightAnalysisRow>>.Success(rows));
    }

    // ---- PDF endpoints ----

    // ---- Resolution helpers (static) ----

    private static string CName(Dictionary<Guid, string> map, Guid? id)
    {
        return id.HasValue && map.TryGetValue(id.Value, out var name) ? name : id?.ToString()?.Substring(0, 8) ?? "—";
    }

    private static string IName(Dictionary<Guid, string> map, Guid id)
    {
        return map.TryGetValue(id, out var desc) ? desc : id.ToString().Substring(0, 8);
    }

    private static string OName(Dictionary<Guid, string> map, Guid? id)
    {
        return id.HasValue && map.TryGetValue(id.Value, out var num) ? num : id?.ToString()?.Substring(0, 8) ?? "—";
    }

    private static string VName(Dictionary<Guid, string> map, Guid? id)
    {
        return id.HasValue && map.TryGetValue(id.Value, out var name) ? name : id?.ToString()?.Substring(0, 8) ?? "—";
    }

    private async Task<Dictionary<Guid, string>> ResolveCustomerNamesAsync(IEnumerable<Guid?> ids, CancellationToken ct)
    {
        var distinct = ids.Where(id => id.HasValue && id.Value != Guid.Empty).Select(id => id!.Value).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var names = await _customerRead.GetCustomerNamesAsync(distinct, ct);
        return names.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private async Task<Dictionary<Guid, string>> ResolveItemDescriptionsAsync(IEnumerable<Guid> itemIds, CancellationToken ct)
    {
        var distinct = itemIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        // Item descriptions are stored on SalesOrderLine — the authoritative source
        // for what was actually sold.
        var descriptions = await _context.SalesOrderLines
            .AsNoTracking()
            .Where(l => distinct.Contains(l.ItemId))
            .GroupBy(l => l.ItemId)
            .Select(g => new { ItemId = g.Key, g.First().Description })
            .ToListAsync(ct);

        return descriptions.ToDictionary(d => d.ItemId, d => d.Description);
    }

    private async Task<Dictionary<Guid, string>> ResolveOrderNumbersAsync(IEnumerable<Guid?> orderIds, CancellationToken ct)
    {
        var distinct = orderIds.Where(id => id.HasValue && id.Value != Guid.Empty).Select(id => id!.Value).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var orders = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => distinct.Contains(o.Id))
            .Select(o => new { o.Id, o.OrderNumber })
            .ToListAsync(ct);

        return orders.ToDictionary(o => o.Id, o => o.OrderNumber);
    }

    private async Task<Dictionary<Guid, string>> ResolveVendorNamesAsync(IEnumerable<Guid?> ids, CancellationToken ct)
    {
        var distinct = ids.Where(id => id.HasValue && id.Value != Guid.Empty).Select(id => id!.Value).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var names = await _vendorRead.GetVendorNamesAsync(distinct, ct);
        return names.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    // ---- PDF endpoints ----

    [HttpGet("open-orders/pdf")]
    public async Task<ActionResult> OpenOrdersPdfAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetOpenOrdersAsync(companyId.Value, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Open Orders Report", "Open Orders", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.OrderNumber,
            CName(custNames, r.CustomerId),
            r.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Status,
            r.OrderedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.BackorderedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.OrderValue.ToString("C", CultureInfo.InvariantCulture),
            r.IsOnCreditHold ? "YES" : "—",
        }).ToArray();
        var doc = new ReportDocument(
            header,
            "Open Orders",
            $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Order", "Customer", "Date", "Status", "Ordered", "Backorder", "Value", "Hold" }, tableRows) },
            null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "OpenOrders.pdf");
    }

    [HttpGet("backorders/pdf")]
    public async Task<ActionResult> BackordersPdfAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetBackordersAsync(companyId.Value, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Backorders Report", "Backorders", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var itemNames = await ResolveItemDescriptionsAsync(rows.Select(r => r.ItemId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.OrderNumber,
            CName(custNames, r.CustomerId),
            IName(itemNames, r.ItemId),
            r.OrderedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.ShippedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.BackorderedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.UnitPrice.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var total = rows.Sum(r => r.BackorderedQty * r.UnitPrice);
        var doc = new ReportDocument(
            header,
            "Backorders",
            $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Order", "Customer", "Item", "Ordered", "Shipped", "Backorder", "Unit Price" }, tableRows) },
            total, "Total Backorder Value");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "Backorders.pdf");
    }

    [HttpGet("shipment-register/pdf")]
    public async Task<ActionResult> ShipmentRegisterPdfAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetShipmentRegisterAsync(companyId.Value, from, to, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Shipment Register", "Shipment Register", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var orderNames = await ResolveOrderNumbersAsync(rows.Select(r => r.SalesOrderId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.ShipmentNumber,
            OName(orderNames, r.SalesOrderId),
            CName(custNames, r.CustomerId),
            r.ShipDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Status,
            r.FreightCost.ToString("C", CultureInfo.InvariantCulture),
            r.ShipmentValue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var totalFreight = rows.Sum(r => r.FreightCost);
        var doc = new ReportDocument(
            header,
            "Shipment Register",
            $"From {from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"} to {to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"}  |  As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Shipment", "Order", "Customer", "Date", "Status", "Freight", "Value" }, tableRows) },
            totalFreight,
            "Total Freight");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ShipmentRegister.pdf");
    }

    [HttpGet("sales-analysis/pdf")]
    public async Task<ActionResult> SalesAnalysisPdfAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetSalesAnalysisAsync(companyId.Value, from, to, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Sales Analysis", "Sales Analysis", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var itemNames = await ResolveItemDescriptionsAsync(rows.Select(r => r.ItemId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            IName(itemNames, r.ItemId),
            CName(custNames, r.CustomerId),
            r.Quantity.ToString("N0", CultureInfo.InvariantCulture),
            r.ShippedQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.NetSales.ToString("C", CultureInfo.InvariantCulture),
            r.TaxAmount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var netSalesTotal = rows.Sum(r => r.NetSales);
        var doc = new ReportDocument(
            header,
            "Sales Analysis",
            $"From {from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"} to {to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"}  |  As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Customer", "Qty", "Shipped", "Net Sales", "Tax" }, tableRows) },
            netSalesTotal, "Total Net Sales");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SalesAnalysis.pdf");
    }

    [HttpGet("credit-holds/pdf")]
    public async Task<ActionResult> CreditHoldsPdfAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetCreditHoldsAsync(companyId.Value, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Credit Holds", "Credit Holds", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.OrderNumber,
            CName(custNames, r.CustomerId),
            r.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Status,
            r.Reason,
        }).ToArray();
        var doc = new ReportDocument(
            header,
            "Credit Holds",
            $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Order", "Customer", "Date", "Status", "Reason" }, tableRows) },
            null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CreditHolds.pdf");
    }

    [HttpGet("drop-ship-status/pdf")]
    public async Task<ActionResult> DropShipStatusPdfAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetDropShipStatusAsync(companyId.Value, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Drop-Ship Status", "Drop-Ship Status", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var itemNames = await ResolveItemDescriptionsAsync(rows.Select(r => r.ItemId), cancellationToken);
        var vendorNames = await ResolveVendorNamesAsync(rows.Select(r => r.DropShipVendorId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.OrderNumber,
            CName(custNames, r.CustomerId),
            IName(itemNames, r.ItemId),
            VName(vendorNames, r.DropShipVendorId),
            r.OrderedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.ShippedQty.ToString("N0", CultureInfo.InvariantCulture),
            r.BackorderedQty.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(
            header,
            "Drop-Ship Status",
            $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Order", "Customer", "Item", "Vendor", "Ordered", "Shipped", "Backorder" }, tableRows) },
            null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "DropShipStatus.pdf");
    }

    [HttpGet("sales-tax/pdf")]
    public async Task<ActionResult> SalesTaxPdfAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetSalesTaxAsync(companyId.Value, from, to, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Sales Tax Summary", "Sales Tax Summary", null, cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            $"{r.TaxPercent:N2}%",
            r.Quantity.ToString("N0", CultureInfo.InvariantCulture),
            r.TaxableAmount.ToString("C", CultureInfo.InvariantCulture),
            r.TaxAmount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var totalTax = rows.Sum(r => r.TaxAmount);
        var doc = new ReportDocument(
            header,
            "Sales Tax Summary",
            $"From {from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"} to {to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"}  |  As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Tax %", "Qty", "Taxable", "Tax" }, tableRows) },
            totalTax, "Total Tax");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SalesTax.pdf");
    }

    [HttpGet("sales-trend/pdf")]
    public async Task<ActionResult> SalesTrendPdfAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetSalesTrendAsync(companyId.Value, from, to, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Sales Trend", "Sales Trend", null, cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.Year.ToString(CultureInfo.InvariantCulture),
            new DateTime(r.Year, r.Month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("MMMM", CultureInfo.InvariantCulture),
            r.Quantity.ToString("N0", CultureInfo.InvariantCulture),
            r.NetSales.ToString("C", CultureInfo.InvariantCulture),
            r.TaxAmount.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var netSalesTotal = rows.Sum(r => r.NetSales);
        var doc = new ReportDocument(
            header,
            "Sales Trend",
            $"From {from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"} to {to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"}  |  As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Year", "Month", "Qty", "Net Sales", "Tax" }, tableRows) },
            netSalesTotal, "Total Net Sales");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SalesTrend.pdf");
    }

    [HttpGet("shipping-log/pdf")]
    public async Task<ActionResult> ShippingLogPdfAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetShippingLogAsync(companyId.Value, from, to, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Shipping Log", "Shipping Log", null, cancellationToken);
        var custNames = await ResolveCustomerNamesAsync(rows.Select(r => r.CustomerId), cancellationToken);
        var orderNames = await ResolveOrderNumbersAsync(rows.Select(r => r.SalesOrderId), cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.ShipmentNumber,
            OName(orderNames, r.SalesOrderId),
            CName(custNames, r.CustomerId),
            r.ShipDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Carrier,
            r.TrackingNumber,
            r.FreightCost.ToString("C", CultureInfo.InvariantCulture),
            r.ShipmentValue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var totalFreight = rows.Sum(r => r.FreightCost);
        var doc = new ReportDocument(
            header,
            "Shipping Log",
            $"From {from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"} to {to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"}  |  As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Shipment", "Order", "Customer", "Date", "Carrier", "Tracking", "Freight", "Value" }, tableRows) },
            totalFreight,
            "Total Freight");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ShippingLog.pdf");
    }

    [HttpGet("freight-analysis/pdf")]
    public async Task<ActionResult> FreightAnalysisPdfAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (companyId is null)
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        var rows = await _reportService.GetFreightAnalysisAsync(companyId.Value, from, to, cancellationToken);
        var header = await _customerRead.GetDocumentHeaderAsync(
            companyId.Value, null, "Freight Analysis", "Freight Analysis", null, cancellationToken);
        var tableRows = rows.Select(r => new[]
        {
            r.Carrier,
            r.ShipmentCount.ToString("N0", CultureInfo.InvariantCulture),
            r.FreightCost.ToString("C", CultureInfo.InvariantCulture),
            r.GoodsValue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var freightTotal = rows.Sum(r => r.FreightCost);
        var doc = new ReportDocument(
            header,
            "Freight Analysis",
            $"From {from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"} to {to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"}  |  As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Carrier", "Shipments", "Freight Cost", "Goods Value" }, tableRows) },
            freightTotal, "Total Freight Cost");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "FreightAnalysis.pdf");
    }
}
