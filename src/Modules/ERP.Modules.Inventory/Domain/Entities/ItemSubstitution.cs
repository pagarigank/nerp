// <copyright file="ItemSubstitution.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

/// <summary>
/// Maps a substitute item for a primary item. Used when the primary item is
/// out of stock: the substitute can fulfil demand (customer or vendor side).
/// Approval prevents unauthorized substitution of bill-of-material-critical parts.
/// </summary>
public class ItemSubstitution : AuditableEntity
{
    private ItemSubstitution() { }

    public ItemSubstitution(
        Guid companyId,
        Guid itemId,
        Guid substituteItemId,
        SubstitutionDirection direction,
        string? reason = null,
        bool requiresApproval = false)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        SubstituteItemId = substituteItemId;
        Direction = direction;
        Reason = reason;
        RequiresApproval = requiresApproval;
        Status = requiresApproval ? SubstitutionStatus.Pending : SubstitutionStatus.Approved;
    }

    public Guid CompanyId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid SubstituteItemId { get; private set; }
    public SubstitutionDirection Direction { get; private set; }
    public string? Reason { get; private set; }
    public bool RequiresApproval { get; private set; }
    public SubstitutionStatus Status { get; private set; }

    public void Approve(string approvedBy)
    {
        if (Status != SubstitutionStatus.Pending)
            throw new InvalidOperationException("Only pending substitutions can be approved.");
        Status = SubstitutionStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedOn = DateTime.UtcNow;
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status != SubstitutionStatus.Pending)
            throw new InvalidOperationException("Only pending substitutions can be rejected.");
        Status = SubstitutionStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectionReason = reason;
    }

    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedOn { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    // Navigation
    public Item? Item { get; set; }
    public Item? SubstituteItem { get; set; }
}

public enum SubstitutionDirection
{
    None = 0,
    Customer = 1, // substitute offered to customers
    Vendor = 2,   // substitute sourced from vendors
    Both = 3,
}

public enum SubstitutionStatus
{
    None = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
}
