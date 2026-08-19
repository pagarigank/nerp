// <copyright file="TaxExemptionCertificate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Tax exemption certificate tracking (Phase 8). Ties a customer's exemption to a
/// certificate number, jurisdiction, and validity window. The <see cref="TaxEngine"/>
/// honours an active certificate as the highest-priority exemption.
/// </summary>
public class TaxExemptionCertificate : AuditableAggregateRoot
{
    protected TaxExemptionCertificate() { }

    public TaxExemptionCertificate(
        Guid companyId,
        string certificateNumber,
        Guid? customerId,
        string jurisdiction,
        DateTime validFrom,
        DateTime validTo,
        string? exemptItemsDescription = null,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            throw new ArgumentException("Certificate number is required.", nameof(certificateNumber));
        if (string.IsNullOrWhiteSpace(jurisdiction))
            throw new ArgumentException("Jurisdiction is required.", nameof(jurisdiction));

        CompanyId = companyId;
        CertificateNumber = certificateNumber;
        CustomerId = customerId;
        Jurisdiction = jurisdiction;
        ValidFrom = validFrom;
        ValidTo = validTo;
        ExemptItemsDescription = exemptItemsDescription;
        Notes = notes;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string CertificateNumber { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public string Jurisdiction { get; private set; } = string.Empty;
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public string? ExemptItemsDescription { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsValidOn(DateTime date) =>
        IsActive && date >= ValidFrom && date <= ValidTo;

    public void Update(
        string certificateNumber,
        string jurisdiction,
        DateTime validFrom,
        DateTime validTo,
        Guid? customerId,
        string? exemptItemsDescription,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            throw new ArgumentException("Certificate number is required.", nameof(certificateNumber));
        if (string.IsNullOrWhiteSpace(jurisdiction))
            throw new ArgumentException("Jurisdiction is required.", nameof(jurisdiction));

        CertificateNumber = certificateNumber;
        Jurisdiction = jurisdiction;
        ValidFrom = validFrom;
        ValidTo = validTo;
        CustomerId = customerId;
        ExemptItemsDescription = exemptItemsDescription;
        Notes = notes;
    }

    public void Revoke() => IsActive = false;
}
