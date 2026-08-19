// <copyright file="Receipt.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.Purchasing.Domain.Events;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class Receipt : AuditableAggregateRoot
{
    private readonly List<ReceiptLine> _lines = [];

    protected Receipt() { }

    public Receipt(
        string receiptNumber,
        Guid companyId,
        Guid? purchaseOrderId,
        Guid? vendorId,
        DateTime receivedDate,
        string? receivedBy,
        string? packingSlipNumber,
        string? notes,
        ReceiptStatus status = ReceiptStatus.Draft)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("Receipt number is required.", nameof(receiptNumber));

        ReceiptNumber = receiptNumber;
        CompanyId = companyId;
        PurchaseOrderId = purchaseOrderId;
        VendorId = vendorId;
        ReceivedDate = receivedDate;
        ReceivedBy = receivedBy;
        PackingSlipNumber = packingSlipNumber;
        Notes = notes;
        Status = status;
    }

    public string ReceiptNumber { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }

    public Guid? VendorId { get; private set; }

    public DateTime ReceivedDate { get; private set; }

    public string? ReceivedBy { get; private set; }

    public string? PackingSlipNumber { get; private set; }

    public string? Notes { get; private set; }

    public ReceiptStatus Status { get; private set; }

    public DateTime? PostedDate { get; private set; }

    public bool IsReversed { get; private set; }

    public DateTime? ReversedDate { get; private set; }

    public string? ReversalReason { get; private set; }

    public IReadOnlyCollection<ReceiptLine> Lines => _lines.AsReadOnly();

    public void AddLine(ReceiptLine line)
    {
        if (Status != ReceiptStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a posted or reversed receipt.");

        _lines.Add(line);
    }

    public void Post()
    {
        if (Status != ReceiptStatus.Draft)
            throw new InvalidOperationException($"Cannot post receipt in {Status} status.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot post receipt with no lines.");

        Status = ReceiptStatus.Posted;
        PostedDate = DateTime.UtcNow;

        var lines = _lines
            .Select(l => new GoodsReceivedLine
            {
                ReceiptLineId = l.Id,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ItemId = l.ItemId,
                Description = l.Description,
                QuantityReceived = l.QuantityReceived,
                UnitOfMeasure = l.UnitOfMeasure,
            })
            .ToList();

        AddDomainEvent(new GoodsReceivedEvent(
            Id, ReceiptNumber, CompanyId, PurchaseOrderId, VendorId, ReceivedDate, lines));
    }

    public void Reverse(string reason)
    {
        if (Status != ReceiptStatus.Posted)
            throw new InvalidOperationException("Only posted receipts can be reversed.");

        if (IsReversed)
            throw new InvalidOperationException("Receipt is already reversed.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reversal reason is required.", nameof(reason));

        IsReversed = true;
        ReversedDate = DateTime.UtcNow;
        ReversalReason = reason;
        Status = ReceiptStatus.Reversed;
    }
}

public enum ReceiptStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2,
}
