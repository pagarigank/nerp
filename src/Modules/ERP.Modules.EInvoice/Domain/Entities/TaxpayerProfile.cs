// <copyright file="TaxpayerProfile.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// Company taxpayer profile for e-Invoice (MyInvois) compliance.
/// Aligns with ITaxpayerProfileService.TaxpayerProfileDto.
/// </summary>
[Table("taxpayer_profile", Schema = "int")]
public class TaxpayerProfile
{
    public TaxpayerProfile() { }

    public TaxpayerProfile(
        Guid companyId,
        string tin,
        string brn,
        bool isSubjectToEInvoice = true)
    {
        CompanyId = companyId;
        TaxIdentificationNumber = tin;
        BusinessRegistrationNumber = brn;
        IsSubjectToEInvoice = isSubjectToEInvoice;
    }

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Company this taxpayer profile belongs to.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company Tax Identification Number (TIN) issued by LHDN.
    /// </summary>
    [Required, MaxLength(50)]
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Business Registration Number (BRN) from SSM.
    /// </summary>
    [MaxLength(50)]
    public string? BusinessRegistrationNumber { get; set; }

    /// <summary>
    /// Sales Tax Registration Number.
    /// </summary>
    [MaxLength(50)]
    public string? SalesTaxNumber { get; set; }

    /// <summary>
    /// Service Tax Registration Number.
    /// </summary>
    [MaxLength(50)]
    public string? ServiceTaxNumber { get; set; }

    /// <summary>
    /// MSIC (Malaysian Standard Industrial Code) classification.
    /// </summary>
    [MaxLength(20)]
    public string? MsicCode { get; set; }

    /// <summary>
    /// Registered address for e-Invoice purposes.
    /// </summary>
    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostCode { get; set; }

    /// <summary>
    /// Whether this company is subject to e-Invoice mandate.
    /// </summary>
    public bool IsSubjectToEInvoice { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public void UpdateEInvoiceStatus(bool isSubjectToEInvoice)
    {
        IsSubjectToEInvoice = isSubjectToEInvoice;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateTIN(string tin)
    {
        TaxIdentificationNumber = tin;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProfile(
        string tin,
        string? brn,
        string? salesTax,
        string? serviceTax,
        string? msic,
        string? address,
        string? city,
        string? state,
        string? postCode)
    {
        TaxIdentificationNumber = tin;
        BusinessRegistrationNumber = brn;
        SalesTaxNumber = salesTax;
        ServiceTaxNumber = serviceTax;
        MsicCode = msic;
        Address = address;
        City = city;
        State = state;
        PostCode = postCode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}