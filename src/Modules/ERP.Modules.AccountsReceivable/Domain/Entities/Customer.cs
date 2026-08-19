// <copyright file="Customer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class Customer : AuditableAggregateRoot
{
    protected Customer() { }

    public Customer(
        string customerId,
        string name,
        string? legalName,
        string? taxId,
        decimal creditLimit,
        int creditHoldDays,
        Guid? defaultPaymentTermId,
        bool taxExempt,
        string? taxExemptCertificate,
        string? currencyCode)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name is required.", nameof(name));

        CustomerId = customerId;
        Name = name;
        LegalName = legalName ?? name;
        TaxId = taxId;
        CreditLimit = creditLimit;
        CreditHoldDays = creditHoldDays;
        DefaultPaymentTermId = defaultPaymentTermId;
        TaxExempt = taxExempt;
        TaxExemptCertificate = taxExemptCertificate;
        CurrencyCode = currencyCode ?? "USD";
        IsActive = true;
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? LegalName { get; private set; }

    public string? TaxId { get; private set; }

    public decimal CreditLimit { get; private set; }

    public int CreditHoldDays { get; private set; }

    public Guid? DefaultPaymentTermId { get; private set; }

    public bool TaxExempt { get; private set; }

    public string? TaxExemptCertificate { get; private set; }

    public string CurrencyCode { get; private set; } = "USD";

    public bool IsActive { get; private set; }

    public decimal CurrentBalance { get; internal set; }

    public void Update(
        string name,
        string? legalName,
        string? taxId,
        decimal creditLimit,
        int creditHoldDays,
        Guid? defaultPaymentTermId,
        bool taxExempt,
        string? taxExemptCertificate,
        string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name is required.", nameof(name));

        Name = name;
        LegalName = legalName ?? name;
        TaxId = taxId;
        CreditLimit = creditLimit;
        CreditHoldDays = creditHoldDays;
        DefaultPaymentTermId = defaultPaymentTermId;
        TaxExempt = taxExempt;
        TaxExemptCertificate = taxExemptCertificate;
        CurrencyCode = currencyCode ?? "USD";
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetCreditLimit(decimal limit)
    {
        if (limit < 0)
            throw new ArgumentException("Credit limit cannot be negative.", nameof(limit));
        CreditLimit = limit;
    }
}
