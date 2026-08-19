// <copyright file="SalesOrder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Events;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Sales order (Phase 8 — Order Management). The order is confirmed (reserved
/// against inventory) and later fulfilled by one or more shipments. Confirms emit
/// a <see cref="SalesOrderConfirmedEvent"/>; shipments emit
/// <see cref="ShipmentConfirmedEvent"/> which the Inventory and AR modules consume
/// to decrement stock and generate the customer invoice.
/// </summary>
public class SalesOrder : AuditableAggregateRoot
{
    private readonly List<SalesOrderLine> _lines = [];

    /// <summary>Maximum line discount (percent) allowed without manager approval.</summary>
    public const decimal DiscountApprovalThreshold = 20m;

    protected SalesOrder() { }

    public SalesOrder(
        string orderNumber,
        Guid companyId,
        Guid customerId,
        DateTime orderDate,
        string? shipToAddress = null,
        string? billToAddress = null,
        string? paymentTermId = null,
        string? salesRepId = null,
        string? shippingMethod = null,
        string? customerPoNumber = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));

        OrderNumber = orderNumber;
        CompanyId = companyId;
        CustomerId = customerId;
        OrderDate = orderDate;
        ShipToAddress = shipToAddress;
        BillToAddress = billToAddress;
        PaymentTermId = paymentTermId;
        SalesRepId = salesRepId;
        ShippingMethod = shippingMethod;
        CustomerPoNumber = customerPoNumber;
        Status = SalesOrderStatus.Draft;
    }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public string? ShipToAddress { get; private set; }
    public string? BillToAddress { get; private set; }
    public string? PaymentTermId { get; private set; }
    public string? SalesRepId { get; private set; }
    public string? ShippingMethod { get; private set; }
    public string? CustomerPoNumber { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public DateTime? ConfirmedDate { get; private set; }
    public bool IsOnCreditHold { get; private set; }
    public string? CreditHoldReason { get; private set; }

    /// <summary>True when at least one line exceeds the discount-approval threshold and the order has not yet been approved.</summary>
    public bool RequiresDiscountApproval { get; private set; }
    public bool DiscountApproved { get; private set; }
    public string? DiscountApprovedBy { get; private set; }

    // --- Quote lifecycle (Phase 8 gap 582). When the order type is a quote, the
    // order moves through Draft -> Sent -> Accepted/Rejected and can be converted
    // into a real (Order-type) sales order. RevisionNumber tracks re-issued quotes.
    public bool IsQuote { get; private set; }
    public QuoteStatus QuoteStatus { get; private set; }
    public DateTime? QuoteSentDate { get; private set; }
    public DateTime? QuoteAcceptedDate { get; private set; }
    public DateTime? QuoteExpiryDate { get; private set; }
    public int RevisionNumber { get; private set; }
    public Guid? ConvertedOrderId { get; private set; }

    public decimal RemainingToShip => Lines.Sum(l => Math.Max(0m, l.Quantity - l.ShippedQuantity));

    public IReadOnlyCollection<SalesOrderLine> Lines => _lines.AsReadOnly();

    public void AddLine(SalesOrderLine line)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a non-draft sales order.");
        _lines.Add(line);
        RecomputeDiscountApproval();
    }

    public void Confirm()
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm sales order in {Status} status.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot confirm a sales order with no lines.");
        if (IsOnCreditHold)
            throw new InvalidOperationException("Cannot confirm a sales order that is on credit hold.");
        if (RequiresDiscountApproval && !DiscountApproved)
            throw new InvalidOperationException($"Order has a discount over {DiscountApprovalThreshold}% that requires manager approval before confirmation.");

        Status = SalesOrderStatus.Confirmed;
        ConfirmedDate = DateTime.UtcNow;
        AddDomainEvent(new SalesOrderConfirmedEvent(Id, OrderNumber, CompanyId, CustomerId, Lines.ToList()));
    }

    public void Cancel()
    {
        if (Status == SalesOrderStatus.Closed)
            throw new InvalidOperationException("Cannot cancel a closed sales order.");
        Status = SalesOrderStatus.Cancelled;
    }

    /// <summary>
    /// Change-order edit: updates an existing draft line's quantity, price and
    /// distribution. Used by the order-entry UI after a customer revision. Lines
    /// can only be edited while the order is still in Draft status (pre-confirmation).
    /// </summary>
    public void UpdateLine(
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        Guid? warehouseId,
        Guid? projectId,
        Guid? accountId,
        string? description)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Cannot edit lines on a sales order that is not in Draft status.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Line {lineId} not found on this sales order.");
        line.Update(quantity, unitPrice, discountPercent, taxPercent, warehouseId, projectId, accountId, description);
        RecomputeDiscountApproval();
    }

    private void RecomputeDiscountApproval()
    {
        RequiresDiscountApproval = _lines.Any(l => l.DiscountPercent > DiscountApprovalThreshold);
        if (!RequiresDiscountApproval)
        {
            DiscountApproved = false;
            DiscountApprovedBy = null;
        }
    }

    /// <summary>
    /// Approves an order whose line discounts exceed the manager-approval threshold
    /// (see <see cref="DiscountApprovalThreshold"/>). Records the approver for audit.
    /// </summary>
    public void MarkDiscountApproved(string approvedBy)
    {
        if (!RequiresDiscountApproval)
            return;
        DiscountApproved = true;
        DiscountApprovedBy = approvedBy;
    }

    /// <summary>
    /// Add a replacement line to a confirmed order (change order). The new line is
    /// added in Draft so it must be re-confirmed to reserve stock.
    /// </summary>
    public void AddReplacementLine(SalesOrderLine line)
    {
        if (Status != SalesOrderStatus.Confirmed && Status != SalesOrderStatus.PartiallyShipped)
            throw new InvalidOperationException("Replacement lines can only be added to a confirmed or partially-shipped order.");

        _lines.Add(line);
    }

    public void PlaceCreditHold(string reason)
    {
        if (Status == SalesOrderStatus.Confirmed || Status == SalesOrderStatus.Closed)
            throw new InvalidOperationException("Cannot place a credit hold on a confirmed or closed order.");
        IsOnCreditHold = true;
        CreditHoldReason = reason;
    }

    public void ReleaseCreditHold()
    {
        IsOnCreditHold = false;
        CreditHoldReason = null;
    }

    // --- Quote lifecycle (Phase 8 gap 582)
    public void ConfigureAsQuote(DateTime? expiryDate = null)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Quote configuration is only allowed while the order is in Draft.");
        IsQuote = true;
        QuoteStatus = QuoteStatus.Draft;
        QuoteExpiryDate = expiryDate;
    }

    public void SendQuote()
    {
        if (!IsQuote)
            throw new InvalidOperationException("This sales order is not configured as a quote.");
        if (QuoteStatus != QuoteStatus.Draft)
            throw new InvalidOperationException($"Cannot send a quote in {QuoteStatus} status.");
        QuoteStatus = QuoteStatus.Sent;
        QuoteSentDate = DateTime.UtcNow;
    }

    public void AcceptQuote()
    {
        if (QuoteStatus != QuoteStatus.Sent)
            throw new InvalidOperationException($"Can only accept a quote in Sent status (current: {QuoteStatus}).");
        QuoteStatus = QuoteStatus.Accepted;
        QuoteAcceptedDate = DateTime.UtcNow;
    }

    public void RejectQuote()
    {
        if (QuoteStatus != QuoteStatus.Sent)
            throw new InvalidOperationException($"Can only reject a quote in Sent status (current: {QuoteStatus}).");
        QuoteStatus = QuoteStatus.Rejected;
    }

    /// <summary>
    /// Re-issue the quote at a new revision (e.g. price/terms change after customer
    /// feedback). Keeps the same order number but bumps the revision and resets to Draft.
    /// </summary>
    public void ReviseQuote()
    {
        if (!IsQuote)
            throw new InvalidOperationException("This sales order is not configured as a quote.");
        RevisionNumber += 1;
        QuoteStatus = QuoteStatus.Draft;
        QuoteSentDate = null;
        QuoteAcceptedDate = null;
    }

    /// <summary>
    /// Converts an accepted quote into a real (Order-type) sales order, copying the
    /// header and lines. Returns the new order so the caller can persist both.
    /// </summary>
    public SalesOrder ConvertToOrder(string newOrderNumber)
    {
        if (!IsQuote)
            throw new InvalidOperationException("Only quotes can be converted to orders.");
        if (QuoteStatus != QuoteStatus.Accepted)
            throw new InvalidOperationException("Only an accepted quote can be converted to an order.");

        var order = new SalesOrder(
            newOrderNumber,
            CompanyId,
            CustomerId,
            OrderDate,
            ShipToAddress,
            BillToAddress,
            PaymentTermId,
            SalesRepId,
            ShippingMethod,
            CustomerPoNumber);

        foreach (var line in _lines)
        {
            order.AddLine(new SalesOrderLine(
                order.Id,
                line.LineNumber,
                line.ItemId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.UnitOfMeasure,
                line.DiscountPercent,
                line.TaxPercent,
                line.WarehouseId,
                line.ProjectId,
                line.AccountId,
                line.IsDropShip,
                line.DropShipVendorId));
        }

        ConvertedOrderId = order.Id;
        return order;
    }

    /// <summary>
    /// Applies a shipment confirmation against this order: marks the matching lines as
    /// shipped (accumulating across multiple partial shipments) and recomputes the
    /// order status. When a line is shipped for less than its ordered quantity it is
    /// left with a positive <see cref="SalesOrderLine.BackorderedQuantity"/> (backorder);
    /// the order becomes <see cref="SalesOrderStatus.PartiallyShipped"/> until every line
    /// is fully shipped, then <see cref="SalesOrderStatus.Shipped"/>. This is the Order
    /// Management leg of the shipment flow (the Inventory and AR legs are handled by
    /// their own handlers) and is what keeps the backorder state authoritative in OM.
    /// </summary>
    public void MarkShipped(Guid salesOrderLineId, decimal shippedQuantity)
    {
        if (shippedQuantity <= 0)
            throw new ArgumentException("Shipped quantity must be positive.", nameof(shippedQuantity));

        var line = _lines.FirstOrDefault(l => l.Id == salesOrderLineId)
            ?? throw new InvalidOperationException($"Line {salesOrderLineId} not found on this sales order.");

        if (line.ShippedQuantity + shippedQuantity > line.Quantity)
            throw new InvalidOperationException("Cannot ship more than the ordered quantity on a line.");

        line.MarkShipped(shippedQuantity);
        RecomputeStatusFromShipments();
    }

    private void RecomputeStatusFromShipments()
    {
        if (_lines.Count == 0)
            return;

        var fullyShipped = _lines.TrueForAll(l => l.ShippedQuantity >= l.Quantity);
        var anyShipped = _lines.Any(l => l.ShippedQuantity > 0);

        if (Status is SalesOrderStatus.Confirmed or SalesOrderStatus.PartiallyShipped)
        {
            if (fullyShipped)
                Status = SalesOrderStatus.Shipped;
            else if (anyShipped)
                Status = SalesOrderStatus.PartiallyShipped;
        }
    }
}

public enum SalesOrderStatus
{
    Draft = 0,
    Confirmed = 1,
    PartiallyShipped = 2,
    Shipped = 3,
    Closed = 4,
    Cancelled = 5,
}

public enum QuoteStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
}
