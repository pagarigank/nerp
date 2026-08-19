// <copyright file="CashReceipt.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class CashReceipt : AuditableAggregateRoot
{
    private readonly List<CashReceiptApplication> _applications = [];

    protected CashReceipt() { }

    public CashReceipt(
        Guid companyId,
        Guid customerId,
        string receiptReference,
        decimal totalAmount,
        DateTimeOffset receiptDate,
        string paymentMethod,
        string? currencyCode,
        string? referenceNumber)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(receiptReference))
            throw new ArgumentException("Receipt reference is required.", nameof(receiptReference));
        if (totalAmount <= 0)
            throw new ArgumentException("Total amount must be positive.", nameof(totalAmount));

        CompanyId = companyId;
        CustomerId = customerId;
        ReceiptReference = receiptReference;
        TotalAmount = totalAmount;
        ReceiptDate = receiptDate;
        PaymentMethod = paymentMethod;
        CurrencyCode = currencyCode ?? "USD";
        ReferenceNumber = referenceNumber;
        Status = CashReceiptStatus.Unapplied;
    }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string ReceiptReference { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public DateTimeOffset ReceiptDate { get; private set; }

    public string PaymentMethod { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = "USD";

    public string? ReferenceNumber { get; private set; }

    public CashReceiptStatus Status { get; private set; }

    public decimal AppliedAmount => _applications.Sum(a => a.AppliedAmount);

    public decimal UnappliedAmount => TotalAmount - AppliedAmount;

    public IReadOnlyList<CashReceiptApplication> Applications => _applications.AsReadOnly();

    public CashReceiptApplication ApplyToInvoice(Invoice invoice, decimal amount)
    {
        if (Status != CashReceiptStatus.Unapplied && Status != CashReceiptStatus.PartiallyApplied)
            throw new InvalidOperationException("Cannot apply a receipt that is not in Unapplied or Partially Applied status.");

        if (amount <= 0)
            throw new ArgumentException("Application amount must be positive.", nameof(amount));

        if (amount > UnappliedAmount)
            throw new ArgumentException("Application amount exceeds unapplied receipt amount.", nameof(amount));

        if (amount > invoice.BalanceDue)
            throw new ArgumentException("Application amount exceeds invoice balance due.", nameof(amount));

        var application = new CashReceiptApplication(Id, invoice.Id, amount);
        _applications.Add(application);
        invoice.ApplyPayment(amount);

        if (UnappliedAmount <= 0)
            Status = CashReceiptStatus.FullyApplied;
        else
            Status = CashReceiptStatus.PartiallyApplied;

        return application;
    }

    public void UnapplyInvoice(Invoice invoice, CashReceiptApplication application)
    {
        if (!_applications.Contains(application))
            throw new InvalidOperationException("Application not found on this receipt.");

        _applications.Remove(application);
        invoice.ApplyPayment(-application.AppliedAmount);

        if (_applications.Count == 0)
            Status = CashReceiptStatus.Unapplied;
    }

    public void MarkAsRefunded()
    {
        Status = CashReceiptStatus.Refunded;
    }
}

public enum CashReceiptStatus
{
    Unapplied = 0,
    PartiallyApplied = 1,
    FullyApplied = 2,
    Refunded = 3,
}

public record CashReceiptAppliedEvent : DomainEvent
{
    public CashReceiptAppliedEvent(Guid receiptId, Guid invoiceId, decimal amount, Guid companyId)
    {
        ReceiptId = receiptId;
        InvoiceId = invoiceId;
        Amount = amount;
        CompanyId = companyId;
    }

    public Guid ReceiptId { get; }
    public Guid InvoiceId { get; }
    public decimal Amount { get; }
    public Guid CompanyId { get; }

    public override string EventType => "CashReceiptApplied";
}
