// <copyright file="ProjectCostPostedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Events;

/// <summary>
/// Raised when a cost is posted to a project. Consumed by GL posting handler
/// for dual-posting: project ledger + company GL.
/// </summary>
public record ProjectCostPostedEvent : DomainEvent
{
    public ProjectCostPostedEvent(
        Guid costTransactionId,
        Guid projectId,
        Guid taskId,
        string costCategory,
        decimal amount,
        Guid companyId)
    {
        CostTransactionId = costTransactionId;
        ProjectId = projectId;
        TaskId = taskId;
        CostCategory = costCategory;
        Amount = amount;
        CompanyId = companyId;
    }

    public Guid CostTransactionId { get; }
    public Guid ProjectId { get; }
    public Guid TaskId { get; }
    public string CostCategory { get; }
    public decimal Amount { get; }
    public Guid CompanyId { get; }
    public override string EventType => "ProjectCostPosted";
}
