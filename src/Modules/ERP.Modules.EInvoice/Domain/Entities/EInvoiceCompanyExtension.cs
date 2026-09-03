// <copyright file="EInvoiceCompanyExtension.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.Core.Domain.Common;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// e-Invoice extension for Company master data.
/// Contains fields for e-Invoice registration and compliance.
/// </summary>
[Table("einvoice_company_extension", Schema = "platform")]
public class EInvoiceCompanyExtension : AuditableAggregateRoot
{
    protected EInvoiceCompanyExtension() { }

    public EInvoiceCompanyExtension(
        Guid companyId,
        string? taxIdentificationNumber = null,
        string? businessRegistrationNumber = null,
        string? sstRegistrationNumber = null,
        string? salesTaxNumber = null,
        string? serviceTaxNumber = null,
        string? msicCode = null,
        DateTime? registrationDate = null,
        bool isSubjectToEInvoice = false)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        TaxIdentificationNumber = taxIdentificationNumber;
        BusinessRegistrationNumber = businessRegistrationNumber;
        SstRegistrationNumber = sstRegistrationNumber;
        SalesTaxNumber = salesTaxNumber;
        ServiceTaxNumber = serviceTaxNumber;
        MsicCode = msicCode;
        RegistrationDate = registrationDate;
        IsSubjectToEInvoice = isSubjectToEInvoice;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Company Tax Identification Number (TIN) / BRN.
    /// </summary>
    [MaxLength(50)]
    public string? TaxIdentificationNumber { get; private set; }

    /// <summary>
    /// Business Registration Number (BRN).
    /// </summary>
    [MaxLength(50)]
    public string? BusinessRegistrationNumber { get; private set; }

    /// <summary>
    /// Sales Tax Registration Number.
    /// </summary>
    [MaxLength(50)]
    public string? SalesTaxNumber { get; private set; }

    /// <summary>
    /// Service Tax Registration Number.
    /// </summary>
    [MaxLength(50)]
    public string? ServiceTaxNumber { get; private set; }

    /// <summary>
    /// SST Registration Number (combined Sales Tax / Service Tax registration).
    /// </summary>
    [MaxLength(50)]
    public string? SstRegistrationNumber { get; private set; }

    /// <summary>
    /// MSIC (Malaysian Standard Industrial Code) classification.
    /// </summary>
    [MaxLength(10)]
    public string? MsicCode { get; private set; }

    /// <summary>
    /// Registration date with LHDN or SST authority.
    /// </summary>
    public DateTime? RegistrationDate { get; private set; }

    /// <summary>
    /// Whether this company is subject to e-Invoice mandate.
    /// Determined by RM3,000,000 turnover threshold or business type.
    /// </summary>
    public bool IsSubjectToEInvoice { get; private set; }

    public void SetTaxIdentificationNumber(string? tin) => TaxIdentificationNumber = tin;

    public void SetBusinessRegistrationNumber(string? brn) => BusinessRegistrationNumber = brn;

    public void SetSstNumbers(string? salesTaxNo, string? serviceTaxNo)
    {
        SalesTaxNumber = salesTaxNo;
        ServiceTaxNumber = serviceTaxNo;
    }

    public void SetRegistrationDate(DateTime? date) => RegistrationDate = date;

    public void SetMsicCode(string? msic) => MsicCode = msic;

    public void SetEInvoiceStatus(bool isSubjectToEInvoice) => IsSubjectToEInvoice = isSubjectToEInvoice;

    public bool IsEInvoiceMandatedByThreshold(decimal annualTurnover, decimal threshold = 3000000m)
    {
        return IsSubjectToEInvoice || annualTurnover >= threshold;
    }
}