// <copyright file="InvoiceLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class InvoiceLine : Entity
{
    protected InvoiceLine() { }

    internal InvoiceLine(
        Guid? invoiceId,
        Guid? creditDebitMemoId,
        Guid accountId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxAmount,
        decimal discountAmount)
        : base(Guid.NewGuid())
    {
        InvoiceId = invoiceId;
        CreditDebitMemoId = creditDebitMemoId;
        AccountId = accountId;
        Description = description ?? string.Empty;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxAmount = taxAmount;
        DiscountAmount = discountAmount;
    }

    public Guid? InvoiceId { get; private set; }

    public Guid? CreditDebitMemoId { get; private set; }

    public Guid AccountId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal TotalAmount => (Quantity * UnitPrice) + TaxAmount - DiscountAmount;

    public decimal PaidAmount { get; internal set; }

    public decimal Debit => TotalAmount;

#pragma warning disable S2325, CA1822
    public decimal Credit => 0;
#pragma warning restore S2325, CA1822
}
