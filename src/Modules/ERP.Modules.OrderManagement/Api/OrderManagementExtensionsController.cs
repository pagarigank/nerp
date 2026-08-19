// <copyright file="OrderManagementExtensionsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Core.Common;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

#pragma warning disable S6960 // Controller groups related Phase 8 gap endpoints; splitting would fragment a single feature area.

namespace ERP.Modules.OrderManagement.Api;

/// <summary>
/// Phase 8 gap-addition endpoints: quote-to-order conversion, blanket/standing
/// orders, backorder substitution offers, return-to-vendor, order notes &amp; change
/// history, customer acknowledgment document, order-status dashboard, freight
/// allocation, and Available-to-Promise (ATP).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om")]
public class OrderManagementExtensionsController : ControllerBase
{
    private readonly OmDbContext _context;
    private readonly IInventoryAvailability _inventoryAvailability;
    private readonly ICurrentUserService _currentUser;

    public OrderManagementExtensionsController(
        OmDbContext context,
        IInventoryAvailability inventoryAvailability,
        ICurrentUserService currentUser)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _inventoryAvailability = inventoryAvailability ?? throw new ArgumentNullException(nameof(inventoryAvailability));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Returns a 404 result when <paramref name="entity"/> is null, otherwise null (caller proceeds).</summary>
    private NotFoundObjectResult? Missing<T>(T? entity, string message) where T : class =>
        entity is null ? NotFound(ApiResponse<T>.Failure(new[] { message })) : null;

    // ----- Quote-to-order conversion (582)
    [HttpPost("sales-orders/{id:guid}/configure-quote")]
    public async Task<ActionResult<ApiResponse<Guid>>> ConfigureQuoteAsync(Guid id, [FromBody] ConfigureQuoteRequest request, CancellationToken ct)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        order!.ConfigureAsQuote(request.ExpiryDate);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("sales-orders/{id:guid}/send-quote")]
    public async Task<ActionResult<ApiResponse<Guid>>> SendQuoteAsync(Guid id, CancellationToken ct)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        order!.SendQuote();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("sales-orders/{id:guid}/accept-quote")]
    public async Task<ActionResult<ApiResponse<Guid>>> AcceptQuoteAsync(Guid id, CancellationToken ct)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        order!.AcceptQuote();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("sales-orders/{id:guid}/reject-quote")]
    public async Task<ActionResult<ApiResponse<Guid>>> RejectQuoteAsync(Guid id, CancellationToken ct)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        order!.RejectQuote();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("sales-orders/{id:guid}/revise-quote")]
    public async Task<ActionResult<ApiResponse<Guid>>> ReviseQuoteAsync(Guid id, CancellationToken ct)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        order!.ReviseQuote();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("sales-orders/{id:guid}/convert-quote")]
    public async Task<ActionResult<ApiResponse<Guid>>> ConvertQuoteAsync(Guid id, [FromBody] ConvertQuoteRequest request, CancellationToken ct)
    {
        var order = await _context.SalesOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        var newOrder = order!.ConvertToOrder(request.NewOrderNumber);
        _context.SalesOrders.Add(newOrder);
        _context.SalesOrders.Update(order);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(newOrder.Id));
    }

    // ----- Blanket / standing orders (583)
    [HttpPost("blanket-orders")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBlanketOrderAsync([FromBody] CreateBlanketOrderRequest request, CancellationToken ct)
    {
        var bo = new BlanketSalesOrder(
            request.OrderNumber,
            request.CompanyId,
            request.CustomerId,
            request.OrderDate,
            request.TotalQuantity,
            request.TotalValue,
            request.ValidFrom,
            request.ValidTo,
            request.Currency);
        _context.BlanketSalesOrders.Add(bo);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(bo.Id));
    }

    [HttpGet("blanket-orders")]
    public async Task<ActionResult<ApiResponse<List<BlanketOrderSummary>>>> GetBlanketOrdersAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var q = _context.BlanketSalesOrders.AsNoTracking();
        if (companyId is not null)
        {
            q = q.Where(x => x.CompanyId == companyId);
        }

        var list = await q.OrderByDescending(x => x.OrderDate)
            .Select(x => new BlanketOrderSummary(x.Id, x.OrderNumber, x.CompanyId, x.CustomerId, x.TotalQuantity, x.TotalValue, x.ReleasedQuantity, x.RemainingQuantity, x.ValidFrom, x.ValidTo, x.Status))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<BlanketOrderSummary>>.Success(list));
    }

    [HttpPost("blanket-orders/{id:guid}/releases")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddReleaseAsync(Guid id, [FromBody] AddReleaseRequest request, CancellationToken ct)
    {
        var bo = await _context.BlanketSalesOrders.Include(x => x.Releases).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (Missing(bo, $"Blanket order {id} not found.") is { } r)
        {
            return r;
        }

        var release = bo!.AddRelease(request.Quantity, request.Value, request.ReleaseDate, request.Reference);
        _context.BlanketReleases.Add(release);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(release.Id));
    }

    // ----- Backorder substitution offers (584)
    [HttpPost("substitution-offers")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateSubstitutionOfferAsync([FromBody] CreateSubstitutionOfferRequest request, CancellationToken ct)
    {
        var offer = new BackorderSubstitutionOffer(
            request.CompanyId,
            request.SalesOrderId,
            request.SalesOrderLineId,
            request.OriginalItemId,
            request.SubstituteItemId,
            request.Quantity,
            request.ApprovedUnitPrice,
            request.Reason);
        _context.BackorderSubstitutionOffers.Add(offer);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(offer.Id));
    }

    [HttpPost("substitution-offers/{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<Guid>>> AcceptOfferAsync(Guid id, CancellationToken ct)
    {
        var offer = await _context.BackorderSubstitutionOffers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (Missing(offer, $"Offer {id} not found.") is { } r)
        {
            return r;
        }

        offer!.Accept();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(offer.Id));
    }

    [HttpPost("substitution-offers/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<Guid>>> RejectOfferAsync(Guid id, [FromBody] RejectOfferRequest request, CancellationToken ct)
    {
        var offer = await _context.BackorderSubstitutionOffers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (Missing(offer, $"Offer {id} not found.") is { } r)
        {
            return r;
        }

        offer!.Reject(request.Reason);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(offer.Id));
    }

    [HttpGet("substitution-offers")]
    public async Task<ActionResult<ApiResponse<List<SubstitutionOfferSummary>>>> GetOffersAsync([FromQuery] Guid? companyId, [FromQuery] Guid? salesOrderId, CancellationToken ct)
    {
        var q = _context.BackorderSubstitutionOffers.AsNoTracking();
        if (companyId is not null)
        {
            q = q.Where(x => x.CompanyId == companyId);
        }

        if (salesOrderId is not null)
        {
            q = q.Where(x => x.SalesOrderId == salesOrderId);
        }

        var list = await q.OrderByDescending(x => x.CreatedOn)
            .Select(x => new SubstitutionOfferSummary(x.Id, x.SalesOrderId, x.OriginalItemId, x.SubstituteItemId, x.Quantity, x.ApprovedUnitPrice, x.Status))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<SubstitutionOfferSummary>>.Success(list));
    }

    // ----- Return-to-vendor (585)
    [HttpPost("returns/{returnId:guid}/rtv")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRtvAsync(Guid returnId, [FromBody] CreateRtvRequest request, CancellationToken ct)
    {
        var rtv = new ReturnToVendor(
            request.CompanyId,
            returnId,
            request.ReturnLineId,
            request.VendorId,
            request.Quantity,
            request.UnitCost,
            request.Reference);
        _context.ReturnToVendors.Add(rtv);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(rtv.Id));
    }

    [HttpPost("rtv/{id:guid}/ship")]
    public async Task<ActionResult<ApiResponse<Guid>>> ShipRtvAsync(Guid id, CancellationToken ct)
    {
        var rtv = await _context.ReturnToVendors.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (Missing(rtv, $"RTV {id} not found.") is { } r)
        {
            return r;
        }

        rtv!.MarkShippedToVendor();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(rtv.Id));
    }

    [HttpPost("rtv/{id:guid}/credit")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreditRtvAsync(Guid id, CancellationToken ct)
    {
        var rtv = await _context.ReturnToVendors.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (Missing(rtv, $"RTV {id} not found.") is { } r)
        {
            return r;
        }

        rtv!.ReceiveVendorCredit();
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(rtv.Id));
    }

    // ----- Order notes + change history (589)
    [HttpPost("sales-orders/{id:guid}/notes")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddNoteAsync(Guid id, [FromBody] AddNoteRequest request, CancellationToken ct)
    {
        var note = new SalesOrderNote(
            request.CompanyId,
            id,
            request.Text,
            request.IsCustomerFacing,
            request.NoteType,
            request.AttachmentLink,
            _currentUser.UserId);
        _context.SalesOrderNotes.Add(note);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(note.Id));
    }

    [HttpGet("sales-orders/{id:guid}/notes")]
    public async Task<ActionResult<ApiResponse<List<SalesOrderNoteSummary>>>> GetNotesAsync(Guid id, CancellationToken ct)
    {
        var list = await _context.SalesOrderNotes.AsNoTracking()
            .Where(n => n.SalesOrderId == id)
            .OrderByDescending(n => n.CreatedOn)
            .Select(n => new SalesOrderNoteSummary(n.Id, n.SalesOrderId, n.Text, n.IsCustomerFacing, n.NoteType, n.AttachmentLink, n.CreatedBy, n.CreatedOn))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<SalesOrderNoteSummary>>.Success(list));
    }

    [HttpPost("sales-orders/{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<Guid>>> RecordHistoryAsync(Guid id, [FromBody] RecordHistoryRequest request, CancellationToken ct)
    {
        var h = new SalesOrderChangeHistory(
            id,
            request.CompanyId,
            _currentUser.UserId ?? "system",
            request.ChangeType,
            request.FieldName,
            request.OldValue,
            request.NewValue,
            request.ReasonCode);
        _context.SalesOrderChangeHistories.Add(h);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(h.Id));
    }

    [HttpGet("sales-orders/{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<List<SalesOrderChangeHistorySummary>>>> GetHistoryAsync(Guid id, CancellationToken ct)
    {
        var list = await _context.SalesOrderChangeHistories.AsNoTracking()
            .Where(h => h.SalesOrderId == id)
            .OrderByDescending(h => h.ChangeDate)
            .Select(h => new SalesOrderChangeHistorySummary(h.Id, h.ChangeType, h.FieldName, h.OldValue, h.NewValue, h.ReasonCode, h.ChangedBy, h.ChangeDate))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<SalesOrderChangeHistorySummary>>.Success(list));
    }

    // ----- Customer order acknowledgment document (588)
    [HttpGet("sales-orders/{id:guid}/acknowledgment")]
    public async Task<ActionResult<ApiResponse<AcknowledgmentDocument>>> GetAcknowledgmentAsync(Guid id, CancellationToken ct)
    {
        var order = await _context.SalesOrders.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        var doc = new AcknowledgmentDocument(
            order!.OrderNumber,
            order.CustomerId,
            order.OrderDate,
            order.Lines.Select(l => new AcknowledgmentLine(l.ItemId, l.Description, l.Quantity, l.UnitPrice, l.UnitOfMeasure)).ToList());
        return Ok(ApiResponse<AcknowledgmentDocument>.Success(doc));
    }

    // ----- Sales order status dashboard (587)
    [HttpGet("dashboard/order-status")]
    public async Task<ActionResult<ApiResponse<List<OrderStatusRow>>>> GetOrderStatusDashboardAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var q = _context.SalesOrders.AsNoTracking();
        if (companyId is not null)
        {
            q = q.Where(x => x.CompanyId == companyId);
        }

        var projected = await q.Select(x => new { x.Status, x.RemainingToShip }).ToListAsync(ct);
        var rows = projected
            .GroupBy(x => x.Status)
            .Select(g => new OrderStatusRow(g.Key.ToString(), g.Count(), g.Sum(o => o.RemainingToShip)))
            .ToList();
        return Ok(ApiResponse<List<OrderStatusRow>>.Success(rows));
    }

    // ----- Freight allocation (578)
    [HttpPost("sales-orders/{id:guid}/allocate-freight")]
    public async Task<ActionResult<ApiResponse<Guid>>> AllocateFreightAsync(Guid id, [FromBody] AllocateFreightRequest request, CancellationToken ct)
    {
        var order = await _context.SalesOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (Missing(order, $"Sales order {id} not found.") is { } r)
        {
            return r;
        }

        var totalValue = order!.Lines.Sum(l => l.Quantity * l.UnitPrice);
        if (totalValue <= 0)
        {
            return BadRequest(ApiResponse<Guid>.Failure(new[] { "Order has no lines to allocate freight across." }));
        }

        foreach (var line in order.Lines)
        {
            var share = (line.Quantity * line.UnitPrice) / totalValue;
            line.AllocateFreight(request.FreightAmount * share);
        }

        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    // ----- Available-to-Promise (581)
    [HttpGet("atp")]
    public async Task<ActionResult<ApiResponse<AtpResult>>> GetAtpAsync(
        [FromQuery] Guid itemId, [FromQuery] Guid warehouseId, [FromQuery] decimal quantity, CancellationToken ct)
    {
        var availability = await _inventoryAvailability.CheckAsync(itemId, warehouseId, quantity, ct);
        var promisedDate = availability.IsSufficient
            ? DateTime.UtcNow.Date
            : DateTime.UtcNow.Date.AddDays(7); // simplistic lead-time fallback
        var result = new AtpResult(itemId, warehouseId, quantity, availability.Available, availability.IsSufficient, promisedDate);
        return Ok(ApiResponse<AtpResult>.Success(result));
    }
}

// Request / response records
public record ConfigureQuoteRequest(DateTime? ExpiryDate);
public record ConvertQuoteRequest(string NewOrderNumber);
public record CreateBlanketOrderRequest(Guid CompanyId, string OrderNumber, Guid CustomerId, DateTime OrderDate, decimal TotalQuantity, decimal TotalValue, DateTime ValidFrom, DateTime ValidTo, string? Currency);
public record AddReleaseRequest(decimal Quantity, decimal Value, DateTime ReleaseDate, string? Reference);
public record CreateSubstitutionOfferRequest(Guid CompanyId, Guid SalesOrderId, Guid SalesOrderLineId, Guid OriginalItemId, Guid SubstituteItemId, decimal Quantity, decimal ApprovedUnitPrice, string? Reason);
public record RejectOfferRequest(string? Reason);
public record CreateRtvRequest(Guid CompanyId, Guid ReturnLineId, Guid VendorId, decimal Quantity, decimal UnitCost, string? Reference);
public record AddNoteRequest(Guid CompanyId, string Text, bool IsCustomerFacing, string NoteType, string? AttachmentLink);
public record RecordHistoryRequest(Guid CompanyId, string ChangeType, string? FieldName, string? OldValue, string? NewValue, string? ReasonCode);
public record AllocateFreightRequest(decimal FreightAmount);

public record BlanketOrderSummary(Guid Id, string OrderNumber, Guid CompanyId, Guid CustomerId, decimal TotalQuantity, decimal TotalValue, decimal ReleasedQuantity, decimal RemainingQuantity, DateTime ValidFrom, DateTime ValidTo, BlanketOrderStatus Status);
public record SubstitutionOfferSummary(Guid Id, Guid SalesOrderId, Guid OriginalItemId, Guid SubstituteItemId, decimal Quantity, decimal ApprovedUnitPrice, SubstitutionOfferStatus Status);
public record SalesOrderNoteSummary(Guid Id, Guid SalesOrderId, string Text, bool IsCustomerFacing, string NoteType, string? AttachmentLink, string? CreatedBy, DateTimeOffset CreatedOn);
public record SalesOrderChangeHistorySummary(Guid Id, string ChangeType, string? FieldName, string? OldValue, string? NewValue, string? ReasonCode, string? ChangedBy, DateTimeOffset ChangeDate);
public record AcknowledgmentLine(Guid ItemId, string Description, decimal Quantity, decimal UnitPrice, string UnitOfMeasure);
public record AcknowledgmentDocument(string OrderNumber, Guid CustomerId, DateTime OrderDate, List<AcknowledgmentLine> Lines);
public record OrderStatusRow(string Status, int OrderCount, decimal RemainingToShip);
public record AtpResult(Guid ItemId, Guid WarehouseId, decimal RequestedQuantity, decimal Available, bool IsSufficient, DateTime PromisedDate);
