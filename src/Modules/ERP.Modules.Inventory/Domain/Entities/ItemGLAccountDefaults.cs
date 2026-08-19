// <copyright file="ItemGLAccountDefaults.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemGLAccountDefaults : AuditableEntity
{
    protected ItemGLAccountDefaults() { }

    public ItemGLAccountDefaults(
        Guid itemId,
        Guid? inventoryAssetAccountId,
        Guid? cogsAccountId,
        Guid? varianceAccountId,
        Guid? purchasePriceVarianceAccountId,
        Guid? salesRevenueAccountId,
        Guid? inventoryAdjustmentAccountId,
        Guid? landedCostClearingAccountId)
        : base(Guid.NewGuid())
    {
        ItemId = itemId;
        InventoryAssetAccountId = inventoryAssetAccountId;
        COGSAccountId = cogsAccountId;
        VarianceAccountId = varianceAccountId;
        PurchasePriceVarianceAccountId = purchasePriceVarianceAccountId;
        SalesRevenueAccountId = salesRevenueAccountId;
        InventoryAdjustmentAccountId = inventoryAdjustmentAccountId;
        LandedCostClearingAccountId = landedCostClearingAccountId;
    }

    public Guid ItemId { get; private set; }

    public Guid? InventoryAssetAccountId { get; private set; }

    public Guid? COGSAccountId { get; private set; }

    public Guid? VarianceAccountId { get; private set; }

    public Guid? PurchasePriceVarianceAccountId { get; private set; }

    public Guid? SalesRevenueAccountId { get; private set; }

    public Guid? InventoryAdjustmentAccountId { get; private set; }

    public Guid? LandedCostClearingAccountId { get; private set; }

    public void UpdateAccounts(
        Guid? inventoryAssetAccountId,
        Guid? cogsAccountId,
        Guid? varianceAccountId,
        Guid? purchasePriceVarianceAccountId,
        Guid? salesRevenueAccountId,
        Guid? inventoryAdjustmentAccountId,
        Guid? landedCostClearingAccountId)
    {
        InventoryAssetAccountId = inventoryAssetAccountId;
        COGSAccountId = cogsAccountId;
        VarianceAccountId = varianceAccountId;
        PurchasePriceVarianceAccountId = purchasePriceVarianceAccountId;
        SalesRevenueAccountId = salesRevenueAccountId;
        InventoryAdjustmentAccountId = inventoryAdjustmentAccountId;
        LandedCostClearingAccountId = landedCostClearingAccountId;
    }
}