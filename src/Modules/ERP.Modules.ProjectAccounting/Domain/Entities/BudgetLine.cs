// <copyright file="BudgetLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class BudgetLine : AuditableEntity
{
    protected BudgetLine() { }

    public BudgetLine(
        Guid projectId,
        Guid taskId,
        CostCategory category,
        decimal budgetAmount,
        decimal? budgetedHours,
        string? description,
        bool isRevised = false,
        int revisionNumber = 0)
        : base(Guid.NewGuid())
    {
        ProjectId = projectId;
        TaskId = taskId;
        Category = category;
        BudgetAmount = budgetAmount;
        BudgetedHours = budgetedHours ?? 0;
        ActualAmount = 0;
        ActualHours = 0;
        CommittedAmount = 0;
        Description = description;
        IsRevised = isRevised;
        RevisionNumber = revisionNumber;
    }

    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public CostCategory Category { get; private set; }
    public decimal BudgetAmount { get; private set; }
    public decimal BudgetedHours { get; private set; }
    public decimal ActualAmount { get; private set; }
    public decimal ActualHours { get; private set; }
    public decimal CommittedAmount { get; private set; }
    public string? Description { get; private set; }
    public bool IsRevised { get; private set; }
    public int RevisionNumber { get; private set; }
    public decimal Variance => BudgetAmount - ActualAmount - CommittedAmount;
    public decimal VariancePercent => BudgetAmount != 0 ? (Variance / BudgetAmount) * 100 : 0;

    public void Update(decimal? budgetAmount, decimal? budgetedHours, string? description)
    {
        if (budgetAmount.HasValue)
        {
            BudgetAmount = budgetAmount.Value;
        }

        if (budgetedHours.HasValue)
        {
            BudgetedHours = budgetedHours.Value;
        }

        if (description is not null)
        {
            Description = description;
        }
    }

    public void UpdateActuals(decimal amount, decimal hours)
    {
        ActualAmount = amount;
        ActualHours = hours;
    }

    public void UpdateCommitted(decimal committedAmount)
    {
        CommittedAmount = committedAmount;
    }
}
