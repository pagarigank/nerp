// <copyright file="PaymentTerm.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class PaymentTerm : AuditableAggregateRoot
{
    protected PaymentTerm() { }

    public PaymentTerm(string name, int dueDays, int discountDays, decimal discountPercent)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Payment term name is required.", nameof(name));

        Name = name;
        DueDays = dueDays;
        DiscountDays = discountDays;
        DiscountPercent = discountPercent;
    }

    public string Name { get; private set; } = string.Empty;

    public int DueDays { get; private set; }

    public int DiscountDays { get; private set; }

    public decimal DiscountPercent { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CalculateDueDate(DateTimeOffset invoiceDate)
        => invoiceDate.AddDays(DueDays);

    public DateTimeOffset CalculateDiscountDate(DateTimeOffset invoiceDate)
        => invoiceDate.AddDays(DiscountDays);

    public decimal CalculateDiscount(decimal amount)
        => Math.Round(amount * DiscountPercent / 100, 2);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void Update(string name, int dueDays, int discountDays, decimal discountPercent)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Payment term name is required.", nameof(name));

        Name = name;
        DueDays = dueDays;
        DiscountDays = discountDays;
        DiscountPercent = discountPercent;
    }
}
