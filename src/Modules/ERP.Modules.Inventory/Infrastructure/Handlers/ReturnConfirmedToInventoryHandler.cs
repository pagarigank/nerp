// <copyright file="ReturnConfirmedToInventoryHandler.cs" company="ERP Project">
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
/// Consumes <see cref="ReturnConfirmedEvent"/> (raised when a customer return / RMA is
/// confirmed) and restocks the returned items by creating a receipt transaction and
/// increasing on-hand quantity. This is the reverse leg of the Sales -> Inventory
/// integration: returns now flow back into inventory (Purchase -> Inventory -> Sales
/// -> Return -> Inventory completes the loop).
/// </summary>
public sealed class ReturnConfirmedToInventoryHandler : IDomainEventHandler<ReturnConfirmedEvent>
{
    private readonly InventoryDbContext _inventoryContext;

    public ReturnConfirmedToInventoryHandler(InventoryDbContext inventoryContext)
    {
        _inventoryContext = inventoryContext ?? throw new ArgumentNullException(nameof(inventoryContext));
    }

    public async Task HandleAsync(ReturnConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
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
                TransactionType.Receipt,
                line.Quantity,
                line.UnitOfMeasure,
                avgCost,
                domainEvent.ReturnDate,
                null,
                null,
                null,
                domainEvent.ReturnNumber,
                line.SalesOrderLineId,
                $"Customer return {domainEvent.ReturnNumber}");

            _inventoryContext.InventoryTransactions.Add(transaction);

            var stock = await _inventoryContext.ItemStocks
                .FirstOrDefaultAsync(s => s.ItemId == line.ItemId && s.WarehouseId == warehouseId.Value, cancellationToken);
            if (stock is not null)
                stock.AdjustOnHand(line.Quantity);
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
