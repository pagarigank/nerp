// <copyright file="FulfillmentDocumentsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/fulfillment")]
public class FulfillmentDocumentsController : ControllerBase
{
    private readonly OmDbContext _context;

    public FulfillmentDocumentsController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Pick list for a sales order: the warehouse-facing document listing items to
    /// pick (item, qty, warehouse, order priority). No pricing is shown.
    /// </summary>
    [HttpGet("pick-list/{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<PickList>>> GetPickListAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _context.SalesOrders.AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales order {orderId} not found." }));

        var lines = order.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new PickListLine(
                l.ItemId,
                l.Description,
                l.Quantity,
                l.UnitOfMeasure,
                l.WarehouseId,
                l.Quantity - l.ShippedQuantity))
            .ToList();

        var doc = new PickList(
            order.Id,
            order.OrderNumber,
            order.CompanyId,
            order.CustomerId,
            order.Status.ToString(),
            order.OrderDate,
            lines);
        return Ok(ApiResponse<PickList>.Success(doc));
    }

    /// <summary>
    /// Packing slip for a shipment: the customer-facing document listing shipped
    /// items (description, qty, UoM) with no pricing detail.
    /// </summary>
    [HttpGet("packing-slip/{shipmentId:guid}")]
    public async Task<ActionResult<ApiResponse<PackingSlip>>> GetPackingSlipAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments.AsNoTracking()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);
        if (shipment is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Shipment {shipmentId} not found." }));

        var lines = shipment.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new PackingSlipLine(l.ItemId, l.Description, l.Quantity, l.UnitOfMeasure))
            .ToList();

        var doc = new PackingSlip(
            shipment.Id,
            shipment.ShipmentNumber,
            shipment.CompanyId,
            shipment.CustomerId,
            shipment.SalesOrderId,
            shipment.ShipmentDate,
            shipment.Carrier,
            shipment.TrackingNumber,
            lines);
        return Ok(ApiResponse<PackingSlip>.Success(doc));
    }
}

public sealed record PickList(
    Guid OrderId, string OrderNumber, Guid CompanyId, Guid? CustomerId,
    string Status, DateTime OrderDate, IReadOnlyList<PickListLine> Lines);

public sealed record PickListLine(
    Guid ItemId, string Description, decimal Quantity, string UnitOfMeasure,
    Guid? WarehouseId, decimal RemainingToPick);

public sealed record PackingSlip(
    Guid ShipmentId, string ShipmentNumber, Guid CompanyId, Guid? CustomerId,
    Guid? SalesOrderId, DateTime ShipmentDate, string? Carrier, string? TrackingNumber,
    IReadOnlyList<PackingSlipLine> Lines);

public sealed record PackingSlipLine(
    Guid ItemId, string Description, decimal Quantity, string UnitOfMeasure);
