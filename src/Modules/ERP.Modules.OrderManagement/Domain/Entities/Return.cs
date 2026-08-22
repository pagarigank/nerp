// <copyright file="Return.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Events;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Customer return (RMA). A confirmed return emits a <see cref="ReturnConfirmedEvent"/>
/// consumed by the Inventory module (restock the returned items) and the AR module
/// (generate a credit memo against the original shipment -> GL). This is the reverse
/// leg of the Sales -> Inventory / Sales -> AR integration.
/// </summary>
public class Return : AuditableAggregateRoot
{
    private readonly List<ReturnLine> _lines = [];

    /// <summary>Returns above this value must pass through the approval workflow before confirmation.</summary>
    public const decimal ApprovalThreshold = 1000m;

    protected Return() { }

    public Return(
        string returnNumber,
        Guid companyId,
        Guid customerId,
        Guid? shipmentId,
        Guid? salesOrderId,
        DateTime returnDate,
        string? reasonCode = null,
        string? note = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
            throw new ArgumentException("Return number is required.", nameof(returnNumber));

        ReturnNumber = returnNumber;
        CompanyId = companyId;
        CustomerId = customerId;
        ShipmentId = shipmentId;
        SalesOrderId = salesOrderId;
        ReturnDate = returnDate;
        ReasonCode = reasonCode;
        Note = note;
        Status = ReturnStatus.Draft;
    }

    public string ReturnNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? ShipmentId { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Note { get; private set; }
    public ReturnStatus Status { get; private set; }

    /// <summary>Set when a return above the approval threshold has been approved and may be confirmed.</summary>
    public bool IsApproved { get; private set; }

    public string? ApprovedBy { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<ReturnLine> Lines => _lines.AsReadOnly();

    public bool RequiresApproval => GetReturnValue() > ApprovalThreshold;

    /// <summary>Gross return value: sum of line quantity x unit price.</summary>
    public decimal GetReturnValue() => _lines.Sum(l => l.ExtendedPrice);

    public void AddLine(ReturnLine line)
    {
        if (Status != ReturnStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a non-draft return.");
        _lines.Add(line);
    }

    public void SubmitForApproval()
    {
        if (Status != ReturnStatus.Draft)
            throw new InvalidOperationException($"Cannot submit a return in {Status} status for approval.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit a return with no lines for approval.");

        Status = ReturnStatus.PendingApproval;
    }

    public void Approve(string approvedBy)
    {
        if (Status != ReturnStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve a return in {Status} status.");
        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("Approver is required.", nameof(approvedBy));

        IsApproved = true;
        ApprovedBy = approvedBy;
        RejectionReason = null;
        Status = ReturnStatus.Draft;
    }

    public void Reject(string reason)
    {
        if (Status != ReturnStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject a return in {Status} status.");

        IsApproved = false;
        ApprovedBy = null;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected without reason." : reason;
        Status = ReturnStatus.Draft;
    }

    public void Confirm()
    {
        if (Status != ReturnStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm return in {Status} status.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot confirm a return with no lines.");
        if (GetReturnValue() > ApprovalThreshold && !IsApproved)
        {
            throw new InvalidOperationException(
                $"Return value {GetReturnValue():C} exceeds the {ApprovalThreshold:C} threshold; submit it for approval first.");
        }

        Status = ReturnStatus.Confirmed;
        AddDomainEvent(new ReturnConfirmedEvent(
            Id, ReturnNumber, CompanyId, CustomerId, ShipmentId, SalesOrderId, ReturnDate, ReasonCode, Lines.ToList()));
    }
}

public enum ReturnStatus
{
    Draft = 0,
    Confirmed = 1,
    Received = 2,
    Cancelled = 3,
    PendingApproval = 4,
}
