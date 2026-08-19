// <copyright file="ApprovalEscalationPolicy.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// Escalation policy for a workflow step. When an approval request sits in a step
/// longer than <see cref="SlaMinutes"/> without the required approvals, it is
/// escalated to a fallback approver (role and/or specific user). Supports
/// multi-level escalation by chaining policies per step.
/// </summary>
public class ApprovalEscalationPolicy : AuditableAggregateRoot
{
    protected ApprovalEscalationPolicy() { }

    public ApprovalEscalationPolicy(
        Guid workflowId,
        int stepOrder,
        int slaMinutes,
        Guid? escalateToRoleId = null,
        Guid? escalateToUserId = null,
        bool notifyOnEscalation = true) : base(Guid.NewGuid())
    {
        WorkflowId = workflowId;
        StepOrder = stepOrder;
        SlaMinutes = slaMinutes;
        EscalateToRoleId = escalateToRoleId;
        EscalateToUserId = escalateToUserId;
        NotifyOnEscalation = notifyOnEscalation;
        IsActive = true;
    }

    public Guid WorkflowId { get; private set; }

    public int StepOrder { get; private set; }

    /// <summary>Minutes a request may sit in the step before escalation fires.</summary>
    public int SlaMinutes { get; private set; }

    public Guid? EscalateToRoleId { get; private set; }

    public Guid? EscalateToUserId { get; private set; }

    public bool NotifyOnEscalation { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(int slaMinutes, Guid? escalateToRoleId, Guid? escalateToUserId, bool notifyOnEscalation)
    {
        SlaMinutes = slaMinutes;
        EscalateToRoleId = escalateToRoleId;
        EscalateToUserId = escalateToUserId;
        NotifyOnEscalation = notifyOnEscalation;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
