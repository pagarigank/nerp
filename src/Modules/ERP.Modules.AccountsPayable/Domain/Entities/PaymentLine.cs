// <copyright file="PaymentLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class PaymentLine : Entity
{
    protected PaymentLine() { }

    internal PaymentLine(Guid paymentId, Guid voucherId, decimal appliedAmount)
        : base(Guid.NewGuid())
    {
        PaymentId = paymentId;
        VoucherId = voucherId;
        AppliedAmount = appliedAmount;
    }

    public Guid PaymentId { get; private set; }

    public Guid VoucherId { get; private set; }

    public decimal AppliedAmount { get; private set; }
}
