// <copyright file="ReportCategory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class ReportCategory : AuditableAggregateRoot
{
    protected ReportCategory() { }

    public ReportCategory(
        Guid companyId,
        string name,
        string? parentId,
        int sortOrder,
        string? description,
        string? icon) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParentId = parentId;
        SortOrder = sortOrder;
        Description = description;
        Icon = icon;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public string? Description { get; private set; }
    public string? Icon { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string? parentId, int sortOrder, string? description, string? icon)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParentId = parentId;
        SortOrder = sortOrder;
        Description = description;
        Icon = icon;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
