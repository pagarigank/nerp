// <copyright file="ItemAlternateCode.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemAlternateCode : AuditableEntity
{
    protected ItemAlternateCode() { }

    public ItemAlternateCode(
        Guid itemId,
        string alternateCode,
        AlternateCodeType codeType,
        string? description = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(alternateCode))
            throw new ArgumentException("Alternate code is required.", nameof(alternateCode));

        ItemId = itemId;
        AlternateCode = alternateCode;
        CodeType = codeType;
        Description = description;
    }

    public Guid ItemId { get; private set; }

    public string AlternateCode { get; private set; } = string.Empty;

    public AlternateCodeType CodeType { get; private set; }

    public string? Description { get; private set; }
}

public enum AlternateCodeType
{
    None = 0,
    VendorItemCode = 1,
    CustomerItemCode = 2,
    OldItemCode = 3,
    Barcode = 4,
    UPC = 5,
}
