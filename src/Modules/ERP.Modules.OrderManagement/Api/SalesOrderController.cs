// <copyright file="SalesOrderController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Core.Common;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
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

        if (companyId is not null)
            query = query.Where(o => o.CompanyId == companyId);

        var list = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new SalesOrderSummary(
                o.Id,
                o.OrderNumber,
                o.CompanyId,
                o.CustomerId,
                o.OrderDate,
                o.Status,
                o.Lines.Sum(l => (l.Quantity * l.UnitPrice) * ((1m - (l.DiscountPercent / 100m)) * (1m + (l.TaxPercent / 100m))))))
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
                l.IsDropShip,
                l.DropShipVendorId,
                l.ShippedQuantity,
                l.LineTotal)).ToList());

        return Ok(ApiResponse<SalesOrderDetail>.Success(detail));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync(
        [FromBody] CreateSalesOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = new SalesOrder(
            request.OrderNumber,
            request.CompanyId,
            request.CustomerId,
            request.OrderDate,
            request.ShipToAddress,
            request.BillToAddress,
            request.PaymentTermId,
            request.SalesRepId,
            request.ShippingMethod,
            request.CustomerPoNumber);

        foreach (var line in request.Lines)
        {
            order.AddLine(new SalesOrderLine(
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
                line.AccountId));
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
                request.Description);

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Line updated"));
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
    Guid? AccountId);

public record UpdateSalesOrderLineRequest(
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    Guid? WarehouseId,
    Guid? ProjectId,
    Guid? AccountId,
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
    decimal TotalAmount);

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
    bool IsDropShip,
    Guid? DropShipVendorId,
    decimal ShippedQuantity,
    decimal LineTotal);

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
    SalesOrderStatus Status,
    bool IsOnCreditHold,
    bool RequiresDiscountApproval,
    bool DiscountApproved,
    List<SalesOrderLineSummary> Lines);
