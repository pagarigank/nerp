// <copyright file="ApprovalWorkflow.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class ApprovalWorkflow : AuditableAggregateRoot
{
    private readonly List<ApprovalStep> _steps = [];

    protected ApprovalWorkflow() { }

    public ApprovalWorkflow(
        string module,
        string documentType,
        string description,
        Guid? companyId = null,
        decimal? thresholdAmount = null) : base(Guid.NewGuid())
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        DocumentType = documentType ?? throw new ArgumentNullException(nameof(documentType));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        CompanyId = companyId;
        ThresholdAmount = thresholdAmount;
        IsActive = true;
    }

    public string Module { get; private set; } = string.Empty;

    public string DocumentType { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? CompanyId { get; private set; }

    public bool IsActive { get; private set; }

    public decimal? ThresholdAmount { get; private set; }

    public IReadOnlyList<ApprovalStep> Steps => _steps.AsReadOnly();

    public void Update(string description, decimal? thresholdAmount)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ThresholdAmount = thresholdAmount;
    }

    public void AddStep(int stepOrder, string stepDescription, Guid? approverRoleId, Guid? specificApproverUserId, int requiredApprovals = 1, decimal? minAmount = null, decimal? maxAmount = null)
    {
        var step = new ApprovalStep(Id, stepOrder, stepDescription, approverRoleId, specificApproverUserId, requiredApprovals, minAmount, maxAmount);
        _steps.Add(step);
    }

    public void RemoveStep(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step != null)
        {
            _steps.Remove(step);
        }
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
