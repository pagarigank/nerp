// <copyright file="CreditDebitMemo.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class CreditDebitMemo : Entity
{
    private readonly List<InvoiceLine> _lines = [];

    protected CreditDebitMemo() { }

    internal CreditDebitMemo(
        Guid invoiceBatchId,
        Guid customerId,
        string referenceNumber,
        DateTimeOffset memoDate,
        Guid? invoiceId,
        string? description)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new ArgumentException("Reference number is required.", nameof(referenceNumber));

        InvoiceBatchId = invoiceBatchId;
        CustomerId = customerId;
        ReferenceNumber = referenceNumber;
        MemoDate = memoDate;
        AppliedToInvoiceId = invoiceId;
        Description = description ?? string.Empty;
        Status = CreditDebitMemoStatus.Open;
    }

    public Guid InvoiceBatchId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string ReferenceNumber { get; private set; } = string.Empty;

    public DateTimeOffset MemoDate { get; private set; }

    public Guid? AppliedToInvoiceId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public CreditDebitMemoType MemoType { get; private set; }

    public CreditDebitMemoStatus Status { get; private set; }

    public decimal TotalAmount => _lines.Sum(l => l.TotalAmount);

    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();

    public decimal Debit => MemoType == CreditDebitMemoType.DebitMemo ? TotalAmount : 0;

    public decimal Credit => MemoType == CreditDebitMemoType.CreditMemo ? TotalAmount : 0;

    public void SetMemoType(CreditDebitMemoType type) => MemoType = type;

    public InvoiceLine AddLine(Guid accountId, string description, decimal quantity, decimal unitPrice, decimal taxAmount, decimal? discountAmount)
    {
        var line = new InvoiceLine(null, Id, accountId, description, quantity, unitPrice, taxAmount, discountAmount ?? 0);
        _lines.Add(line);
        return line;
    }

    public void Apply()
    {
        if (Status != CreditDebitMemoStatus.Open)
            throw new InvalidOperationException("Memo is not in Open status.");
        Status = CreditDebitMemoStatus.Applied;
    }

    public void Void()
    {
        if (Status == CreditDebitMemoStatus.Voided)
            throw new InvalidOperationException("Memo is already voided.");
        Status = CreditDebitMemoStatus.Voided;
    }
}

public enum CreditDebitMemoType
{
    CreditMemo = 0,
    DebitMemo = 1,
}

public enum CreditDebitMemoStatus
{
    Open = 0,
    Applied = 1,
    Voided = 2,
}
