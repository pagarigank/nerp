// <copyright file="Requisition.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class Requisition : AuditableAggregateRoot
{
    private readonly List<RequisitionLine> _lines = [];

    protected Requisition() { }

    public Requisition(
        string requisitionNumber,
        Guid companyId,
        Guid requestorId,
        DateTime requestDate,
        DateTime? needByDate,
        string? description,
        RequisitionStatus status = RequisitionStatus.Draft)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(requisitionNumber))
            throw new ArgumentException("Requisition number is required.", nameof(requisitionNumber));

        RequisitionNumber = requisitionNumber;
        CompanyId = companyId;
        RequestorId = requestorId;
        RequestDate = requestDate;
        NeedByDate = needByDate;
        Description = description;
        Status = status;
    }

    public string RequisitionNumber { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public Guid RequestorId { get; private set; }

    public DateTime RequestDate { get; private set; }

    public DateTime? NeedByDate { get; private set; }

    public string? Description { get; private set; }

    public RequisitionStatus Status { get; private set; }

    public DateTime? ApprovedDate { get; private set; }

    public Guid? ApprovedById { get; private set; }

    public DateTime? RejectedDate { get; private set; }

    public Guid? RejectedById { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime? ConvertedDate { get; private set; }

    public IReadOnlyCollection<RequisitionLine> Lines => _lines.AsReadOnly();

    public void AddLine(RequisitionLine line)
    {
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a non-draft requisition.");

        _lines.Add(line);
    }

    public void Approve(Guid approvedById)
    {
        if (Status != RequisitionStatus.PendingApproval && Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot approve requisition in {Status} status.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot approve requisition with no lines.");

        Status = RequisitionStatus.Approved;
        ApprovedDate = DateTime.UtcNow;
        ApprovedById = approvedById;
    }

    public void Reject(Guid rejectedById, string reason)
    {
        if (Status != RequisitionStatus.PendingApproval && Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot reject requisition in {Status} status.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = RequisitionStatus.Rejected;
        RejectedDate = DateTime.UtcNow;
        RejectedById = rejectedById;
        RejectionReason = reason;
    }

    public void SubmitForApproval()
    {
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit requisition in {Status} status.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit requisition with no lines.");

        Status = RequisitionStatus.PendingApproval;
    }

    public void MarkAsConverted()
    {
        if (Status != RequisitionStatus.Approved)
            throw new InvalidOperationException("Only approved requisitions can be converted to PO.");

        Status = RequisitionStatus.ConvertedToPO;
        ConvertedDate = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == RequisitionStatus.ConvertedToPO)
            throw new InvalidOperationException("Cannot cancel requisition that has been converted to PO.");

        Status = RequisitionStatus.Cancelled;
    }

    public decimal GetTotalAmount()
    {
        return _lines.Sum(l => l.Quantity * l.EstimatedUnitPrice);
    }
}

public enum RequisitionStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    ConvertedToPO = 4,
    Cancelled = 5,
}
