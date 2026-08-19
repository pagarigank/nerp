// <copyright file="PutAwayPickingRule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

/// <summary>
/// Warehouse bin strategy rules: bin rank (lower = preferred for put-away),
/// pick sequence (lower = picked first) and the FIFO/FEFO picking policy used
/// when selecting lots for issue.
/// </summary>
public class PutAwayPickingRule : AuditableEntity
{
    private PutAwayPickingRule() { }

    public PutAwayPickingRule(
        Guid companyId,
        Guid warehouseId,
        Guid binId,
        int putAwayRank,
        int pickSequence,
        PickingPolicy pickingPolicy)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        WarehouseId = warehouseId;
        BinId = binId;
        PutAwayRank = putAwayRank;
        PickSequence = pickSequence;
        PickingPolicy = pickingPolicy;
    }

    public Guid CompanyId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BinId { get; private set; }
    public int PutAwayRank { get; private set; }
    public int PickSequence { get; private set; }
    public PickingPolicy PickingPolicy { get; private set; }

    public void Update(int putAwayRank, int pickSequence, PickingPolicy pickingPolicy)
    {
        PutAwayRank = putAwayRank;
        PickSequence = pickSequence;
        PickingPolicy = pickingPolicy;
    }

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public WarehouseBin? Bin { get; set; }
}

public enum PickingPolicy
{
    None = 0,
    Fifo = 1, // first in, first out (oldest lot)
    Fefo = 2, // first expired, first out (earliest expiration)
    BinSequence = 3, // by bin pick sequence
}
