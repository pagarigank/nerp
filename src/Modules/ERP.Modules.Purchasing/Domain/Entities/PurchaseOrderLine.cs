// <copyright file="PurchaseOrderLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class PurchaseOrderLine : AuditableEntity
{
    protected PurchaseOrderLine() { }

    public PurchaseOrderLine(
        Guid purchaseOrderId,
        int lineNumber,
        string? itemId,
        string description,
        decimal quantity,
        string unitOfMeasure,
        decimal unitPrice,
        DateTime? needByDate,
        Guid? accountId,
        Guid? projectId,
        Guid? taskId,
        Guid? requisitionLineId = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
            throw new ArgumentException("Unit of measure is required.", nameof(unitOfMeasure));

        PurchaseOrderId = purchaseOrderId;
        LineNumber = lineNumber;
        ItemId = itemId;
        Description = description;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        NeedByDate = needByDate;
        AccountId = accountId;
        ProjectId = projectId;
        TaskId = taskId;
        RequisitionLineId = requisitionLineId;
    }

    public Guid PurchaseOrderId { get; private set; }

    public int LineNumber { get; private set; }

    public string? ItemId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public DateTime? NeedByDate { get; private set; }

    public Guid? AccountId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? TaskId { get; private set; }

    public Guid? RequisitionLineId { get; private set; }

    public string? TaxCode { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal TaxAmount => Math.Round(Quantity * UnitPrice * TaxRate, 2, MidpointRounding.AwayFromZero);

    public decimal QuantityReceived { get; private set; }

    public decimal QuantityInvoiced { get; private set; }

    public bool IsCancelled { get; private set; }

    public string? CancellationReason { get; private set; }

    public decimal GetExtendedPriceWithTax() => Math.Round(GetExtendedPrice() + TaxAmount, 2, MidpointRounding.AwayFromZero);

    public void SetTax(string? taxCode, decimal taxRate)
    {
        if (taxRate < 0)
            throw new ArgumentException("Tax rate cannot be negative.", nameof(taxRate));
        TaxCode = taxCode;
        TaxRate = taxRate;
    }

    public decimal GetExtendedPrice() => Quantity * UnitPrice;

    public decimal GetRemainingQuantity() => Quantity - QuantityReceived;

    public decimal GetRemainingAmount() => GetRemainingQuantity() * UnitPrice;

    public bool IsFullyReceived() => QuantityReceived >= Quantity;

    public bool IsFullyInvoiced() => QuantityInvoiced >= Quantity;

    public void UpdateQuantityReceived(decimal receivedQuantity, decimal overReceiptTolerance = 0.05m)
    {
        if (IsCancelled)
            throw new InvalidOperationException("Cannot receive against cancelled line.");

        if (receivedQuantity < 0)
            throw new ArgumentException("Received quantity cannot be negative.", nameof(receivedQuantity));

        var newTotal = QuantityReceived + receivedQuantity;
        var maxAllowed = Quantity * (1 + overReceiptTolerance);

        if (newTotal > maxAllowed)
            throw new InvalidOperationException(
                $"Receiving {receivedQuantity} would exceed over-receipt tolerance. " +
                $"Ordered: {Quantity}, Already received: {QuantityReceived}, Max allowed: {maxAllowed}");

        QuantityReceived = newTotal;
    }

    public void UpdateQuantityInvoiced(decimal invoicedQuantity)
    {
        if (IsCancelled)
            throw new InvalidOperationException("Cannot invoice against cancelled line.");

        if (invoicedQuantity < 0)
            throw new ArgumentException("Invoiced quantity cannot be negative.", nameof(invoicedQuantity));

        var newTotal = QuantityInvoiced + invoicedQuantity;

        if (newTotal > Quantity)
            throw new InvalidOperationException(
                $"Invoicing {invoicedQuantity} would exceed ordered quantity. " +
                $"Ordered: {Quantity}, Already invoiced: {QuantityInvoiced}");

        QuantityInvoiced = newTotal;
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));

        if (QuantityReceived > 0)
            throw new InvalidOperationException("Cannot cancel line that has been partially received.");

        IsCancelled = true;
        CancellationReason = reason;
    }

    public void UpdatePrice(decimal newUnitPrice)
    {
        if (newUnitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(newUnitPrice));

        UnitPrice = newUnitPrice;
    }

    public void UpdateQuantity(decimal newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

        if (newQuantity < QuantityReceived)
            throw new InvalidOperationException(
                $"Cannot reduce quantity below already received amount. Received: {QuantityReceived}");

        Quantity = newQuantity;
    }
}
