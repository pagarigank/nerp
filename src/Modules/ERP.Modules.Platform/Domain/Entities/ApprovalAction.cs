// <copyright file="ApprovalAction.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public enum ApprovalDecision
{
    Approved = 0,
    Rejected = 1,
}

public class ApprovalAction : Entity
{
    protected ApprovalAction() { }

    public ApprovalAction(
        Guid requestId,
        Guid? stepId,
        string actionedBy,
        ApprovalDecision decision,
        string? comments = null) : base(Guid.NewGuid())
    {
        RequestId = requestId;
        StepId = stepId;
        ActionedBy = actionedBy ?? throw new ArgumentNullException(nameof(actionedBy));
        Decision = decision;
        Comments = comments;
        ActionedOn = DateTimeOffset.UtcNow;
    }

    public Guid RequestId { get; private set; }

    public Guid? StepId { get; private set; }

    public string ActionedBy { get; private set; } = string.Empty;

    public ApprovalDecision Decision { get; private set; }

    public string? Comments { get; private set; }

    public DateTimeOffset ActionedOn { get; private set; }
}
