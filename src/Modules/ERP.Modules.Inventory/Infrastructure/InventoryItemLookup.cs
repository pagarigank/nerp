// <copyright file="InventoryItemLookup.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure;

public class InventoryItemLookup : IInventoryItemLookup
{
    private readonly InventoryDbContext _context;

    public InventoryItemLookup(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<InventoryItemInfo>> GetItemsAsync(IReadOnlyList<Guid> itemIds, CancellationToken ct)
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        var rows = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new InventoryItemInfo(
                i.Id,
                i.ItemCode,
                i.Status == ItemStatus.Active,
                null,
                i.StandardCost))
            .ToListAsync(ct);

        return rows;
    }
}
