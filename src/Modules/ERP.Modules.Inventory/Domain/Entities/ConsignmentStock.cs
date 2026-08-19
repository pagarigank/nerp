// <copyright file="ConsignmentStock.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

/// <summary>
/// Vendor-owned inventory held on the company's premises. Tracked separately
/// from owned on-hand so it does not inflate the balance sheet. Consuming
/// (transferring to owned stock) triggers a payable to the vendor.
/// </summary>
public class ConsignmentStock : AuditableEntity
{
    private ConsignmentStock() { }

    public ConsignmentStock(
        Guid companyId,
        Guid vendorId,
        Guid itemId,
        Guid warehouseId,
        decimal quantityOnHand,
        string unitOfMeasure,
        decimal? consignmentCost = null,
        Guid? lotId = null)
        : base(Guid.NewGuid())
    {
        if (quantityOnHand < 0)
            throw new ArgumentException("Quantity on hand cannot be negative.", nameof(quantityOnHand));

        CompanyId = companyId;
        VendorId = vendorId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        QuantityOnHand = quantityOnHand;
        UnitOfMeasure = unitOfMeasure;
        ConsignmentCost = consignmentCost;
        LotId = lotId;
    }

    public Guid CompanyId { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal? ConsignmentCost { get; private set; }
    public Guid? LotId { get; private set; }

    public void Receive(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Receipt quantity must be greater than zero.", nameof(quantity));
        QuantityOnHand += quantity;
    }

    public void Consume(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Consume quantity must be greater than zero.", nameof(quantity));
        if (quantity > QuantityOnHand)
            throw new InvalidOperationException("Cannot consume more consignment stock than on hand.");
        QuantityOnHand -= quantity;
    }

    // Navigation
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
}
