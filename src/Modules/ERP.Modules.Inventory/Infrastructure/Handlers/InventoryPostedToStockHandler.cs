// <copyright file="InventoryPostedToStockHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="InventoryTransactionPostedEvent"/> and maintains the
/// perpetual <see cref="ItemStock"/> on-hand / allocated balances. This closes the
/// previously-missing Inventory sub-ledger update: without it, receipts/issues
/// posted to GL but never changed available quantity, so reservations and
/// available-quantity checks always reported zero. Transfers move on-hand between
/// warehouses; internal transfers do not change total company on-hand.
/// </summary>
public sealed class InventoryPostedToStockHandler : IDomainEventHandler<InventoryTransactionPostedEvent>
{
    private readonly InventoryDbContext _inventoryContext;

    public InventoryPostedToStockHandler(InventoryDbContext inventoryContext)
    {
        _inventoryContext = inventoryContext ?? throw new ArgumentNullException(nameof(inventoryContext));
    }

    public async Task HandleAsync(InventoryTransactionPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var type = domainEvent.TransactionType;

        switch (type)
        {
            case nameof(TransactionType.Receipt):
            case nameof(TransactionType.ProductionReceipt):
            case nameof(TransactionType.Adjustment):
            case nameof(TransactionType.Transfer):
            case nameof(TransactionType.TransferIn):
            case nameof(TransactionType.TransferOut):
                // Quantity already carries direction: receipts/transfers-in are
                // positive, issues/transfers-out are negative (the transfer flow
                // emits two transactions with signed quantities).
                await ApplyAsync(domainEvent.CompanyId, domainEvent.ItemId, domainEvent.WarehouseId, domainEvent.Quantity, cancellationToken);
                break;

            case nameof(TransactionType.Issue):
                await ApplyAsync(domainEvent.CompanyId, domainEvent.ItemId, domainEvent.WarehouseId, -domainEvent.Quantity, cancellationToken);
                break;

            default:
                return;
        }
    }

    private async Task ApplyAsync(Guid companyId, Guid itemId, Guid warehouseId, decimal delta, CancellationToken cancellationToken)
    {
        if (delta == 0m)
            return;

        var stock = await _inventoryContext.ItemStocks
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.ItemId == itemId && s.WarehouseId == warehouseId, cancellationToken);

        if (stock is null)
        {
            stock = new ItemStock(companyId, itemId, warehouseId);
            _inventoryContext.ItemStocks.Add(stock);
        }

        stock.AdjustOnHand(delta);

        await _inventoryContext.SaveChangesAsync(cancellationToken);
    }
}
