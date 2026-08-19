// <copyright file="InventoryReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Application.Services;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Inventory.Api;

[ApiController]
[Route("api/v1/inventory/reports")]
public class InventoryReportsController : ControllerBase
{
    private readonly InventoryReportService _reports;

    public InventoryReportsController(InventoryReportService reports)
    {
        _reports = reports;
    }

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
}
