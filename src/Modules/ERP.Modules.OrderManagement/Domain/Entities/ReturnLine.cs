// <copyright file="ReturnLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class ReturnLine : AuditableEntity
{
    protected ReturnLine() { }

    public ReturnLine(
        Guid returnId,
        int lineNumber,
        Guid itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        string unitOfMeasure,
        Guid? warehouseId = null,
        Guid? shipmentLineId = null,
        Guid? salesOrderLineId = null,
        Guid? accountId = null,
        decimal discountPercent = 0,
        decimal taxPercent = 0,
        string? restockDisposition = null)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        ReturnId = returnId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description ?? string.Empty;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitOfMeasure = unitOfMeasure ?? "EA";
        WarehouseId = warehouseId;
        ShipmentLineId = shipmentLineId;
        SalesOrderLineId = salesOrderLineId;
        AccountId = accountId;
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
        RestockDisposition = restockDisposition;
    }

    public Guid ReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string UnitOfMeasure { get; private set; } = "EA";
    public Guid? WarehouseId { get; private set; }
    public Guid? ShipmentLineId { get; private set; }
    public Guid? SalesOrderLineId { get; private set; }
    public Guid? AccountId { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public string? RestockDisposition { get; private set; }

    public decimal ExtendedPrice => Quantity * UnitPrice;
    public decimal DiscountAmount => ExtendedPrice * (DiscountPercent / 100m);
    public decimal TaxAmount => (ExtendedPrice - DiscountAmount) * (TaxPercent / 100m);
    public decimal LineTotal => ExtendedPrice - DiscountAmount + TaxAmount;
}
