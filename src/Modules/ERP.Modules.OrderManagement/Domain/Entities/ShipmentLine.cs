// <copyright file="ShipmentLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class ShipmentLine : AuditableEntity
{
    protected ShipmentLine() { }

    public ShipmentLine(
        Guid shipmentId,
        int lineNumber,
        Guid itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        string unitOfMeasure,
        Guid? warehouseId = null,
        Guid? salesOrderLineId = null,
        Guid? projectId = null,
        Guid? accountId = null,
        decimal discountPercent = 0,
        decimal taxPercent = 0)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        ShipmentId = shipmentId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description ?? string.Empty;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitOfMeasure = unitOfMeasure ?? "EA";
        WarehouseId = warehouseId;
        SalesOrderLineId = salesOrderLineId;
        ProjectId = projectId;
        AccountId = accountId;
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
    }

    public Guid ShipmentId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string UnitOfMeasure { get; private set; } = "EA";
    public Guid? WarehouseId { get; private set; }
    public Guid? SalesOrderLineId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? AccountId { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
}
