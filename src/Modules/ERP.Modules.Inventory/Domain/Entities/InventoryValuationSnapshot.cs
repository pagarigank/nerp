// <copyright file="InventoryValuationSnapshot.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class InventoryValuationSnapshot : AuditableEntity
{
    protected InventoryValuationSnapshot() { }

    public InventoryValuationSnapshot(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        DateTime snapshotDate,
        decimal onHandQuantity,
        decimal standardCost,
        decimal averageCost)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        SnapshotDate = snapshotDate;
        OnHandQuantity = onHandQuantity;
        StandardCost = standardCost;
        AverageCost = averageCost;
        StandardValue = onHandQuantity * standardCost;
        AverageValue = onHandQuantity * averageCost;
    }

    public Guid CompanyId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime SnapshotDate { get; private set; }
    public decimal OnHandQuantity { get; private set; }
    public decimal StandardCost { get; private set; }
    public decimal AverageCost { get; private set; }
    public decimal StandardValue { get; private set; }
    public decimal AverageValue { get; private set; }
}