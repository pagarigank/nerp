// <copyright file="CashReceiptApplication.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class CashReceiptApplication : Entity
{
    protected CashReceiptApplication() { }

    internal CashReceiptApplication(Guid cashReceiptId, Guid invoiceId, decimal appliedAmount)
        : base(Guid.NewGuid())
    {
        CashReceiptId = cashReceiptId;
        InvoiceId = invoiceId;
        AppliedAmount = appliedAmount;
    }

    public Guid CashReceiptId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public decimal AppliedAmount { get; private set; }
}
