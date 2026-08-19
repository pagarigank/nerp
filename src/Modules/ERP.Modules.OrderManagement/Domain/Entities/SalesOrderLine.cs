// <copyright file="SalesOrderLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class SalesOrderLine : AuditableEntity
{
    protected SalesOrderLine() { }

    public SalesOrderLine(
        Guid salesOrderId,
        int lineNumber,
        Guid itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        string unitOfMeasure,
        decimal discountPercent = 0,
        decimal taxPercent = 0,
        Guid? warehouseId = null,
        Guid? projectId = null,
        Guid? accountId = null,
        bool isDropShip = false,
        Guid? dropShipVendorId = null)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        SalesOrderId = salesOrderId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description ?? string.Empty;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitOfMeasure = unitOfMeasure ?? "EA";
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
        WarehouseId = warehouseId;
        ProjectId = projectId;
        AccountId = accountId;
        IsDropShip = isDropShip;
        DropShipVendorId = isDropShip ? dropShipVendorId : null;
        ShippedQuantity = 0;
    }

    public Guid SalesOrderId { get; private set; }
#pragma warning disable S1144 // Private setter is used by EF Core to materialize the navigation.
    public SalesOrder? SalesOrder { get; private set; }
#pragma warning restore S1144
    public int LineNumber { get; private set; }
    public Guid ItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string UnitOfMeasure { get; private set; } = "EA";
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? AccountId { get; private set; }
    public bool IsDropShip { get; private set; }
    public Guid? DropShipVendorId { get; private set; }
    public decimal ShippedQuantity { get; private set; }

    /// <summary>Portion of the order-level freight allocated to this line (Phase 8 gap 578).</summary>
    public decimal AllocatedFreight { get; private set; }

    /// <summary>Quantity still owed to the customer (ordered minus already shipped).</summary>
    public decimal BackorderedQuantity => Quantity - ShippedQuantity;

    public decimal ExtendedPrice => Quantity * UnitPrice;
    public decimal DiscountAmount => ExtendedPrice * (DiscountPercent / 100m);
    public decimal TaxAmount => (ExtendedPrice - DiscountAmount) * (TaxPercent / 100m);
    public decimal LineTotal => ExtendedPrice - DiscountAmount + TaxAmount;

    public void AllocateFreight(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Allocated freight cannot be negative.", nameof(amount));
        AllocatedFreight = amount;
    }

    public void Update(
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        Guid? warehouseId,
        Guid? projectId,
        Guid? accountId,
        string? description)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
        WarehouseId = warehouseId;
        ProjectId = projectId;
        AccountId = accountId;
        Description = description ?? string.Empty;
    }

    public void MarkShipped(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Shipped quantity must be positive.", nameof(quantity));
        if (ShippedQuantity + quantity > Quantity)
            throw new InvalidOperationException("Cannot ship more than the ordered quantity.");

        ShippedQuantity += quantity;
    }
}
