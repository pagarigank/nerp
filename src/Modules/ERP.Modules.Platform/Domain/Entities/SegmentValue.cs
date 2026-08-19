// <copyright file="SegmentValue.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class SegmentValue : AuditableAggregateRoot
{
    protected SegmentValue() { }

    public SegmentValue(
        Guid segmentTypeId,
        Guid companyId,
        string value,
        string description,
        int displayOrder) : base(Guid.NewGuid())
    {
        SegmentTypeId = segmentTypeId;
        CompanyId = companyId;
        Value = value?.ToUpperInvariant().Trim() ?? throw new ArgumentNullException(nameof(value));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Guid SegmentTypeId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string value, string description, int displayOrder)
    {
        Value = value?.ToUpperInvariant().Trim() ?? throw new ArgumentNullException(nameof(value));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        DisplayOrder = displayOrder;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
