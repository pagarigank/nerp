// <copyright file="Company.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class Company : AuditableAggregateRoot
{
    protected Company() { }

    public Company(
        string name,
        string legalName,
        string baseCurrency,
        string? taxId = null,
        string? address = null,
        Guid? parentCompanyId = null) : base(Guid.NewGuid())
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        LegalName = legalName ?? throw new ArgumentNullException(nameof(legalName));
        BaseCurrency = baseCurrency ?? throw new ArgumentNullException(nameof(baseCurrency));
        TaxId = taxId;
        Address = address;
        ParentCompanyId = parentCompanyId;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string LegalName { get; private set; } = string.Empty;

    public string BaseCurrency { get; private set; } = "USD";

    public string? TaxId { get; private set; }

    public string? Address { get; private set; }

    public Guid? ParentCompanyId { get; private set; }

    public bool IsActive { get; private set; }

    // Navigation property for hierarchy
    public virtual ICollection<Company> ChildCompanies { get; private set; } = new List<Company>();

    public virtual Company? ParentCompany { get; }

    public void Update(string name, string legalName, string baseCurrency, string? taxId, string? address)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        LegalName = legalName ?? throw new ArgumentNullException(nameof(legalName));
        BaseCurrency = baseCurrency ?? throw new ArgumentNullException(nameof(baseCurrency));
        TaxId = taxId;
        Address = address;
    }

    public void SetParentCompany(Guid? parentCompanyId)
    {
        ParentCompanyId = parentCompanyId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
