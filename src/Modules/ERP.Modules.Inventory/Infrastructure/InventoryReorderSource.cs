// <copyright file="InventoryReorderSource.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure;

public class InventoryReorderSource : IInventoryReorderSource
{
    private readonly InventoryDbContext _context;

    public InventoryReorderSource(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ReorderCandidate>> GetBelowReorderPointAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.Items
            .Where(i => i.Status == ItemStatus.Active && i.ReorderPoint != null && i.ReorderPoint > 0)
            .Select(i => new
            {
                i.Id,
                i.CompanyId,
                i.ItemCode,
                ReorderPointValue = i.ReorderPoint!.Value,
                ReorderQuantityValue = i.ReorderQuantity ?? 0m
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
            return [];

        var itemIds = items.Select(i => i.Id).ToList();

        var stockTotals = await _context.ItemStocks
            .Where(s => itemIds.Contains(s.ItemId))
            .GroupBy(s => s.ItemId)
            .Select(g => new { ItemId = g.Key, OnHand = g.Sum(x => x.OnHandQuantity) })
            .ToListAsync(cancellationToken);

        var vendorAssignments = await _context.ItemVendorAssignments
            .Where(a => a.IsPrimaryVendor && a.IsActive)
            .Select(a => new { a.ItemId, a.VendorId })
            .ToListAsync(cancellationToken);

        var onHandByItem = stockTotals.ToDictionary(s => s.ItemId, s => s.OnHand);
        var vendorByItem = vendorAssignments
            .GroupBy(a => a.ItemId)
            .ToDictionary(g => g.Key, g => g.First().VendorId);

        List<ReorderCandidate> candidates = [];
        foreach (var item in items)
        {
            var onHand = onHandByItem.GetValueOrDefault(item.Id);
            if (onHand >= item.ReorderPointValue)
                continue;

            Guid? preferredVendorId = null;
            if (vendorByItem.TryGetValue(item.Id, out var vendorId))
                preferredVendorId = vendorId;

            candidates.Add(new ReorderCandidate(
                item.CompanyId,
                item.Id,
                item.ItemCode,
                preferredVendorId,
                onHand,
                item.ReorderPointValue,
                item.ReorderQuantityValue));
        }

        return candidates;
    }
}
