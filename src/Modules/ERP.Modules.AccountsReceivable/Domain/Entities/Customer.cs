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
        string? currencyCode,
        Guid? salesRepId = null,
        Guid? taxCodeId = null,
        Guid? taxExemptionCertificateId = null)
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
        SalesRepId = salesRepId;
        TaxCodeId = taxCodeId;
        TaxExemptionCertificateId = taxExemptionCertificateId;
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

    /// <summary>Default sales rep (salesperson) assigned to this customer.</summary>
    public Guid? SalesRepId { get; private set; }

    /// <summary>Default tax code applied to this customer's sales orders.</summary>
    public Guid? TaxCodeId { get; private set; }

    /// <summary>Default tax-exemption certificate applied to this customer's sales orders.</summary>
    public Guid? TaxExemptionCertificateId { get; private set; }

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
        string? currencyCode,
        Guid? salesRepId,
        Guid? taxCodeId,
        Guid? taxExemptionCertificateId)
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
        SalesRepId = salesRepId;
        TaxCodeId = taxCodeId;
        TaxExemptionCertificateId = taxExemptionCertificateId;
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
