// <copyright file="ApprovalStep.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class ApprovalStep : Entity
{
    protected ApprovalStep() { }

    public ApprovalStep(
        Guid workflowId,
        int stepOrder,
        string description,
        Guid? approverRoleId,
        Guid? specificApproverUserId,
        int requiredApprovals = 1,
        decimal? minAmount = null,
        decimal? maxAmount = null) : base(Guid.NewGuid())
    {
        WorkflowId = workflowId;
        StepOrder = stepOrder;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ApproverRoleId = approverRoleId;
        SpecificApproverUserId = specificApproverUserId;
        RequiredApprovals = requiredApprovals;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }

    public Guid WorkflowId { get; private set; }

    public int StepOrder { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? ApproverRoleId { get; private set; }

    public Guid? SpecificApproverUserId { get; private set; }

    public int RequiredApprovals { get; private set; }

    public decimal? MinAmount { get; private set; }

    public decimal? MaxAmount { get; private set; }

    public void Update(string description, int stepOrder, Guid? approverRoleId, Guid? specificApproverUserId, int requiredApprovals, decimal? minAmount, decimal? maxAmount)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        StepOrder = stepOrder;
        ApproverRoleId = approverRoleId;
        SpecificApproverUserId = specificApproverUserId;
        RequiredApprovals = requiredApprovals;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }
}
