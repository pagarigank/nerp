// <copyright file="FinanceCharge.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class FinanceCharge : AuditableAggregateRoot
{
    protected FinanceCharge() { }

    public FinanceCharge(
        Guid companyId,
        Guid customerId,
        string chargeNumber,
        DateTimeOffset chargeDate,
        decimal chargeAmount,
        decimal annualRate,
        string description)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(chargeNumber))
            throw new ArgumentException("Charge number is required.", nameof(chargeNumber));
        if (chargeAmount <= 0)
            throw new ArgumentException("Charge amount must be positive.", nameof(chargeAmount));

        CompanyId = companyId;
        CustomerId = customerId;
        ChargeNumber = chargeNumber;
        ChargeDate = chargeDate;
        ChargeAmount = chargeAmount;
        AnnualRate = annualRate;
        Description = description ?? string.Empty;
        Status = FinanceChargeStatus.Open;
    }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string ChargeNumber { get; private set; } = string.Empty;

    public DateTimeOffset ChargeDate { get; private set; }

    public decimal ChargeAmount { get; private set; }

    public decimal AnnualRate { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public FinanceChargeStatus Status { get; private set; }

    public void Void()
    {
        if (Status == FinanceChargeStatus.Voided)
            throw new InvalidOperationException("Finance charge is already voided.");
        Status = FinanceChargeStatus.Voided;
    }
}

public enum FinanceChargeStatus
{
    Open = 0,
    Voided = 1,
}
