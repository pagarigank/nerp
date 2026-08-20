// <copyright file="Phase6Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.Purchasing.Domain.Events;

namespace ERP.Modules.Purchasing.Domain.Entities;

/// <summary>
/// Request-for-quote (RFQ) / vendor quote workflow: solicit pricing from one or more
/// vendors, capture their quoted lines, compare, and award a PO. [GAP-2026-08-18]
/// </summary>
public class VendorQuote : AuditableAggregateRoot
{
    private readonly List<VendorQuoteLine> _lines = [];

    protected VendorQuote() { }

    public VendorQuote(
        string rfxNumber,
        Guid companyId,
        Guid vendorId,
        Guid? requestedById,
        DateTime? validUntil,
        string? notes)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(rfxNumber))
            throw new ArgumentException("RFQ number is required.", nameof(rfxNumber));

        RfxNumber = rfxNumber;
        CompanyId = companyId;
        VendorId = vendorId;
        RequestedById = requestedById;
        ValidUntil = validUntil;
        Notes = notes;
        Status = VendorQuoteStatus.Requested;
    }

    public string RfxNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid? RequestedById { get; private set; }
    public VendorQuoteStatus Status { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public string? Notes { get; private set; }

    // Quoted response (filled when the vendor responds)
    public string? QuoteNumber { get; private set; }
    public DateTime? QuoteDate { get; private set; }
    public decimal QuoteFreight { get; private set; }
    public decimal QuoteTotal => Math.Round(_lines.Sum(l => l.LineTotal) + QuoteFreight, 2, MidpointRounding.AwayFromZero);

    public IReadOnlyList<VendorQuoteLine> Lines => _lines.AsReadOnly();

    public void AddLine(string? itemId, string description, decimal quantity, string unitOfMeasure, decimal unitPrice)
    {
        if (Status != VendorQuoteStatus.Requested && Status != VendorQuoteStatus.Received)
            throw new InvalidOperationException($"Cannot add lines to a quote in {Status} status.");
        _lines.Add(new VendorQuoteLine(Id, itemId, description, quantity, unitOfMeasure, unitPrice));
    }

    public void ReceiveQuote(string quoteNumber, DateTime quoteDate, decimal freight, IReadOnlyList<VendorQuoteLine>? lines = null)
    {
        if (Status != VendorQuoteStatus.Requested)
            throw new InvalidOperationException($"Cannot receive a quote in {Status} status.");
        QuoteNumber = quoteNumber;
        QuoteDate = quoteDate;
        QuoteFreight = freight;
        if (lines != null)
        {
            _lines.Clear();
            foreach (var l in lines)
                _lines.Add(l);
        }

        Status = VendorQuoteStatus.Received;
    }

    public void Award()
    {
        if (Status != VendorQuoteStatus.Received)
            throw new InvalidOperationException("Only received quotes can be awarded.");
        Status = VendorQuoteStatus.Awarded;
    }

    public void Reject(string? reason = null)
    {
        if (Status == VendorQuoteStatus.Awarded)
            throw new InvalidOperationException("Cannot reject an awarded quote.");
        Notes = reason;
        Status = VendorQuoteStatus.Rejected;
    }
}

public class VendorQuoteLine : AuditableEntity
{
    protected VendorQuoteLine() { }

    public VendorQuoteLine(Guid vendorQuoteId, string? itemId, string description, decimal quantity, string unitOfMeasure, decimal unitPrice)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        VendorQuoteId = vendorQuoteId;
        ItemId = itemId;
        Description = description;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
    }

    public Guid VendorQuoteId { get; private set; }
    public string? ItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
}

public enum VendorQuoteStatus
{
    Requested = 0,
    Received = 1,
    Awarded = 2,
    Rejected = 3,
}

/// <summary>
/// A goods receipt entered without a purchase order, for example an invoice
/// received first (IFU) or a miscellaneous charge. Such receipts require
/// explicit approval before posting so they can be tied to a cost center /
/// project budget (spec §6: Receipt-without-PO workflow).
/// </summary>
public class ReceiptWithoutPO : AuditableAggregateRoot
{
    private readonly List<ReceiptWithoutPOLine> _lines = [];

    protected ReceiptWithoutPO() { }

    public ReceiptWithoutPO(
        string receiptNumber,
        Guid companyId,
        Guid vendorId,
        DateTime receivedDate,
        string? receivedBy,
        string? packingSlipNumber,
        string? notes)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("Receipt number is required.", nameof(receiptNumber));

        ReceiptNumber = receiptNumber;
        CompanyId = companyId;
        VendorId = vendorId;
        ReceivedDate = receivedDate;
        ReceivedBy = receivedBy;
        PackingSlipNumber = packingSlipNumber;
        Notes = notes;
        Status = ReceiptWithoutPOStatus.Draft;
    }

    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid VendorId { get; private set; }
    public DateTime ReceivedDate { get; private set; }
    public string? ReceivedBy { get; private set; }
    public string? PackingSlipNumber { get; private set; }
    public string? Notes { get; private set; }
    public ReceiptWithoutPOStatus Status { get; private set; }
    public DateTime? PostedDate { get; private set; }
    public bool IsReversed { get; private set; }
    public DateTime? ReversedDate { get; private set; }
    public string? ReversalReason { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }

    public IReadOnlyList<ReceiptWithoutPOLine> Lines => _lines.AsReadOnly();

    public void AddLine(ReceiptWithoutPOLine line)
    {
        if (Status != ReceiptWithoutPOStatus.Draft)
            throw new InvalidOperationException($"Cannot add lines to a receipt in {Status} status.");
        _lines.Add(line);
    }

    public void MarkPendingApproval(Guid approvalRequestId)
    {
        if (Status != ReceiptWithoutPOStatus.Draft)
            throw new InvalidOperationException($"Cannot submit a receipt in {Status} status.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit a receipt with no lines.");

        ApprovalRequestId = approvalRequestId;
        Status = ReceiptWithoutPOStatus.PendingApproval;
    }

    public void Post()
    {
        if (Status != ReceiptWithoutPOStatus.Approved)
            throw new InvalidOperationException($"Cannot post receipt in {Status} status.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot post receipt with no lines.");

        Status = ReceiptWithoutPOStatus.Posted;
        PostedDate = DateTime.UtcNow;

        var lines = _lines
            .Select(l => new GoodsReceivedLine
            {
                ReceiptLineId = l.Id,
                PurchaseOrderLineId = null,
                ItemId = l.ItemId,
                Description = l.Description,
                QuantityReceived = l.QuantityReceived,
                UnitOfMeasure = l.UnitOfMeasure,
                ProjectId = l.ProjectId,
                TaskId = l.TaskId,
            })
            .ToList();

        AddDomainEvent(new GoodsReceivedEvent(
            Id, ReceiptNumber, CompanyId, null, VendorId, ReceivedDate, lines));
    }

    public void Approve(Guid approvedById)
    {
        if (Status != ReceiptWithoutPOStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve receipt in {Status} status.");

        Status = ReceiptWithoutPOStatus.Approved;
    }

    public decimal GetTotalAmount() => Math.Round(_lines.Sum(l => l.ExtendedAmount), 2, MidpointRounding.AwayFromZero);

    public void Reverse(string reason)
    {
        if (Status != ReceiptWithoutPOStatus.Posted)
            throw new InvalidOperationException("Only posted receipts can be reversed.");

        if (IsReversed)
            throw new InvalidOperationException("Receipt is already reversed.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reversal reason is required.", nameof(reason));

        IsReversed = true;
        ReversedDate = DateTime.UtcNow;
        ReversalReason = reason;
        Status = ReceiptWithoutPOStatus.Reversed;
    }
}

public enum ReceiptWithoutPOStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Posted = 3,
    Reversed = 4,
}

public class ReceiptWithoutPOLine : AuditableEntity
{
    protected ReceiptWithoutPOLine() { }

    public ReceiptWithoutPOLine(
        Guid receiptId,
        int lineNumber,
        string? itemId,
        string description,
        decimal quantityReceived,
        string unitOfMeasure,
        decimal unitPrice,
        Guid? accountId,
        Guid? projectId,
        Guid? taskId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (quantityReceived <= 0)
            throw new ArgumentException("Quantity received must be greater than zero.", nameof(quantityReceived));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
            throw new ArgumentException("Unit of measure is required.", nameof(unitOfMeasure));

        ReceiptId = receiptId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description;
        QuantityReceived = quantityReceived;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        AccountId = accountId;
        ProjectId = projectId;
        TaskId = taskId;
    }

    public Guid ReceiptId { get; private set; }
    public int LineNumber { get; private set; }
    public string? ItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal QuantityReceived { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }

    public decimal ExtendedAmount => Math.Round(QuantityReceived * UnitPrice, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Record of an over-receipt that exceeded tolerance and requires buyer-manager
/// approval before the receipt can be finalized (spec §6: Over-receipt exception
/// approval workflow).
/// </summary>
public class OverReceiptApproval : AuditableEntity
{
    protected OverReceiptApproval() { }

    public OverReceiptApproval(
        Guid companyId,
        Guid receiptId,
        string receiptNumber,
        Guid purchaseOrderId,
        Guid purchaseOrderLineId,
        decimal orderedQuantity,
        decimal receivedQuantity,
        decimal overReceiptTolerance)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ReceiptId = receiptId;
        ReceiptNumber = receiptNumber;
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderLineId = purchaseOrderLineId;
        OrderedQuantity = orderedQuantity;
        ReceivedQuantity = receivedQuantity;
        OverReceiptTolerance = overReceiptTolerance;
        Status = OverReceiptApprovalStatus.Pending;
    }

    public Guid CompanyId { get; private set; }
    public Guid ReceiptId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid PurchaseOrderId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal OverReceiptTolerance { get; private set; }
    public OverReceiptApprovalStatus Status { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }

    public bool IsWithinTolerance => ReceivedQuantity <= OrderedQuantity * (1 + OverReceiptTolerance);

    public void SetApprovalRequest(Guid approvalRequestId)
    {
        ApprovalRequestId = approvalRequestId;
    }

    public void Resolve(OverReceiptApprovalStatus status)
    {
        if (Status != OverReceiptApprovalStatus.Pending)
            throw new InvalidOperationException($"Cannot change status from {Status}.");
        Status = status;
    }
}

public enum OverReceiptApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
