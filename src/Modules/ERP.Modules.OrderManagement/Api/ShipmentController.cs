// <copyright file="ShipmentController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/shipments")]
public class ShipmentController : ControllerBase
{
    private readonly OmDbContext _context;

    public ShipmentController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ShipmentSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.Shipments.AsNoTracking();

        query = query.ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId);

        var list = await query
            .OrderByDescending(s => s.ShipmentDate)
            .Select(s => new ShipmentSummary(
                s.Id,
                s.ShipmentNumber,
                s.CompanyId,
                s.CustomerId,
                s.SalesOrderId,
                s.ShipmentDate,
                s.Status,
                s.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ShipmentSummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShipmentDetail>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments
            .AsNoTracking()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shipment is null)
            return NotFound(ApiResponse<ShipmentDetail>.Failure(new[] { $"Shipment {id} not found." }));

        var detail = new ShipmentDetail(
            shipment.Id,
            shipment.ShipmentNumber,
            shipment.CompanyId,
            shipment.CustomerId,
            shipment.SalesOrderId,
            shipment.ShipmentDate,
            shipment.Carrier,
            shipment.TrackingNumber,
            shipment.FreightCost,
            shipment.Status,
            shipment.Lines.Select(l => new ShipmentLineSummary(
                l.Id,
                l.LineNumber,
                l.ItemId,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.UnitOfMeasure,
                l.WarehouseId,
                l.SalesOrderLineId,
                l.ProjectId,
                l.AccountId,
                l.DiscountPercent,
                l.TaxPercent)).ToList());

        return Ok(ApiResponse<ShipmentDetail>.Success(detail));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync(
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var shipment = new Shipment(
            request.ShipmentNumber,
            request.CompanyId,
            request.CustomerId,
            request.SalesOrderId,
            request.ShipmentDate,
            request.Carrier,
            request.TrackingNumber,
            request.FreightCost);

        foreach (var line in request.Lines)
        {
            shipment.AddLine(new ShipmentLine(
                shipment.Id,
                line.LineNumber,
                line.ItemId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.UnitOfMeasure,
                line.WarehouseId,
                line.SalesOrderLineId,
                line.ProjectId,
                line.AccountId,
                line.DiscountPercent,
                line.TaxPercent));
        }

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(shipment.Id));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shipment is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Shipment {id} not found." }));

        try
        {
            shipment.Confirm();

            // Saving dispatches ShipmentConfirmedEvent, which the Inventory and AR
            // modules consume to relieve stock and generate the customer invoice.
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Confirmed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }
}

public record CreateShipmentRequest(
    string ShipmentNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTime ShipmentDate,
    string? Carrier,
    string? TrackingNumber,
    decimal FreightCost,
    List<CreateShipmentLineRequest> Lines);

public record CreateShipmentLineRequest(
    int LineNumber,
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitOfMeasure,
    Guid? WarehouseId,
    Guid? SalesOrderLineId,
    Guid? ProjectId,
    Guid? AccountId,
    decimal DiscountPercent,
    decimal TaxPercent);

public record ShipmentSummary(
    Guid Id,
    string ShipmentNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTime ShipmentDate,
    ShipmentStatus Status,
    decimal TotalAmount);

public record ShipmentLineSummary(
    Guid Id,
    int LineNumber,
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitOfMeasure,
    Guid? WarehouseId,
    Guid? SalesOrderLineId,
    Guid? ProjectId,
    Guid? AccountId,
    decimal DiscountPercent,
    decimal TaxPercent);

public record ShipmentDetail(
    Guid Id,
    string ShipmentNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTime ShipmentDate,
    string? Carrier,
    string? TrackingNumber,
    decimal FreightCost,
    ShipmentStatus Status,
    List<ShipmentLineSummary> Lines);
