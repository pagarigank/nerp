// <copyright file="PurchaseOrder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class PurchaseOrder : AuditableAggregateRoot
{
    private readonly List<PurchaseOrderLine> _lines = [];

    protected PurchaseOrder() { }

    public PurchaseOrder(
        string poNumber,
        Guid companyId,
        Guid vendorId,
        DateTime orderDate,
        PurchaseOrderType orderType,
        string? shipToName,
        string? shipToAddress,
        Guid? paymentTermId,
        Guid? buyerId,
        string? buyerNotes,
        string? vendorReference,
        PurchaseOrderStatus status = PurchaseOrderStatus.Draft)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(poNumber))
            throw new ArgumentException("PO number is required.", nameof(poNumber));

        PONumber = poNumber;
        CompanyId = companyId;
        VendorId = vendorId;
        OrderDate = orderDate;
        OrderType = orderType;
        ShipToName = shipToName;
        ShipToAddress = shipToAddress;
        PaymentTermId = paymentTermId;
        BuyerId = buyerId;
        BuyerNotes = buyerNotes;
        VendorReference = vendorReference;
        Status = status;
    }

    public string PONumber { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public Guid VendorId { get; private set; }

    public DateTime OrderDate { get; private set; }

    public PurchaseOrderType OrderType { get; private set; }

    public string? ShipToName { get; private set; }

    public string? ShipToAddress { get; private set; }

    public Guid? PaymentTermId { get; private set; }

    public Guid? BuyerId { get; private set; }

    public string? BuyerNotes { get; private set; }

    public string? VendorReference { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public DateTime? ApprovedDate { get; private set; }

    public Guid? ApprovedById { get; private set; }

    public DateTime? SentToVendorDate { get; private set; }

    public DateTime? ClosedDate { get; private set; }

    public decimal? BlanketAmountLimit { get; private set; }

    public decimal ReleasedAmount { get; private set; }

    public decimal FreightAmount { get; private set; }

    public decimal FreightTaxAmount { get; private set; }

    public bool TaxExempt { get; private set; }

    public DateTime? PrintedDate { get; private set; }

    public DateTime? EmailedToVendorDate { get; private set; }

    public string? CancellationReason { get; private set; }

    public int RevisionNumber { get; private set; }

    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public void AddLine(PurchaseOrderLine line)
    {
        if (Status != PurchaseOrderStatus.Draft && Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot add lines to PO in {Status} status.");

        _lines.Add(line);
    }

    public void Approve(Guid approvedById)
    {
        if (Status != PurchaseOrderStatus.PendingApproval && Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot approve PO in {Status} status.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot approve PO with no lines.");

        Status = PurchaseOrderStatus.Approved;
        ApprovedDate = DateTime.UtcNow;
        ApprovedById = approvedById;
    }

    public void SubmitForApproval()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot submit PO in {Status} status.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit PO with no lines.");

        Status = PurchaseOrderStatus.PendingApproval;
    }

    public void MarkAsSent()
    {
        if (Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException("Only approved POs can be sent to vendor.");

        SentToVendorDate = DateTime.UtcNow;
    }

    public void Close(string? reason = null)
    {
        if (Status == PurchaseOrderStatus.Closed || Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException($"PO is already {Status}.");

        Status = PurchaseOrderStatus.Closed;
        ClosedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
            CancellationReason = reason;
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));

        if (Status == PurchaseOrderStatus.Closed)
            throw new InvalidOperationException("Cannot cancel a closed PO.");

        Status = PurchaseOrderStatus.Cancelled;
        CancellationReason = reason;
        ClosedDate = DateTime.UtcNow;
    }

    public void CreateChangeOrder()
    {
        if (Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException("Only approved POs can have change orders.");

        RevisionNumber++;
        Status = PurchaseOrderStatus.Draft;
    }

    public decimal GetTotalAmount()
    {
        return _lines.Sum(l => l.GetExtendedPrice());
    }

    public decimal GetRemainingAmount()
    {
        return _lines.Sum(l => l.GetRemainingAmount());
    }

    public bool IsFullyReceived()
    {
        return _lines.All(l => l.IsFullyReceived());
    }

    public bool IsFullyInvoiced()
    {
        return _lines.All(l => l.IsFullyInvoiced());
    }

    public decimal GetTaxTotal()
    {
        return _lines.Sum(l => l.TaxAmount);
    }

    public decimal GetTotalAmountWithTax()
    {
        return GetTotalAmount() + GetTaxTotal() + FreightAmount + FreightTaxAmount;
    }

    public void SetFreight(decimal freightAmount, decimal freightTaxAmount = 0m)
    {
        if (freightAmount < 0)
            throw new ArgumentException("Freight amount cannot be negative.", nameof(freightAmount));
        if (freightTaxAmount < 0)
            throw new ArgumentException("Freight tax amount cannot be negative.", nameof(freightTaxAmount));
        FreightAmount = freightAmount;
        FreightTaxAmount = freightTaxAmount;
    }

    public void SetTaxExempt(bool exempt)
    {
        TaxExempt = exempt;
    }

    public void SetBlanketLimit(decimal? limit)
    {
        BlanketAmountLimit = limit;
    }

    /// <summary>
    /// Draw down against a blanket/standing PO. Tracks cumulative released amount and blocks over-release.
    /// </summary>
    public void Release(decimal amount)
    {
        if (OrderType != PurchaseOrderType.Blanket && OrderType != PurchaseOrderType.Standing)
            throw new InvalidOperationException("Releases are only allowed on blanket or standing POs.");
        if (amount <= 0)
            throw new ArgumentException("Release amount must be greater than zero.", nameof(amount));
        if (BlanketAmountLimit.HasValue && ReleasedAmount + amount > BlanketAmountLimit.Value)
            throw new InvalidOperationException(
                $"Release of {amount} exceeds blanket limit. Limit: {BlanketAmountLimit}, Released: {ReleasedAmount}.");
        ReleasedAmount += amount;
    }

    public void MarkPrinted()
    {
        PrintedDate = DateTime.UtcNow;
    }

    public void MarkEmailedToVendor()
    {
        EmailedToVendorDate = DateTime.UtcNow;
    }
}

public enum PurchaseOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Closed = 3,
    Cancelled = 4,
}

public enum PurchaseOrderType
{
    Standard = 0,
    Blanket = 1,
    Standing = 2,
    DropShip = 3,
}
