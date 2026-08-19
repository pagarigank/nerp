// <copyright file="Voucher.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class Voucher : Entity
{
    private readonly List<VoucherDistribution> _distributions = [];

    protected Voucher() { }

    internal Voucher(
        Guid voucherBatchId,
        Guid vendorId,
        VoucherType voucherType,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset dueDate,
        decimal totalAmount,
        decimal discountAmount,
        string? description,
        Guid? paymentTermId,
        Guid? purchaseOrderId = null,
        Guid? receiptLineId = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));
        if (totalAmount <= 0)
            throw new ArgumentException("Total amount must be positive.", nameof(totalAmount));

        VoucherBatchId = voucherBatchId;
        VendorId = vendorId;
        VoucherType = voucherType;
        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        TotalAmount = totalAmount;
        DiscountAmount = discountAmount;
        Description = description ?? string.Empty;
        PaymentTermId = paymentTermId;
        PurchaseOrderId = purchaseOrderId;
        ReceiptLineId = receiptLineId;
    }

    public Guid VoucherBatchId { get; private set; }

    public VoucherBatch? VoucherBatch { get; internal set; }

    public Guid VendorId { get; private set; }

    public VoucherType VoucherType { get; private set; }

    public string InvoiceNumber { get; private set; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; private set; }

    public DateTimeOffset DueDate { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? PaymentTermId { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }

    public Guid? ReceiptLineId { get; private set; }

    public decimal Form1099Amount { get; private set; }

    public decimal BackupWithholdingAmount { get; private set; }

    public bool SelectedForPayment { get; private set; }

    public bool Is1099Reportable => Form1099Amount > 0;

    public IReadOnlyList<VoucherDistribution> Distributions => _distributions.AsReadOnly();

    public void SetThreeWayMatchReferences(Guid? purchaseOrderId, Guid? receiptLineId)
    {
        PurchaseOrderId = purchaseOrderId;
        ReceiptLineId = receiptLineId;
    }

    public void Set1099Amount(decimal form1099Amount)
    {
        if (form1099Amount < 0)
            throw new ArgumentException("1099 amount cannot be negative.", nameof(form1099Amount));
        if (form1099Amount > TotalAmount)
            throw new ArgumentException("1099 amount cannot exceed total amount.", nameof(form1099Amount));

        Form1099Amount = form1099Amount;
    }

    public void SetBackupWithholding(decimal backupWithholdingAmount)
    {
        if (backupWithholdingAmount < 0)
            throw new ArgumentException("Backup withholding amount cannot be negative.", nameof(backupWithholdingAmount));

        BackupWithholdingAmount = backupWithholdingAmount;
    }

    public VoucherDistribution AddDistribution(Guid accountId, decimal? debit, decimal? credit, Guid? projectId, Guid? taskId)
    {
        if ((debit.HasValue && credit.HasValue) || (!debit.HasValue && !credit.HasValue))
            throw new ArgumentException("Each distribution must have either a debit OR a credit amount, not both.");

        if (debit.HasValue && debit <= 0)
            throw new ArgumentException("Debit amount must be positive.", nameof(debit));

        if (credit.HasValue && credit <= 0)
            throw new ArgumentException("Credit amount must be positive.", nameof(credit));

        var distribution = new VoucherDistribution(Id, accountId, debit ?? 0, credit ?? 0, projectId, taskId);
        _distributions.Add(distribution);
        return distribution;
    }

    public bool IsBalanced()
    {
        var totalDebits = _distributions.Sum(d => d.Debit);
        var totalCredits = _distributions.Sum(d => d.Credit);
        return Math.Round(totalDebits, 2) == Math.Round(totalCredits, 2);
    }

    public void MarkSelectedForPayment() => SelectedForPayment = true;

    public void ClearPaymentSelection() => SelectedForPayment = false;
}

public enum VoucherType
{
    Invoice = 0,
    CreditMemo = 1,
    DebitMemo = 2,
    Prepayment = 3,
}
