// <copyright file="SegmentType.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class SegmentType : AuditableAggregateRoot
{
    protected SegmentType() { }

    public SegmentType(
        Guid companyId,
        string name,
        string code,
        int displayOrder,
        bool isRequired) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        DisplayOrder = displayOrder;
        IsRequired = isRequired;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string name, string code, int displayOrder, bool isRequired)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        DisplayOrder = displayOrder;
        IsRequired = isRequired;
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
