// <copyright file="ItemUnitOfMeasureConversion.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemUnitOfMeasureConversion : AuditableEntity
{
    protected ItemUnitOfMeasureConversion() { }

    public ItemUnitOfMeasureConversion(
        Guid itemId,
        string fromUOM,
        string toUOM,
        decimal conversionFactor)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(fromUOM))
            throw new ArgumentException("From UOM is required.", nameof(fromUOM));
        if (string.IsNullOrWhiteSpace(toUOM))
            throw new ArgumentException("To UOM is required.", nameof(toUOM));
        if (conversionFactor <= 0)
            throw new ArgumentException("Conversion factor must be positive.", nameof(conversionFactor));

        ItemId = itemId;
        FromUOM = fromUOM;
        ToUOM = toUOM;
        ConversionFactor = conversionFactor;
    }

    public Guid ItemId { get; private set; }

    public string FromUOM { get; private set; } = string.Empty;

    public string ToUOM { get; private set; } = string.Empty;

    public decimal ConversionFactor { get; private set; }

    public void UpdateConversionFactor(decimal newFactor)
    {
        if (newFactor <= 0)
            throw new ArgumentException("Conversion factor must be positive.", nameof(newFactor));
        ConversionFactor = newFactor;
    }
}
