// <copyright file="ProjectCommittedCost.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Reserved-but-not-yet-posted cost against a project budget (open POs, open
/// subcontracts, committed labor). Feeds the budget vs. committed vs. actual
/// three-way view (spec §5.7 / §7.3 committed cost).
/// </summary>
public class ProjectCommittedCost : AuditableEntity
{
    protected ProjectCommittedCost() { }

    public ProjectCommittedCost(
        Guid companyId,
        Guid projectId,
        Guid? taskId,
        CostCategory category,
        decimal amount,
        string sourceType,
        string? sourceReference = null,
        DateTime? expectedDate = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ProjectId = projectId;
        TaskId = taskId;
        Category = category;
        Amount = amount;
        SourceType = sourceType;
        SourceReference = sourceReference;
        ExpectedDate = expectedDate;
        IsReleased = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public CostCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string? SourceReference { get; private set; }
    public DateTime? ExpectedDate { get; private set; }
    public bool IsReleased { get; private set; }

    public void Release() => IsReleased = true;
}
