// <copyright file="PurchaseOrderTemplateLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class PurchaseOrderTemplateLine : AuditableEntity
{
    protected PurchaseOrderTemplateLine() { }

    public PurchaseOrderTemplateLine(
        Guid templateId,
        int lineNumber,
        string? itemId,
        string description,
        decimal? defaultQuantity,
        string unitOfMeasure,
        decimal unitPrice,
        Guid? accountId,
        Guid? projectId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TemplateId = templateId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description;
        DefaultQuantity = defaultQuantity;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        AccountId = accountId;
        ProjectId = projectId;
    }

    public Guid TemplateId { get; private set; }

    public int LineNumber { get; private set; }

    public string? ItemId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal? DefaultQuantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public Guid? AccountId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public void UpdatePricing(decimal newUnitPrice)
    {
        if (newUnitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(newUnitPrice));

        UnitPrice = newUnitPrice;
    }
}
