// <copyright file="ApprovalDelegation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// Approver-of-record delegation: a user (the delegator) temporarily grants their
/// approval authority to a substitute (the delegate) for a module / document type /
/// workflow. While active, the delegate may act on approval requests the delegator
/// would otherwise own. Escalation (vacation substitution, stale-approval timers) is
/// handled separately by <see cref="ApprovalEscalationPolicy"/>.
/// </summary>
public class ApprovalDelegation : AuditableAggregateRoot
{
    protected ApprovalDelegation() { }

    public ApprovalDelegation(
        Guid delegatorUserId,
        Guid delegateUserId,
        DateTimeOffset startsOn,
        DateTimeOffset endsOn,
        string? module = null,
        string? documentType = null,
        Guid? workflowId = null) : base(Guid.NewGuid())
    {
        DelegatorUserId = delegatorUserId;
        DelegateUserId = delegateUserId;
        StartsOn = startsOn;
        EndsOn = endsOn;
        Module = module;
        DocumentType = documentType;
        WorkflowId = workflowId;
        IsActive = true;
    }

    public Guid DelegatorUserId { get; private set; }

    public Guid DelegateUserId { get; private set; }

    public string? Module { get; private set; }

    public string? DocumentType { get; private set; }

    public Guid? WorkflowId { get; private set; }

    public DateTimeOffset StartsOn { get; private set; }

    public DateTimeOffset EndsOn { get; private set; }

    public bool IsActive { get; private set; }

    public bool Covers(string? module, string? documentType, Guid? workflowId, DateTimeOffset now)
    {
        if (!IsActive)
            return false;
        if (now < StartsOn || now > EndsOn)
            return false;
        if (Module is not null && !string.Equals(Module, module, StringComparison.OrdinalIgnoreCase))
            return false;
        if (DocumentType is not null && !string.Equals(DocumentType, documentType, StringComparison.OrdinalIgnoreCase))
            return false;
        if (WorkflowId.HasValue && workflowId.HasValue && WorkflowId.Value != workflowId.Value)
            return false;
        return true;
    }

    public void Revoke() => IsActive = false;
}
