// <copyright file="Phase6Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

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
