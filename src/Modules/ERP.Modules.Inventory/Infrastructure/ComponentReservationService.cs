// <copyright file="ComponentReservationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure;

public class ComponentReservationService : IComponentReservationService
{
    private readonly InventoryDbContext _context;

    public ComponentReservationService(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> ReserveForBuildOrderAsync(Guid companyId, Guid buildOrderId, IReadOnlyList<ComponentReservationRequest> components, CancellationToken ct)
    {
        var existing = await _context.ItemReservations
            .Where(r => r.SourceType == ReservationSourceType.ProductionOrder
                        && r.SourceId == buildOrderId
                        && r.Status != ItemReservationStatus.Cancelled)
            .CountAsync(ct);

        if (existing > 0)
        {
            return 0;
        }

        var requestedItemIds = components.Select(c => c.ItemId).Distinct().ToList();

        var stockWarehouseByItem = (await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && requestedItemIds.Contains(s.ItemId))
            .OrderByDescending(s => s.OnHandQuantity)
            .Select(s => new { s.ItemId, s.WarehouseId })
            .ToListAsync(ct))
            .GroupBy(s => s.ItemId)
            .ToDictionary(g => g.Key, g => g.First().WarehouseId);

        var companyWarehouses = await _context.Warehouses
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .OrderBy(w => w.CreatedOn)
            .Select(w => w.Id)
            .ToListAsync(ct);

        var defaultWarehouseId = companyWarehouses.FirstOrDefault();
        var stocksByItemWarehouse = await LoadStockIndexAsync(companyId, requestedItemIds, ct);

        var created = 0;

        foreach (var request in components)
        {
            if (request.Quantity <= 0)
            {
                continue;
            }

            var warehouseId = stockWarehouseByItem.TryGetValue(request.ItemId, out var stocked)
                ? stocked
                : defaultWarehouseId;

            if (warehouseId == Guid.Empty)
            {
                continue;
            }

            var reservation = new ItemReservation(
                companyId,
                request.ItemId,
                warehouseId,
                request.Quantity,
                request.UnitOfMeasure,
                ReservationSourceType.ProductionOrder,
                buildOrderId,
                notes: "Build order release");

            _context.ItemReservations.Add(reservation);
            created++;

            if (stocksByItemWarehouse.TryGetValue((request.ItemId, warehouseId), out var stock))
            {
                stock.AdjustAllocated(request.Quantity);
            }
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return created;
    }

    private async Task<Dictionary<(Guid ItemId, Guid WarehouseId), ItemStock>> LoadStockIndexAsync(
        Guid companyId, IReadOnlyList<Guid> itemIds, CancellationToken ct)
    {
        var stocks = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && itemIds.Contains(s.ItemId))
            .ToListAsync(ct);

        return stocks.ToDictionary(s => (s.ItemId, s.WarehouseId));
    }
}
