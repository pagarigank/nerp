// <copyright file="ShipmentConfirmedToOmHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Domain.Events;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Infrastructure.Handlers;

/// <summary>
/// Order Management's own leg of the shipment flow. When a shipment is confirmed the
/// Inventory and AR modules relieve stock and raise the invoice, but OM must also
/// record that the sales order's lines were shipped (driving the backorder quantity and
/// the order status). This handler is co-located with the OM module so it can update the
/// <see cref="SalesOrder"/> aggregate directly, keeping backorder state authoritative in OM
/// (no cross-module reference cycle: OM consumes its own event).
/// </summary>
public sealed class ShipmentConfirmedToOmHandler : IDomainEventHandler<ShipmentConfirmedEvent>
{
    private readonly OmDbContext _omContext;

    public ShipmentConfirmedToOmHandler(OmDbContext omContext)
    {
        _omContext = omContext ?? throw new ArgumentNullException(nameof(omContext));
    }

    public async Task HandleAsync(ShipmentConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.SalesOrderId is null)
            return;

        var order = await _omContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == domainEvent.SalesOrderId, cancellationToken);
        if (order is null)
            return;

        foreach (var line in domainEvent.Lines)
        {
            if (line.SalesOrderLineId is null)
                continue;

            var shippedQty = line.Quantity;
            try
            {
                order.MarkShipped(line.SalesOrderLineId.Value, shippedQty);
            }
            catch (InvalidOperationException)
            {
                // Line already fully shipped or not found on this order; skip defensively.
            }
        }

        if (_omContext.ChangeTracker.HasChanges())
            await _omContext.SaveChangesAsync(cancellationToken);
    }
}
