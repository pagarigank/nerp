// <copyright file="GoodsReceivedToInventoryHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Purchasing.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="GoodsReceivedEvent"/> (raised when a purchasing goods
/// receipt is posted) and creates the corresponding Inventory receipt
/// transactions so received stock is reflected in inventory. This closes the
/// Purchasing -&gt; Inventory integration: previously the event was defined but
/// never raised or consumed, so goods receipts never moved inventory. Creating
/// the inventory receipt transaction also triggers the Inventory -&gt; GL posting
/// handler, so the receipt flows all the way to the General Ledger.
/// </summary>
public sealed class GoodsReceivedToInventoryHandler : IDomainEventHandler<GoodsReceivedEvent>
{
    private readonly InventoryDbContext _inventoryContext;

    public GoodsReceivedToInventoryHandler(InventoryDbContext inventoryContext)
    {
        _inventoryContext = inventoryContext ?? throw new ArgumentNullException(nameof(inventoryContext));
    }

    public async Task HandleAsync(GoodsReceivedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0)
            return;

        // GoodsReceivedLine does not carry a warehouse; use the company's first
        // warehouse as the receipt destination.
        var warehouse = await _inventoryContext.Warehouses
            .Where(w => w.CompanyId == domainEvent.CompanyId)
            .OrderBy(w => w.WarehouseCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null)
            return;

        foreach (var line in domainEvent.Lines)
        {
            var item = await ResolveItemAsync(line.ItemId, cancellationToken);
            if (item is null)
                continue;

            // Unit cost is not supplied on a goods receipt; fall back to the item's
            // standard cost so the receipt still carries a value into inventory/GL.
            var unitCost = item.StandardCost ?? 0m;

            var transaction = new InventoryTransaction(
                domainEvent.CompanyId,
                item.Id,
                warehouse.Id,
                TransactionType.Receipt,
                line.QuantityReceived,
                line.UnitOfMeasure,
                unitCost,
                domainEvent.ReceivedDate,
                referenceNumber: domainEvent.ReceiptNumber);

            _inventoryContext.InventoryTransactions.Add(transaction);
        }

        if (_inventoryContext.ChangeTracker.HasChanges())
            await _inventoryContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Item?> ResolveItemAsync(string? itemId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (Guid.TryParse(itemId, out var itemGuid))
        {
            var byId = await _inventoryContext.Items
                .FirstOrDefaultAsync(i => i.Id == itemGuid, cancellationToken);
            if (byId is not null)
                return byId;
        }

        return await _inventoryContext.Items
            .FirstOrDefaultAsync(i => i.ItemCode == itemId, cancellationToken);
    }
}
