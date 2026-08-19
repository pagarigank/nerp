// <copyright file="SalesReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using ERP.Modules.OrderManagement.Application.Services;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/reports")]
public class SalesReportsController : ControllerBase
{
    private readonly SalesReportService _reportService;

    public SalesReportsController(SalesReportService reportService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
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
}
