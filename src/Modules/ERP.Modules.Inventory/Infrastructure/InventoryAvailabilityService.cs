// <copyright file="InventoryAvailabilityService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Common;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure;

/// <summary>
/// Implements <see cref="IInventoryAvailability"/> for the Order Management module.
/// Reports on-hand, allocated, and available (on-hand - allocated) quantities so a
/// sales order can be gated on real inventory without OM referencing the Inventory
/// module (Inventory -> OM for the shipment-to-issue handler, so the dependency is
/// intentionally one-directional).
/// </summary>
public sealed class InventoryAvailabilityService : IInventoryAvailability
{
    private readonly InventoryDbContext _context;

    public InventoryAvailabilityService(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AvailabilityResult> CheckAsync(Guid itemId, Guid warehouseId, decimal requestedQuantity, CancellationToken cancellationToken = default)
    {
        var stock = await _context.ItemStocks
            .FirstOrDefaultAsync(s => s.ItemId == itemId && s.WarehouseId == warehouseId, cancellationToken);

        var onHand = stock?.OnHandQuantity ?? 0m;
        var allocated = stock?.AllocatedQuantity ?? 0m;
        var available = onHand - allocated;

        return new AvailabilityResult(onHand, allocated, available, available >= requestedQuantity);
    }
}
