// <copyright file="CostAdjustment.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Correction of a misposted cost, or a project-to-project transfer. A new,
/// reversed-sign cost transaction is posted to the source and (for transfers)
/// a matching cost to the destination. Requires approval (spec §7.3 cost
/// adjustment).
/// </summary>
public class CostAdjustment : AuditableEntity
{
    protected CostAdjustment() { }

    public CostAdjustment(
        Guid companyId,
        Guid sourceProjectId,
        Guid sourceCostTransactionId,
        decimal adjustmentAmount,
        string reason,
        Guid? destinationProjectId = null,
        string? approvedBy = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        SourceProjectId = sourceProjectId;
        SourceCostTransactionId = sourceCostTransactionId;
        AdjustmentAmount = adjustmentAmount;
        Reason = reason;
        DestinationProjectId = destinationProjectId;
        ApprovedBy = approvedBy;
        Status = AdjustmentStatus.Pending;
    }

    public Guid CompanyId { get; private set; }
    public Guid SourceProjectId { get; private set; }
    public Guid SourceCostTransactionId { get; private set; }
    public Guid? DestinationProjectId { get; private set; }
    public decimal AdjustmentAmount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public AdjustmentStatus Status { get; private set; }
    public Guid? ReversingCostTransactionId { get; private set; }
    public Guid? DestinationCostTransactionId { get; private set; }

    public void Approve(string approvedBy, Guid reversingCostTxnId, Guid? destinationCostTxnId = null)
    {
        if (Status == AdjustmentStatus.Approved)
            throw new InvalidOperationException("Adjustment already approved.");
        Status = AdjustmentStatus.Approved;
        ApprovedBy = approvedBy;
        ReversingCostTransactionId = reversingCostTxnId;
        DestinationCostTransactionId = destinationCostTxnId;
    }

    public void Reject(string? reason = null)
    {
        Status = AdjustmentStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(reason))
            Reason = reason;
    }
}

public enum AdjustmentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
