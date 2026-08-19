// <copyright file="RequisitionLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class RequisitionLine : AuditableEntity
{
    protected RequisitionLine() { }

    public RequisitionLine(
        Guid requisitionId,
        int lineNumber,
        string? itemId,
        string description,
        decimal quantity,
        string unitOfMeasure,
        decimal estimatedUnitPrice,
        DateTime? needByDate,
        Guid? preferredVendorId,
        Guid? accountId,
        Guid? projectId,
        Guid? taskId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
            throw new ArgumentException("Unit of measure is required.", nameof(unitOfMeasure));

        RequisitionId = requisitionId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        EstimatedUnitPrice = estimatedUnitPrice;
        NeedByDate = needByDate;
        PreferredVendorId = preferredVendorId;
        AccountId = accountId;
        ProjectId = projectId;
        TaskId = taskId;
    }

    public Guid RequisitionId { get; private set; }

    public int LineNumber { get; private set; }

    public string? ItemId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public decimal EstimatedUnitPrice { get; private set; }

    public DateTime? NeedByDate { get; private set; }

    public Guid? PreferredVendorId { get; private set; }

    public Guid? AccountId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? TaskId { get; private set; }

    public decimal QuantityConverted { get; private set; }

    public bool IsFullyConverted => QuantityConverted >= Quantity;

    public decimal GetExtendedPrice() => Quantity * EstimatedUnitPrice;

    public void UpdateQuantityConverted(decimal convertedQuantity)
    {
        if (convertedQuantity < 0)
            throw new ArgumentException("Converted quantity cannot be negative.", nameof(convertedQuantity));

        if (QuantityConverted + convertedQuantity > Quantity)
            throw new InvalidOperationException("Cannot convert more than requisitioned quantity.");

        QuantityConverted += convertedQuantity;
    }
}
