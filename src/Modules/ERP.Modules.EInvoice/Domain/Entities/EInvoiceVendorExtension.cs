// <copyright file="EInvoiceVendorExtension.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.Core.Domain.Common;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// e-Invoice extension for Vendor master data.
/// Contains fields required for Self-Billed Invoice scenarios and AP compliance.
/// </summary>
[Table("einvoice_vendor_extension", Schema = "ap")]
public class EInvoiceVendorExtension : AuditableAggregateRoot
{
    protected EInvoiceVendorExtension() { }

    public EInvoiceVendorExtension(
        Guid vendorId,
        string? taxIdentificationNumber = null,
        string? idType = null,
        string? idValue = null,
        string? sstRegistrationNumber = null,
        string? sstBillingAddress = null,
        string? sstBillingCity = null,
        string? sstBillingState = null,
        string? sstBillingPostalCode = null,
        string? sstBillingCountry = null,
        bool isSubjectToEInvoice = false)
        : base(Guid.NewGuid())
    {
        VendorId = vendorId;
        TaxIdentificationNumber = taxIdentificationNumber;
        IdType = idType;
        IdValue = idValue;
        SstRegistrationNumber = sstRegistrationNumber;
        SstBillingAddress = sstBillingAddress;
        SstBillingCity = sstBillingCity;
        SstBillingState = sstBillingState;
        SstBillingPostalCode = sstBillingPostalCode;
        SstBillingCountry = sstBillingCountry ?? "MY";
        IsSubjectToEInvoice = isSubjectToEInvoice;
    }

    /// <summary>Gets the company scoping identifier.</summary>
    public Guid CompanyId { get; set; }

    public Guid VendorId { get; private set; }

    /// <summary>
    /// Tax Identification Number (TIN) / Tax ID for e-Invoice purposes.
    /// </summary>
    [MaxLength(50)]
    public string? TaxIdentificationNumber { get; private set; }

    /// <summary>
    /// Identity type: BRN, NRIC, Army, Passport, Old BRN, etc.
    /// Used for validating TIN against MyInvois.
    /// </summary>
    [MaxLength(20)]
    public string? IdType { get; private set; }

    /// <summary>
    /// Identity document value (NRIC number, passport number, BRN, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? IdValue { get; private set; }

    /// <summary>
    /// SST Registration Number (Sales/Service Tax registration).
    /// </summary>
    [MaxLength(50)]
    public string? SstRegistrationNumber { get; private set; }

    // Malaysian SST billing address fields for self-billing scenarios

    [MaxLength(200)]
    public string? SstBillingAddress { get; private set; }

    [MaxLength(100)]
    public string? SstBillingCity { get; private set; }

    [MaxLength(100)]
    public string? SstBillingState { get; private set; }

    [MaxLength(20)]
    public string? SstBillingPostalCode { get; private set; }

    [MaxLength(3)]
    public string? SstBillingCountry { get; private set; }

    /// <summary>
    /// Whether this vendor is subject to e-Invoice mandate.
    /// </summary>
    public bool IsSubjectToEInvoice { get; private set; }

    /// <summary>
    /// Indicates if this vendor can issue self-billed invoices.
    /// Required for self-billing scenarios under LHDN guidelines.
    /// </summary>
    public bool CanIssueSelfBilled { get; private set; }

    public void SetTaxIdentificationNumber(string? tin) => TaxIdentificationNumber = tin;

    public void SetIdType(string? idType) => IdType = idType;

    public void SetIdValue(string? idValue) => IdValue = idValue;

    public void SetSstRegistrationNumber(string? registrationNo) => SstRegistrationNumber = registrationNo;

    public void SetSstBillingAddress(
        string? address,
        string? city,
        string? state,
        string? postalCode,
        string? country = "MY")
    {
        SstBillingAddress = address;
        SstBillingCity = city;
        SstBillingState = state;
        SstBillingPostalCode = postalCode;
        SstBillingCountry = country;
    }

    public void SetEInvoiceStatus(bool isSubjectToEInvoice) => IsSubjectToEInvoice = isSubjectToEInvoice;

    public void SetSelfBilledEligible(bool canSelfBill) => CanIssueSelfBilled = canSelfBill;
}