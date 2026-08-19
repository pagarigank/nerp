// <copyright file="RequisitionTemplateLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class RequisitionTemplateLine : AuditableEntity
{
    protected RequisitionTemplateLine() { }

    public RequisitionTemplateLine(
        Guid templateId,
        int lineNumber,
        string? itemId,
        string description,
        decimal defaultQuantity,
        string unitOfMeasure,
        Guid? accountId,
        Guid? projectId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (defaultQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(defaultQuantity));

        TemplateId = templateId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description;
        DefaultQuantity = defaultQuantity;
        UnitOfMeasure = unitOfMeasure;
        AccountId = accountId;
        ProjectId = projectId;
    }

    public Guid TemplateId { get; private set; }

    public int LineNumber { get; private set; }

    public string? ItemId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal DefaultQuantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public Guid? AccountId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public void UpdateQuantity(decimal newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

        DefaultQuantity = newQuantity;
    }
}
