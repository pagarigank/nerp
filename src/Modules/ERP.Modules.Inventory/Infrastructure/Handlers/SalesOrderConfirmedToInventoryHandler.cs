// <copyright file="SalesOrderConfirmedToInventoryHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.OrderManagement.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="SalesOrderConfirmedEvent"/> (raised when a sales order is
/// confirmed) and reserves the ordered quantities against inventory by increasing the
/// allocated quantity on the item stock record. This is the Sales -&gt; Inventory
/// leg of the integrated flow: confirming an order earmarks stock so concurrent
/// orders cannot oversell, and the later shipment issue (ShipmentConfirmedEvent)
/// relieves both on-hand and the allocation. Mirrors the GoodsReceived / Shipment
/// cross-module handlers, which also do not raise secondary events.
/// </summary>
public sealed class SalesOrderConfirmedToInventoryHandler : IDomainEventHandler<SalesOrderConfirmedEvent>
{
    private readonly InventoryDbContext _inventoryContext;

    public SalesOrderConfirmedToInventoryHandler(InventoryDbContext inventoryContext)
    {
        _inventoryContext = inventoryContext ?? throw new ArgumentNullException(nameof(inventoryContext));
    }

    public async Task HandleAsync(SalesOrderConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var line in domainEvent.Lines)
        {
            if (line.WarehouseId is null)
                continue;

            var stock = await _inventoryContext.ItemStocks
                .FirstOrDefaultAsync(s => s.ItemId == line.ItemId && s.WarehouseId == line.WarehouseId.Value, cancellationToken);

            if (stock is null)
                continue;

            stock.AdjustAllocated(line.Quantity);
        }

        if (_inventoryContext.ChangeTracker.HasChanges())
            await _inventoryContext.SaveChangesAsync(cancellationToken);
    }
}
