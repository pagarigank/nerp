// <copyright file="WarehouseBin.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class WarehouseBin : AuditableEntity
{
    protected WarehouseBin() { }

    public WarehouseBin(
        Guid warehouseId,
        string binCode,
        BinType binType,
        string? aisle = null,
        string? rack = null,
        string? shelf = null,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(binCode))
            throw new ArgumentException("Bin code is required.", nameof(binCode));

        WarehouseId = warehouseId;
        BinCode = binCode;
        BinType = binType;
        Aisle = aisle;
        Rack = rack;
        Shelf = shelf;
        IsActive = isActive;
    }

    public Guid WarehouseId { get; private set; }

    public string BinCode { get; private set; } = string.Empty;

    public BinType BinType { get; private set; }

    public string? Aisle { get; private set; }

    public string? Rack { get; private set; }

    public string? Shelf { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateLocation(string? aisle, string? rack, string? shelf)
    {
        Aisle = aisle;
        Rack = rack;
        Shelf = shelf;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

public enum BinType
{
    None = 0,
    Picking = 1,
    Bulk = 2,
    Receiving = 3,
    Shipping = 4,
}
