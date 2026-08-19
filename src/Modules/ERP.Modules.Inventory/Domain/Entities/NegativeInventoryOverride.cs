// <copyright file="NegativeInventoryOverride.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class NegativeInventoryOverride : AuditableEntity
{
    protected NegativeInventoryOverride() { }

    public NegativeInventoryOverride(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        Guid? binId,
        decimal requestedQuantity,
        string unitOfMeasure,
        string reason,
        Guid requestedBy,
        string? referenceNumber = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        BinId = binId;
        RequestedQuantity = requestedQuantity;
        UnitOfMeasure = unitOfMeasure;
        Reason = reason;
        RequestedBy = requestedBy;
        ReferenceNumber = referenceNumber;
        Status = NegativeInventoryOverrideStatus.Pending;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? BinId { get; private set; }

    public decimal RequestedQuantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public Guid RequestedBy { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public NegativeInventoryOverrideStatus Status { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedDate { get; private set; }

    public string? ApprovalNotes { get; private set; }

    public Guid? RejectedBy { get; private set; }

    public DateTime? RejectedDate { get; private set; }

    public string? RejectionReason { get; private set; }

    public void Approve(Guid approvedBy, string? notes = null)
    {
        if (Status != NegativeInventoryOverrideStatus.Pending)
        {
            throw new InvalidOperationException("Only pending overrides can be approved.");
        }

        Status = NegativeInventoryOverrideStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedDate = DateTime.UtcNow;
        ApprovalNotes = notes;
    }

    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status != NegativeInventoryOverrideStatus.Pending)
        {
            throw new InvalidOperationException("Only pending overrides can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        Status = NegativeInventoryOverrideStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectedDate = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public void Cancel(Guid cancelledBy)
    {
        if (Status != NegativeInventoryOverrideStatus.Pending)
        {
            throw new InvalidOperationException("Only pending overrides can be cancelled.");
        }

        Status = NegativeInventoryOverrideStatus.Cancelled;
    }
}

public enum NegativeInventoryOverrideStatus
{
    None = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
}