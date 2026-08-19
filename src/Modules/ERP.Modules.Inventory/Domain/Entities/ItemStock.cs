// <copyright file="ItemStock.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemStock : AuditableEntity
{
    protected ItemStock() { }

    public ItemStock(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        Guid? binId = null,
        Guid? lotId = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        BinId = binId;
        LotId = lotId;
        OnHandQuantity = 0;
        AllocatedQuantity = 0;
        OnOrderQuantity = 0;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? BinId { get; private set; }

    public Guid? LotId { get; private set; }

    public decimal OnHandQuantity { get; private set; }

    public decimal AllocatedQuantity { get; private set; }

    public decimal OnOrderQuantity { get; private set; }

    public decimal AvailableQuantity => OnHandQuantity - AllocatedQuantity;

    public void AdjustOnHand(decimal quantity) => OnHandQuantity += quantity;

    public void AdjustAllocated(decimal quantity) => AllocatedQuantity += quantity;

    public void AdjustOnOrder(decimal quantity) => OnOrderQuantity += quantity;

    public void SetOnHand(decimal quantity) => OnHandQuantity = quantity;

    // Navigation properties
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
    public WarehouseBin? Bin { get; set; }
}
