// <copyright file="Payment.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class Payment : AuditableAggregateRoot
{
    private readonly List<PaymentLine> _lines = [];

    protected Payment() { }

    public Payment(
        Guid companyId,
        Guid vendorId,
        string paymentReference,
        DateTimeOffset paymentDate,
        PaymentMethod paymentMethod,
        string? currencyCode,
        Guid? bankAccountId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(paymentReference))
            throw new ArgumentException("Payment reference is required.", nameof(paymentReference));

        CompanyId = companyId;
        VendorId = vendorId;
        PaymentReference = paymentReference;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        CurrencyCode = currencyCode ?? "USD";
        BankAccountId = bankAccountId;
        Status = PaymentStatus.Selected;
    }

    public Guid CompanyId { get; private set; }

    public Guid VendorId { get; private set; }

    public string PaymentReference { get; private set; } = string.Empty;

    public DateTimeOffset PaymentDate { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public string CurrencyCode { get; private set; } = "USD";

    public Guid? BankAccountId { get; private set; }

    public PaymentStatus Status { get; private set; }

    public decimal TotalAmount => _lines.Sum(l => l.AppliedAmount);

    public IReadOnlyList<PaymentLine> Lines => _lines.AsReadOnly();

    public void AddVoucher(Voucher voucher, decimal appliedAmount)
    {
        if (Status != PaymentStatus.Selected)
            throw new InvalidOperationException("Cannot modify a payment that is not in Selected status.");

        if (!voucher.SelectedForPayment)
            throw new InvalidOperationException("Voucher must be selected for payment first.");

        if (appliedAmount <= 0)
            throw new ArgumentException("Applied amount must be positive.", nameof(appliedAmount));

        if (appliedAmount > voucher.TotalAmount - _lines.Where(l => l.VoucherId == voucher.Id).Sum(l => l.AppliedAmount))
            throw new ArgumentException("Applied amount exceeds remaining voucher balance.");

        var line = new PaymentLine(Id, voucher.Id, appliedAmount);
        _lines.Add(line);
        voucher.MarkSelectedForPayment();
    }

    public void Issue()
    {
        if (Status != PaymentStatus.Selected)
            throw new InvalidOperationException("Only a Selected payment can be issued.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot issue a payment with no vouchers.");

        Status = PaymentStatus.Issued;
        AddDomainEvent(new PaymentIssuedEvent(Id, PaymentReference, CompanyId, VendorId, TotalAmount, PaymentDate));
    }

    public void Clear()
    {
        if (Status != PaymentStatus.Issued)
            throw new InvalidOperationException("Only an Issued payment can be cleared.");

        Status = PaymentStatus.Cleared;
    }

    public void Void(string reason)
    {
        if (Status == PaymentStatus.Voided)
            throw new InvalidOperationException("Payment is already voided.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Void reason is required.", nameof(reason));

        Status = PaymentStatus.Voided;
    }
}

public enum PaymentStatus
{
    Selected = 0,
    Issued = 1,
    Cleared = 2,
    Voided = 3,
}

public enum PaymentMethod
{
    Check = 0,
    ACH = 1,
    WireTransfer = 2,
    CreditCard = 3,
    Cash = 4,
}

public record PaymentIssuedEvent : DomainEvent
{
    public PaymentIssuedEvent(
        Guid paymentId,
        string paymentReference,
        Guid companyId,
        Guid vendorId,
        decimal totalAmount,
        DateTimeOffset paymentDate)
    {
        PaymentId = paymentId;
        PaymentReference = paymentReference;
        CompanyId = companyId;
        VendorId = vendorId;
        TotalAmount = totalAmount;
        PaymentDate = paymentDate;
    }

    public Guid PaymentId { get; }
    public string PaymentReference { get; }
    public Guid CompanyId { get; }
    public Guid VendorId { get; }
    public decimal TotalAmount { get; }
    public DateTimeOffset PaymentDate { get; }

    public override string EventType => "PaymentIssued";
}
