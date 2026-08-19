// <copyright file="ShipmentConfirmedToInventoryHandler.cs" company="ERP Project">
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
/// Consumes <see cref="ShipmentConfirmedEvent"/> (raised when a sales shipment is
/// confirmed) and relieves inventory by issuing the shipped quantities. This closes
/// the Sales -&gt; Inventory leg of the integrated flow (Purchase -&gt; Inventory
/// -&gt; Sales): shipping stock decrements on-hand and records COGS via the issue
/// transaction, which also feeds the Inventory -&gt; GL posting handler.
/// </summary>
public sealed class ShipmentConfirmedToInventoryHandler : IDomainEventHandler<ShipmentConfirmedEvent>
{
    private readonly InventoryDbContext _inventoryContext;

    public ShipmentConfirmedToInventoryHandler(InventoryDbContext inventoryContext)
    {
        _inventoryContext = inventoryContext ?? throw new ArgumentNullException(nameof(inventoryContext));
    }

    public async Task HandleAsync(ShipmentConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0)
            return;

        foreach (var line in domainEvent.Lines)
        {
            var item = await _inventoryContext.Items.FirstOrDefaultAsync(i => i.Id == line.ItemId, cancellationToken);
            if (item is null)
                continue;

            var warehouseId = line.WarehouseId
                ?? (await _inventoryContext.Warehouses
                    .Where(w => w.CompanyId == domainEvent.CompanyId)
                    .OrderBy(w => w.WarehouseCode)
                    .FirstOrDefaultAsync(cancellationToken))?.Id;
            if (warehouseId is null)
                continue;

            var avgCost = await GetAverageCostAsync(line.ItemId, warehouseId.Value, cancellationToken);

            var transaction = new InventoryTransaction(
                domainEvent.CompanyId,
                line.ItemId,
                warehouseId.Value,
                TransactionType.Issue,
                -line.Quantity,
                line.UnitOfMeasure,
                avgCost,
                domainEvent.ShipmentDate,
                null,
                null,
                null,
                domainEvent.ShipmentNumber,
                line.ProjectId,
                $"Shipment {domainEvent.ShipmentNumber}");

            _inventoryContext.InventoryTransactions.Add(transaction);

            // Maintain the on-hand quantity on the item stock record, and release the
            // allocation that the sales-order confirmation reserved for this line.
            var stock = await _inventoryContext.ItemStocks
                .FirstOrDefaultAsync(s => s.ItemId == line.ItemId && s.WarehouseId == warehouseId.Value, cancellationToken);
            if (stock is not null)
            {
                stock.AdjustOnHand(-line.Quantity);
                stock.AdjustAllocated(-line.Quantity);
            }
        }

        if (_inventoryContext.ChangeTracker.HasChanges())
            await _inventoryContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal> GetAverageCostAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var receipts = await _inventoryContext.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId && t.Quantity > 0)
            .Select(t => new { t.Quantity, t.ExtendedCost })
            .ToListAsync(cancellationToken);

        if (receipts.Count == 0)
        {
            var item = await _inventoryContext.Items.FindAsync(new object[] { itemId }, cancellationToken);
            return item?.StandardCost ?? 0m;
        }

        var totalQty = receipts.Sum(r => r.Quantity);
        var totalCost = receipts.Sum(r => r.ExtendedCost);
        return totalQty > 0 ? totalCost / totalQty : 0m;
    }
}
