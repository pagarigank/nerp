// <copyright file="InventoryReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.Inventory.Application.Services;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.Inventory.Api;

[ApiController]
[Route("api/v1/inventory/reports")]
public class InventoryReportsController : ControllerBase
{
    private readonly InventoryReportService _reports;
    private readonly ICustomerReadService _customerRead;

    public InventoryReportsController(InventoryReportService reports, ICustomerReadService customerRead)
    {
        _reports = reports;
        _customerRead = customerRead;
    }

    private async Task<CompanyDocumentHeader> GetHeaderAsync(Guid companyId, string title, string number, CancellationToken ct)
    {
        return await _customerRead.GetDocumentHeaderAsync(companyId, null, title, number, null, ct);
    }

    private static string S(string? val) => string.IsNullOrWhiteSpace(val) ? "—" : val;

    [HttpGet("valuation")]
    public async Task<ActionResult<ApiResponse<List<InventoryValuationRow>>>> Valuation(
        [FromQuery] Guid companyId, [FromQuery] Guid? warehouseId, [FromQuery] Guid? itemId, CancellationToken ct)
        => Ok(ApiResponse<List<InventoryValuationRow>>.Success(
            await _reports.GetValuationAsync(companyId, warehouseId, itemId, ct)));

    [HttpGet("reorder")]
    public async Task<ActionResult<ApiResponse<List<ReorderReportRow>>>> Reorder(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<ReorderReportRow>>.Success(await _reports.GetReorderReportAsync(companyId, ct)));

    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<List<TransactionHistoryRow>>>> Transactions(
        [FromQuery] Guid companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] Guid? itemId, [FromQuery] Guid? warehouseId, CancellationToken ct)
        => Ok(ApiResponse<List<TransactionHistoryRow>>.Success(
            await _reports.GetTransactionHistoryAsync(companyId, from, to, itemId, warehouseId, ct)));

    [HttpGet("stock-out")]
    public async Task<ActionResult<ApiResponse<List<StockOutRow>>>> StockOut(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<StockOutRow>>.Success(await _reports.GetStockOutReportAsync(companyId, ct)));

    [HttpGet("negative")]
    public async Task<ActionResult<ApiResponse<List<NegativeInventoryRow>>>> Negative(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<NegativeInventoryRow>>.Success(await _reports.GetNegativeInventoryReportAsync(companyId, ct)));

    [HttpGet("slow-moving")]
    public async Task<ActionResult<ApiResponse<List<SlowMovingRow>>>> SlowMoving(
        [FromQuery] Guid companyId, [FromQuery] int monthsThreshold = 12, CancellationToken ct = default)
        => Ok(ApiResponse<List<SlowMovingRow>>.Success(await _reports.GetSlowMovingReportAsync(companyId, monthsThreshold, ct)));

    [HttpGet("abc-analysis")]
    public async Task<ActionResult<ApiResponse<List<AbcAnalysisRow>>>> AbcAnalysis(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<AbcAnalysisRow>>.Success(await _reports.GetAbcAnalysisAsync(companyId, ct)));

    [HttpGet("lot-traceability")]
    public async Task<ActionResult<ApiResponse<List<LotTraceabilityRow>>>> LotTraceability(
        [FromQuery] Guid companyId, [FromQuery] Guid? itemId, CancellationToken ct)
        => Ok(ApiResponse<List<LotTraceabilityRow>>.Success(await _reports.GetLotTraceabilityAsync(companyId, itemId, ct)));

    [HttpGet("serial-traceability")]
    public async Task<ActionResult<ApiResponse<List<SerialTraceabilityRow>>>> SerialTraceability(
        [FromQuery] Guid companyId, [FromQuery] Guid? itemId, CancellationToken ct)
        => Ok(ApiResponse<List<SerialTraceabilityRow>>.Success(await _reports.GetSerialTraceabilityAsync(companyId, itemId, ct)));

    [HttpGet("inventory-turnover")]
    public async Task<ActionResult<ApiResponse<List<InventoryTurnoverRow>>>> InventoryTurnover(
        [FromQuery] Guid companyId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(ApiResponse<List<InventoryTurnoverRow>>.Success(await _reports.GetInventoryTurnoverAsync(companyId, from, to, ct)));

    [HttpGet("cycle-count-variance")]
    public async Task<ActionResult<ApiResponse<List<CycleCountVarianceRow>>>> CycleCountVariance(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<CycleCountVarianceRow>>.Success(await _reports.GetCycleCountVarianceAsync(companyId, ct)));

    [HttpGet("cycle-count-summary")]
    public async Task<ActionResult<ApiResponse<List<CycleCountSummaryRow>>>> CycleCountSummary(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<CycleCountSummaryRow>>.Success(await _reports.GetCycleCountSummaryAsync(companyId, ct)));

    [HttpGet("gl-tie-out")]
    public async Task<ActionResult<ApiResponse<List<InventoryGlTieOutRow>>>> GlTieOut(
        [FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<InventoryGlTieOutRow>>.Success(await _reports.GetGlTieOutAsync(companyId, ct)));

    [HttpGet("stock-card")]
    public async Task<ActionResult<ApiResponse<List<StockCardRow>>>> StockCard(
        [FromQuery] Guid companyId, [FromQuery] Guid itemId,
        [FromQuery] Guid? warehouseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResponse<List<StockCardRow>>.Success(
            await _reports.GetStockCardAsync(companyId, itemId, warehouseId, from, to, ct)));

    // ---- PDF endpoints ----

    [HttpGet("valuation/pdf")]
    public async Task<ActionResult> ValuationPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? warehouseId, [FromQuery] Guid? itemId, CancellationToken ct)
    {
        var rows = await _reports.GetValuationAsync(companyId, warehouseId, itemId, ct);
        var header = await GetHeaderAsync(companyId, "Inventory Valuation", "Valuation", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.WarehouseId.ToString().Substring(0, 8),
            r.OnHandQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.UnitCost.ToString("C", CultureInfo.InvariantCulture), r.ExtendedValue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var total = rows.Sum(r => r.ExtendedValue);
        var doc = new ReportDocument(header, "Inventory Valuation", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Description", "Warehouse", "On Hand", "Unit Cost", "Extended" }, tableRows) },
            total, "Total Valuation");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "InventoryValuation.pdf");
    }

    [HttpGet("reorder/pdf")]
    public async Task<ActionResult> ReorderPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetReorderReportAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Reorder Report", "Reorder", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.WarehouseId.ToString().Substring(0, 8),
            r.AvailableQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.ReorderPoint.ToString("N0", CultureInfo.InvariantCulture), r.SuggestedOrderQuantity.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Reorder Report", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Description", "Warehouse", "Available", "Reorder Pt", "Suggested" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ReorderReport.pdf");
    }

    [HttpGet("transactions/pdf")]
    public async Task<ActionResult> TransactionsPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? itemId, [FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var rows = await _reports.GetTransactionHistoryAsync(companyId, from, to, itemId, warehouseId, ct);
        var header = await GetHeaderAsync(companyId, "Transaction History", "Transactions", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.TransactionType, r.ItemId.ToString().Substring(0, 8), r.WarehouseId.ToString().Substring(0, 8),
            r.Quantity.ToString("N0", CultureInfo.InvariantCulture),
            r.UnitCost.ToString("C", CultureInfo.InvariantCulture), r.ExtendedCost.ToString("C", CultureInfo.InvariantCulture),
            r.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Transaction History", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Type", "Item", "Warehouse", "Qty", "Cost", "Extended", "Date" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "TransactionHistory.pdf");
    }

    [HttpGet("stock-out/pdf")]
    public async Task<ActionResult> StockOutPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetStockOutReportAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Stock-Out Report", "Stock-Out", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.WarehouseId.ToString().Substring(0, 8),
            r.OnHandQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.AllocatedQuantity.ToString("N0", CultureInfo.InvariantCulture), r.ReorderPoint.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Stock-Out Report", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Description", "Warehouse", "On Hand", "Allocated", "Reorder Pt" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "StockOutReport.pdf");
    }

    [HttpGet("negative/pdf")]
    public async Task<ActionResult> NegativePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetNegativeInventoryReportAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Negative Inventory Report", "Negative", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.WarehouseId.ToString().Substring(0, 8),
            r.OnHandQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.AllocatedQuantity.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Negative Inventory", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Description", "Warehouse", "On Hand", "Allocated" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "NegativeInventory.pdf");
    }

    [HttpGet("slow-moving/pdf")]
    public async Task<ActionResult> SlowMovingPdfAsync([FromQuery] Guid companyId, [FromQuery] int monthsThreshold = 12, CancellationToken ct = default)
    {
        var rows = await _reports.GetSlowMovingReportAsync(companyId, monthsThreshold, ct);
        var header = await GetHeaderAsync(companyId, "Slow-Moving Inventory", "Slow-Moving", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.WarehouseId.ToString().Substring(0, 8),
            r.OnHandQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.UnitCost.ToString("C", CultureInfo.InvariantCulture), r.OnHandValue.ToString("C", CultureInfo.InvariantCulture),
            r.LastMovementDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—",
        }).ToArray();
        var doc = new ReportDocument(header, "Slow-Moving Inventory", $"Threshold: {monthsThreshold} months",
            new[] { (new[] { "Item", "Description", "Warehouse", "On Hand", "Unit Cost", "Value", "Last Movement" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SlowMoving.pdf");
    }

    [HttpGet("abc-analysis/pdf")]
    public async Task<ActionResult> AbcAnalysisPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetAbcAnalysisAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "ABC Analysis", "ABC", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.UsageValue.ToString("C", CultureInfo.InvariantCulture),
            r.PercentOfTotal.ToString("F1") + "%", r.CumulativePercent.ToString("F1") + "%", r.ABCClass,
        }).ToArray();
        var doc = new ReportDocument(header, "ABC Analysis", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Description", "Usage Value", "% of Total", "Cumulative %", "Class" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "AbcAnalysis.pdf");
    }

    [HttpGet("lot-traceability/pdf")]
    public async Task<ActionResult> LotTraceabilityPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? itemId, CancellationToken ct)
    {
        var rows = await _reports.GetLotTraceabilityAsync(companyId, itemId, ct);
        var header = await GetHeaderAsync(companyId, "Lot Traceability", "Lot Trace", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.LotNumber), S(r.ItemCode), r.WarehouseId.ToString().Substring(0, 8),
            r.ReceivedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.ExpirationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—", r.Status,
            r.ReceivedQuantity.ToString("N0", CultureInfo.InvariantCulture), r.IssuedQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.RemainingQuantity.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Lot Traceability", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Lot", "Item", "Warehouse", "Received", "Expiration", "Status", "Recv Qty", "Issued", "Remaining" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "LotTraceability.pdf");
    }

    [HttpGet("serial-traceability/pdf")]
    public async Task<ActionResult> SerialTraceabilityPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid? itemId, CancellationToken ct)
    {
        var rows = await _reports.GetSerialTraceabilityAsync(companyId, itemId, ct);
        var header = await GetHeaderAsync(companyId, "Serial Traceability", "Serial Trace", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.SerialNo), S(r.ItemCode), r.WarehouseId.ToString().Substring(0, 8),
            r.ReceivedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Status, r.CustomerId?.ToString().Substring(0, 8) ?? "—",
            r.InstallationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—",
        }).ToArray();
        var doc = new ReportDocument(header, "Serial Traceability", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Serial", "Item", "Warehouse", "Received", "Status", "Customer", "Installed" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SerialTraceability.pdf");
    }

    [HttpGet("inventory-turnover/pdf")]
    public async Task<ActionResult> InventoryTurnoverPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var rows = await _reports.GetInventoryTurnoverAsync(companyId, from, to, ct);
        var header = await GetHeaderAsync(companyId, "Inventory Turnover", "Turnover", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.ItemCode), S(r.Description), r.Cogs.ToString("C", CultureInfo.InvariantCulture),
            r.AverageInventory.ToString("C", CultureInfo.InvariantCulture), r.Turnover.ToString("F2"),
        }).ToArray();
        var doc = new ReportDocument(header, "Inventory Turnover", $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
            new[] { (new[] { "Item", "Description", "COGS", "Avg Inventory", "Turnover" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "InventoryTurnover.pdf");
    }

    [HttpGet("cycle-count-variance/pdf")]
    public async Task<ActionResult> CycleCountVariancePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetCycleCountVarianceAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Cycle Count Variance", "Count Variance", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.CountNumber, S(r.ItemCode), r.WarehouseId?.ToString().Substring(0, 8) ?? "—",
            r.SystemQuantity.ToString("N0", CultureInfo.InvariantCulture), r.CountedQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.VarianceQuantity.ToString("N0", CultureInfo.InvariantCulture), r.VarianceValue.ToString("C", CultureInfo.InvariantCulture),
            r.VariancePercent.ToString("F2") + "%", S(r.Notes),
        }).ToArray();
        var doc = new ReportDocument(header, "Cycle Count Variance", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Count", "Item", "Warehouse", "System", "Counted", "Variance", "Variance $", "%", "Notes" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CycleCountVariance.pdf");
    }

    [HttpGet("cycle-count-summary/pdf")]
    public async Task<ActionResult> CycleCountSummaryPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetCycleCountSummaryAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "Cycle Count Summary", "Count Summary", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.WarehouseId.ToString().Substring(0, 8), r.LineCount.ToString(),
            r.TotalSystemQuantity.ToString("N0", CultureInfo.InvariantCulture), r.TotalCountedQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.TotalVarianceQuantity.ToString("N0", CultureInfo.InvariantCulture), r.TotalVarianceValue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Cycle Count Summary", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Warehouse", "Lines", "System", "Counted", "Variance", "Variance $" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CycleCountSummary.pdf");
    }

    [HttpGet("gl-tie-out/pdf")]
    public async Task<ActionResult> GlTieOutPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var rows = await _reports.GetGlTieOutAsync(companyId, ct);
        var header = await GetHeaderAsync(companyId, "GL Tie-Out", "GL Tie-Out", ct);
        var tableRows = rows.Select(r => new[]
        {
            S(r.GlAccountNumber), r.ItemCategoryId.ToString().Substring(0, 8),
            r.SubLedgerValue.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var totalVar = rows.Sum(r => r.SubLedgerValue);
        var doc = new ReportDocument(header, "GL Tie-Out", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "GL Account", "Category", "Sub-Ledger Value" }, tableRows) },
            totalVar, "Total Sub-Ledger");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "GlTieOut.pdf");
    }

    [HttpGet("stock-card/pdf")]
    public async Task<ActionResult> StockCardPdfAsync([FromQuery] Guid companyId, [FromQuery] Guid itemId, [FromQuery] Guid? warehouseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await _reports.GetStockCardAsync(companyId, itemId, warehouseId, from, to, ct);
        var header = await GetHeaderAsync(companyId, "Stock Card", "Stock Card", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), r.TransactionType,
            S(r.ReferenceNumber), r.QuantityIn.ToString("N0", CultureInfo.InvariantCulture),
            r.QuantityOut.ToString("N0", CultureInfo.InvariantCulture), r.RunningBalance.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Stock Card", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Date", "Type", "Reference", "Qty In", "Qty Out", "Running Balance" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "StockCard.pdf");
    }
}
