// <copyright file="ApprovalRequest.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Withdrawn = 3,
    PartiallyApproved = 4,
}

public class ApprovalRequest : AuditableAggregateRoot
{
    private readonly List<ApprovalAction> _actions = [];

    protected ApprovalRequest() { }

    public ApprovalRequest(
        Guid workflowId,
        string module,
        string documentType,
        Guid documentId,
        string documentNumber,
        decimal amount,
        string requestedBy,
        int currentStep = 1,
        string? notes = null) : base(Guid.NewGuid())
    {
        WorkflowId = workflowId;
        Module = module ?? throw new ArgumentNullException(nameof(module));
        DocumentType = documentType ?? throw new ArgumentNullException(nameof(documentType));
        DocumentId = documentId;
        DocumentNumber = documentNumber ?? throw new ArgumentNullException(nameof(documentNumber));
        Amount = amount;
        RequestedBy = requestedBy ?? throw new ArgumentNullException(nameof(requestedBy));
        CurrentStep = currentStep;
        Notes = notes;
        Status = ApprovalStatus.Pending;
    }

    public Guid WorkflowId { get; private set; }

    public string Module { get; private set; } = string.Empty;

    public string DocumentType { get; private set; } = string.Empty;

    public Guid DocumentId { get; private set; }

    public string DocumentNumber { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string RequestedBy { get; private set; } = string.Empty;

    public ApprovalStatus Status { get; private set; }

    public int CurrentStep { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyList<ApprovalAction> Actions => _actions.AsReadOnly();

    public void AddAction(string actionedBy, ApprovalDecision decision, Guid? stepId = null, string? comments = null)
    {
        var action = new ApprovalAction(Id, stepId, actionedBy, decision, comments);
        _actions.Add(action);

        if (decision == ApprovalDecision.Rejected)
        {
            Status = ApprovalStatus.Rejected;
        }
    }

    public void AdvanceStep()
    {
        CurrentStep++;
    }

    public void Approve()
    {
        Status = ApprovalStatus.Approved;
    }

    public void Reject()
    {
        Status = ApprovalStatus.Rejected;
    }

    public void Withdraw()
    {
        Status = ApprovalStatus.Withdrawn;
    }

    public void MarkPartiallyApproved()
    {
        Status = ApprovalStatus.PartiallyApproved;
    }
}
