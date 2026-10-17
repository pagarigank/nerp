// <copyright file="SalesCallLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class SalesCallLine : Entity
{
    private SalesCallLine() { }

    public SalesCallLine(
        Guid salesCallId,
        int lineNumber,
        Guid itemId,
        string itemCode,
        string description,
        decimal quantity,
        decimal sellingPrice,
        string unitOfMeasure,
        decimal discountPercent = 0m)
        : base(Guid.NewGuid())
    {
        SalesCallId = salesCallId;
        LineNumber = lineNumber;
        ItemId = itemId;
        ItemCode = itemCode;
        Description = description;
        Quantity = quantity;
        SellingPrice = sellingPrice;
        UnitOfMeasure = unitOfMeasure;
        DiscountPercent = discountPercent;
        LineTotal = Math.Round(quantity * sellingPrice * (1m - (discountPercent / 100m)), 2);
    }

    public Guid SalesCallId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid ItemId { get; private set; }

    public string ItemCode { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal SellingPrice { get; private set; }

    public string UnitOfMeasure { get; private set; } = "EA";

    public decimal DiscountPercent { get; private set; }

    public decimal LineTotal { get; private set; }

    public SalesCall? SalesCall { get; }

    public void UpdateLine(decimal quantity, decimal sellingPrice, decimal discountPercent)
    {
        Quantity = quantity;
        SellingPrice = sellingPrice;
        DiscountPercent = discountPercent;
        LineTotal = Math.Round(quantity * sellingPrice * (1m - (discountPercent / 100m)), 2);
    }
}
