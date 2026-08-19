// <copyright file="ProjectTask.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class ProjectTask : AuditableEntity
{
    protected ProjectTask() { }

    public ProjectTask(
        Guid projectId,
        string taskCode,
        string description,
        Guid? parentTaskId,
        decimal? budgetedHours,
        decimal? budgetedCost)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(taskCode))
        {
            throw new ArgumentException("Task code is required.", nameof(taskCode));
        }

        ProjectId = projectId;
        TaskCode = taskCode;
        Description = description;
        ParentTaskId = parentTaskId;
        BudgetedHours = budgetedHours ?? 0;
        BudgetedCost = budgetedCost ?? 0;
        ActualHours = 0;
        ActualCost = 0;
        PercentComplete = 0;
        SortOrder = 0;
    }

    public Guid ProjectId { get; private set; }
    public string TaskCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? ParentTaskId { get; private set; }
    public decimal BudgetedHours { get; private set; }
    public decimal BudgetedCost { get; private set; }
    public decimal ActualHours { get; private set; }
    public decimal ActualCost { get; private set; }
    public decimal PercentComplete { get; private set; }
    public int SortOrder { get; private set; }

    public void Update(
        string? description,
        Guid? parentTaskId,
        decimal? budgetedHours,
        decimal? budgetedCost,
        int? sortOrder)
    {
        if (description is not null)
        {
            Description = description;
        }

        if (parentTaskId.HasValue)
        {
            ParentTaskId = parentTaskId;
        }

        if (budgetedHours.HasValue)
        {
            BudgetedHours = budgetedHours.Value;
        }

        if (budgetedCost.HasValue)
        {
            BudgetedCost = budgetedCost.Value;
        }

        if (sortOrder.HasValue)
        {
            SortOrder = sortOrder.Value;
        }
    }

    public void UpdateActuals(decimal hours, decimal cost)
    {
        ActualHours = hours;
        ActualCost = cost;
    }

    public void UpdatePercentComplete(decimal percent)
    {
        PercentComplete = percent;
    }
}
