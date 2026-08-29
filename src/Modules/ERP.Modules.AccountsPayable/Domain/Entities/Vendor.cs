// <copyright file="Vendor.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class Vendor : AuditableAggregateRoot
{
    private readonly List<VendorBankAccount> _bankAccounts = [];

    protected Vendor() { }

    public Vendor(
        Guid companyId,
        string vendorId,
        string name,
        string? legalName,
        string? taxId,
        Vendor1099Category? form1099Category,
        Guid? defaultPaymentTermId,
        bool isActive,
        bool backupWithholdingFlag = false,
        decimal backupWithholdingRate = 0.24m)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(vendorId))
            throw new ArgumentException("Vendor ID is required.", nameof(vendorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vendor name is required.", nameof(name));

        VendorId = vendorId;
        Name = name;
        CompanyId = companyId;
        LegalName = legalName ?? name;
        TaxId = taxId;
        Form1099Category = form1099Category;
        DefaultPaymentTermId = defaultPaymentTermId;
        IsActive = isActive;
        BackupWithholdingFlag = backupWithholdingFlag;
        BackupWithholdingRate = backupWithholdingRate;
    }

    public Guid CompanyId { get; private set; }

    public string VendorId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? LegalName { get; private set; }

    public string? TaxId { get; private set; }

    public Vendor1099Category? Form1099Category { get; private set; }

    public Guid? DefaultPaymentTermId { get; private set; }

    public bool IsActive { get; private set; }

    public bool BackupWithholdingFlag { get; private set; }

    public decimal BackupWithholdingRate { get; private set; } = 0.24m;

    public bool OnHold { get; private set; }

    public string? InsuranceCarrier { get; private set; }

    public string? InsurancePolicyNumber { get; private set; }

    public DateTimeOffset? InsuranceExpiry { get; private set; }

    public string? DiversityClassification { get; private set; }

    public IReadOnlyList<VendorBankAccount> BankAccounts => _bankAccounts.AsReadOnly();

    public void AddBankAccount(string bankName, string accountNumber, string? routingNumber, bool isDefault)
    {
        if (_bankAccounts.Any(b => b.IsDefault && isDefault))
        {
            foreach (var acct in _bankAccounts)
            {
                acct.ClearDefault();
            }
        }

        var account = new VendorBankAccount(Id, bankName, accountNumber, routingNumber, isDefault);
        _bankAccounts.Add(account);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetOnHold(bool onHold) => OnHold = onHold;

    public void SetCompliance(string? insuranceCarrier, string? insurancePolicyNumber, DateTimeOffset? insuranceExpiry, string? diversityClassification)
    {
        InsuranceCarrier = insuranceCarrier;
        InsurancePolicyNumber = insurancePolicyNumber;
        InsuranceExpiry = insuranceExpiry;
        DiversityClassification = diversityClassification;
    }

    public void Update(Guid companyId, string name, string? legalName, string? taxId, Vendor1099Category? form1099Category, Guid? defaultPaymentTermId, bool backupWithholdingFlag = false, decimal backupWithholdingRate = 0.24m)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vendor name is required.", nameof(name));

        CompanyId = companyId;
        Name = name;
        LegalName = legalName ?? name;
        TaxId = taxId;
        Form1099Category = form1099Category;
        DefaultPaymentTermId = defaultPaymentTermId;
        BackupWithholdingFlag = backupWithholdingFlag;
        BackupWithholdingRate = backupWithholdingRate;
    }
}

public enum Vendor1099Category
{
    None = 0,
    IndependentContractor = 1,
    Rent = 2,
    Royalties = 3,
    NonEmployeeCompensation = 4,
    MedicalAndHealth = 5,
    Attorney = 6,
    Other = 99,
}
