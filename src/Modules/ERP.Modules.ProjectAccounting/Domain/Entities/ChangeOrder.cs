// <copyright file="ChangeOrder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class ChangeOrder : AuditableEntity
{
    protected ChangeOrder() { }

    public ChangeOrder(
        Guid projectId,
        string description,
        decimal amount,
        CostCategory category,
        string? reason)
        : base(Guid.NewGuid())
    {
        ProjectId = projectId;
        Description = description;
        Amount = amount;
        Category = category;
        Reason = reason;
        Status = ChangeOrderStatus.Draft;
        SubmittedDate = null;
        ApprovedDate = null;
        ApprovedBy = null;
    }

    public Guid ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public CostCategory Category { get; private set; }
    public string? Reason { get; private set; }
    public ChangeOrderStatus Status { get; private set; }
    public DateTime? SubmittedDate { get; private set; }
    public DateTime? ApprovedDate { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    public void UpdateStatus(ChangeOrderStatus status, string? approvedBy = null, string? rejectionReason = null)
    {
        Status = status;
        if (status == ChangeOrderStatus.Submitted)
            SubmittedDate = DateTime.UtcNow;
        if (status == ChangeOrderStatus.Approved)
        {
            ApprovedDate = DateTime.UtcNow;
            ApprovedBy = approvedBy;
        }

        if (status == ChangeOrderStatus.Rejected)
        {
            RejectionReason = rejectionReason;
        }
    }

    public void Update(decimal? amount, string? description, string? reason)
    {
        if (amount.HasValue)
        {
            Amount = amount.Value;
        }

        if (description is not null)
        {
            Description = description;
        }

        if (reason is not null)
        {
            Reason = reason;
        }
    }
}
