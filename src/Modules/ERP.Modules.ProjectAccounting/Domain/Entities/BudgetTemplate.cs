// <copyright file="BudgetTemplate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Reusable budget structure for similar project types. Lines are copied into a
/// new project's BudgetLines on creation (spec §7.2 budget template).
/// </summary>
public class BudgetTemplate : AuditableEntity
{
    private readonly List<BudgetTemplateLine> _lines = [];

    protected BudgetTemplate() { }

    public BudgetTemplate(Guid companyId, string name, string? projectType = null, string? description = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));

        CompanyId = companyId;
        Name = name;
        ProjectType = projectType;
        Description = description;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ProjectType { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<BudgetTemplateLine> Lines => _lines.AsReadOnly();

    public void Update(string? name, string? projectType, string? description, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
        if (projectType is not null)
            ProjectType = projectType;
        if (description is not null)
            Description = description;
        if (isActive.HasValue)
            IsActive = isActive.Value;
    }

    public BudgetTemplateLine AddLine(CostCategory category, decimal budgetAmount, decimal? budgetedHours, string? description = null)
    {
        var line = new BudgetTemplateLine(Id, category, budgetAmount, budgetedHours, description);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is not null)
            _lines.Remove(line);
    }
}

public class BudgetTemplateLine : AuditableEntity
{
    protected BudgetTemplateLine() { }

    public BudgetTemplateLine(Guid templateId, CostCategory category, decimal budgetAmount, decimal? budgetedHours, string? description)
        : base(Guid.NewGuid())
    {
        TemplateId = templateId;
        Category = category;
        BudgetAmount = budgetAmount;
        BudgetedHours = budgetedHours;
        Description = description;
    }

    public Guid TemplateId { get; private set; }
    public CostCategory Category { get; private set; }
    public decimal BudgetAmount { get; private set; }
    public decimal? BudgetedHours { get; private set; }
    public string? Description { get; private set; }
}
