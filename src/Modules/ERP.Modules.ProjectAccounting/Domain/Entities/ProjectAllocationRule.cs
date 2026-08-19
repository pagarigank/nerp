// <copyright file="ProjectAllocationRule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class ProjectAllocationRule : AuditableEntity
{
    protected ProjectAllocationRule() { }

    public ProjectAllocationRule(
        Guid projectId,
        CostCategory category,
        decimal markupPercentage,
        decimal? overheadPercentage,
        string? description,
        int priority)
        : base(Guid.NewGuid())
    {
        ProjectId = projectId;
        Category = category;
        MarkupPercentage = markupPercentage;
        OverheadPercentage = overheadPercentage;
        Description = description;
        Priority = priority;
        IsActive = true;
    }

    public Guid ProjectId { get; private set; }
    public CostCategory Category { get; private set; }
    public decimal MarkupPercentage { get; private set; }
    public decimal? OverheadPercentage { get; private set; }
    public string? Description { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the burden (markup) amount on a cost.
    /// </summary>
    /// <param name="costAmount">The base cost amount to calculate burden on.</param>
    /// <returns>The total burden amount.</returns>
    public decimal CalculateBurden(decimal costAmount)
    {
        var overhead = OverheadPercentage.HasValue ? costAmount * OverheadPercentage.Value / 100m : 0m;
        var markup = (costAmount + overhead) * MarkupPercentage / 100m;
        return overhead + markup;
    }

    public void Update(
        decimal? markupPercentage,
        decimal? overheadPercentage,
        string? description,
        int? priority,
        bool? isActive)
    {
        if (markupPercentage.HasValue)
        {
            MarkupPercentage = markupPercentage.Value;
        }

        if (overheadPercentage.HasValue)
        {
            OverheadPercentage = overheadPercentage;
        }

        if (description is not null)
        {
            Description = description;
        }

        if (priority.HasValue)
        {
            Priority = priority.Value;
        }

        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
    }
}
