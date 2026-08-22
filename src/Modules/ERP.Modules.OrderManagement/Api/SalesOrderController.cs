// <copyright file="SalesOrderController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Core.Common;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Domain.Services;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/sales-orders")]
public class SalesOrderController : ControllerBase
{
    private readonly OmDbContext _context;
    private readonly ICreditLimitCheck _creditLimitCheck;
    private readonly IInventoryAvailability _inventoryAvailability;

    public SalesOrderController(
        OmDbContext context,
        ICreditLimitCheck creditLimitCheck,
        IInventoryAvailability inventoryAvailability)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _creditLimitCheck = creditLimitCheck ?? throw new ArgumentNullException(nameof(creditLimitCheck));
        _inventoryAvailability = inventoryAvailability ?? throw new ArgumentNullException(nameof(inventoryAvailability));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalesOrderSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.SalesOrders.AsNoTracking();

        query = query.ApplyCompanyScope(HttpContext, o => o.CompanyId, companyId);

        var list = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new SalesOrderSummary(
                o.Id,
                o.OrderNumber,
                o.CompanyId,
                o.CustomerId,
                o.OrderDate,
                o.Status,
                o.Lines.Sum(l => (l.Quantity * l.UnitPrice) * ((1m - (l.DiscountPercent / 100m)) * (1m + (l.TaxPercent / 100m)))),
                o.Lines.Count(l => l.IsDropShip && l.DropShipConfirmedOn == null),
                o.Lines
                    .Where(l => l.IsDropShip && l.DropShipConfirmedOn == null)
                    .OrderBy(l => l.LineNumber)
                    .Select(l => (Guid?)l.Id)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<SalesOrderSummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesOrderDetail>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return NotFound(ApiResponse<SalesOrderDetail>.Failure(new[] { $"Sales order {id} not found." }));

        var detail = new SalesOrderDetail(
            order.Id,
            order.OrderNumber,
            order.CompanyId,
            order.CustomerId,
            order.OrderDate,
            order.ShipToAddress,
            order.BillToAddress,
            order.PaymentTermId,
            order.SalesRepId,
            order.ShippingMethod,
            order.CustomerPoNumber,
            order.SalesOrderTypeId,
            order.TaxCodeId,
            order.TaxExemptionCertificateId,
            order.Status,
            order.IsOnCreditHold,
            order.RequiresDiscountApproval,
            order.DiscountApproved,
            order.Lines.Select(l => new SalesOrderLineSummary(
                l.Id,
                l.LineNumber,
                l.ItemId,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.UnitOfMeasure,
                l.DiscountPercent,
                l.TaxPercent,
                l.WarehouseId,
                l.ProjectId,
                l.AccountId,
                l.ItemCategoryId,
                l.IsDropShip,
                l.DropShipVendorId,
                l.ShippedQuantity,
                l.LineTotal,
                l.AppliedPricingRuleId,
                l.DropShipConfirmedOn)).ToList());

        return Ok(ApiResponse<SalesOrderDetail>.Success(detail));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync(
        [FromBody] CreateSalesOrderRequest request,
        CancellationToken cancellationToken)
    {
        // Sales person, tax code and tax-exemption are maintained on the Customer
        // master; inherit them onto the order unless explicitly overridden here.
        // Pricing is applied per-line automatically from the pricing-rule master,
        // so there is intentionally no pricing-rule field on the order header.
        // (AR owns the Customer entity; we read only the three defaults via a
        // lightweight SQL projection to avoid a cross-module project reference.)
        string? salesRepId = request.SalesRepId;
        Guid? taxCodeId = request.TaxCodeId;
        Guid? taxExemptionCertificateId = request.TaxExemptionCertificateId;
        if (salesRepId is null || taxCodeId is null || taxExemptionCertificateId is null)
        {
            var conn = _context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SalesRepId, TaxCodeId, TaxExemptionCertificateId FROM ar.Customers WHERE Id = @cid";
            var p = cmd.CreateParameter();
            p.ParameterName = "@cid";
            p.Value = request.CustomerId;
            cmd.Parameters.Add(p);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                if (salesRepId is null && !await reader.IsDBNullAsync(0, cancellationToken))
                    salesRepId = reader.GetGuid(0).ToString();
                if (taxCodeId is null && !await reader.IsDBNullAsync(1, cancellationToken))
                    taxCodeId = reader.GetGuid(1);
                if (taxExemptionCertificateId is null && !await reader.IsDBNullAsync(2, cancellationToken))
                    taxExemptionCertificateId = reader.GetGuid(2);
            }
        }

        var order = new SalesOrder(
            request.OrderNumber,
            request.CompanyId,
            request.CustomerId,
            request.OrderDate,
            request.ShipToAddress,
            request.BillToAddress,
            request.PaymentTermId,
            salesRepId,
            request.ShippingMethod,
            request.CustomerPoNumber,
            request.SalesOrderTypeId,
            taxCodeId,
            taxExemptionCertificateId);

        // Load the company's active pricing rules once and auto-apply the winning
        // rule to each line (by customer / item / item-category / quantity / date).
        var pricingRules = await _context.PricingRules
            .AsNoTracking()
            .Where(r => r.CompanyId == request.CompanyId)
            .ToListAsync(cancellationToken);

        foreach (var line in request.Lines)
        {
            var lineEntity = new SalesOrderLine(
                order.Id,
                line.LineNumber,
                line.ItemId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.UnitOfMeasure,
                line.DiscountPercent,
                line.TaxPercent,
                line.WarehouseId,
                line.ProjectId,
                line.AccountId,
                line.ItemCategoryId);

            var result = PricingEngine.CalculatePrice(
                line.UnitPrice,
                request.CustomerId,
                line.ItemId,
                line.ItemCategoryId,
                line.Quantity,
                pricingRules,
                request.OrderDate);
            lineEntity.SetPricingApplied(result.UnitPrice, result.DiscountPercent, result.AppliedRuleId);

            order.AddLine(lineEntity);
        }

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        if (order.IsOnCreditHold)
            return BadRequest(ApiResponse<string>.Failure(new[] { $"Order is on credit hold: {order.CreditHoldReason}" }));

        // Real-time credit-limit check (Purchase -> Inventory -> Sales integration):
        // the order's extended total must fit within the customer's available credit.
        var orderTotal = order.Lines.Sum(l => l.LineTotal);
        var credit = await _creditLimitCheck.CheckAsync(order.CustomerId, orderTotal, cancellationToken);
        if (!credit.IsApproved)
            return BadRequest(ApiResponse<string>.Failure(new[] { credit.Message ?? "Order exceeds available credit." }));

        // Real-time availability check: every line must have enough available stock.
        foreach (var line in order.Lines)
        {
            if (line.WarehouseId is null)
                continue;

            var avail = await _inventoryAvailability.CheckAsync(line.ItemId, line.WarehouseId.Value, line.Quantity, cancellationToken);
            if (!avail.IsSufficient)
            {
                return BadRequest(ApiResponse<string>.Failure(new[]
                {
                    $"Item {line.ItemId} has {avail.Available} available but order requires {line.Quantity}."
                }));
            }
        }

        try
        {
            order.Confirm();
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Confirmed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<string>>> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        order.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Cancelled"));
    }

    [HttpPost("{id:guid}/credit-hold")]
    public async Task<ActionResult<ApiResponse<string>>> PlaceCreditHoldAsync(
        Guid id, [FromBody] CreditHoldRequest request, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        try
        {
            order.PlaceCreditHold(request.Reason);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Credit hold placed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }

    [HttpPost("{id:guid}/release-hold")]
    public async Task<ActionResult<ApiResponse<string>>> ReleaseCreditHoldAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        order.ReleaseCreditHold();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Credit hold released"));
    }

    /// <summary>
    /// Approves an order whose line discounts exceed the manager-approval threshold
    /// (see <see cref="SalesOrder.DiscountApprovalThreshold"/>). Required before the
    /// order can be confirmed.
    /// </summary>
    [HttpPost("{id:guid}/discount-approval")]
    public async Task<ActionResult<ApiResponse<string>>> ApproveDiscountAsync(
        Guid id, [FromBody] DiscountApprovalRequest request, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        if (!order.RequiresDiscountApproval)
            return BadRequest(ApiResponse<string>.Failure(new[] { "Order has no discount requiring approval." }));

        order.MarkDiscountApproved(request.ApprovedBy);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Discount approved"));
    }

    // Change-order edit: update a single draft line (qty / price / distribution).
    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateLineAsync(
        Guid id,
        Guid lineId,
        [FromBody] UpdateSalesOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        try
        {
            order.UpdateLine(
                lineId,
                request.Quantity,
                request.UnitPrice,
                request.DiscountPercent,
                request.TaxPercent,
                request.WarehouseId,
                request.ProjectId,
                request.AccountId,
                request.ItemCategoryId,
                request.Description);

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Line updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Vendor drop-ship confirmation (Phase 6 gap 389): records that the vendor
    /// shipped the given drop-ship line directly to the customer.
    /// </summary>
    [HttpPost("{id:guid}/lines/{lineId:guid}/confirm-drop-ship")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmDropShipAsync(
        Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {id} not found." }));

        try
        {
            order.ConfirmDropShipLine(lineId);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Drop-ship confirmed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }
}

public record CreateSalesOrderRequest(
    string OrderNumber,
    Guid CompanyId,
    Guid CustomerId,
    DateTime OrderDate,
    string? ShipToAddress,
    string? BillToAddress,
    string? PaymentTermId,
    string? SalesRepId,
    string? ShippingMethod,
    string? CustomerPoNumber,
    Guid? SalesOrderTypeId,
    Guid? TaxCodeId,
    Guid? TaxExemptionCertificateId,
    List<CreateSalesOrderLineRequest> Lines);

public record CreateSalesOrderLineRequest(
    int LineNumber,
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitOfMeasure,
    decimal DiscountPercent,
    decimal TaxPercent,
    Guid? WarehouseId,
    Guid? ProjectId,
    Guid? AccountId,
    Guid? ItemCategoryId,
    bool IsDropShip,
    Guid? DropShipVendorId);

public record UpdateSalesOrderLineRequest(
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    Guid? WarehouseId,
    Guid? ProjectId,
    Guid? AccountId,
    Guid? ItemCategoryId,
    string? Description);

public record CreditHoldRequest(string Reason);

public record DiscountApprovalRequest(string ApprovedBy);

public record SalesOrderSummary(
    Guid Id,
    string OrderNumber,
    Guid CompanyId,
    Guid CustomerId,
    DateTime OrderDate,
    SalesOrderStatus Status,
    decimal TotalAmount,
    int PendingDropShipCount,
    Guid? FirstPendingDropShipLineId);

public record SalesOrderLineSummary(
    Guid Id,
    int LineNumber,
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitOfMeasure,
    decimal DiscountPercent,
    decimal TaxPercent,
    Guid? WarehouseId,
    Guid? ProjectId,
    Guid? AccountId,
    Guid? ItemCategoryId,
    bool IsDropShip,
    Guid? DropShipVendorId,
    decimal ShippedQuantity,
    decimal LineTotal,
    Guid? AppliedPricingRuleId,
    DateTimeOffset? DropShipConfirmedOn);

public record SalesOrderDetail(
    Guid Id,
    string OrderNumber,
    Guid CompanyId,
    Guid CustomerId,
    DateTime OrderDate,
    string? ShipToAddress,
    string? BillToAddress,
    string? PaymentTermId,
    string? SalesRepId,
    string? ShippingMethod,
    string? CustomerPoNumber,
    Guid? SalesOrderTypeId,
    Guid? TaxCodeId,
    Guid? TaxExemptionCertificateId,
    SalesOrderStatus Status,
    bool IsOnCreditHold,
    bool RequiresDiscountApproval,
    bool DiscountApproved,
    List<SalesOrderLineSummary> Lines);
