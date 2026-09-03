// <copyright file="TaxpayerProfile.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// Company taxpayer profile for e-Invoice (MyInvois) compliance.
/// </summary>
[Table("taxpayer_profile", Schema = "int")]
public class TaxpayerProfile
{
    public TaxpayerProfile() { }

    public TaxpayerProfile(string registrationNo, string entityName, bool isSubjectToEInvoice = true)
    {
        RegistrationNo = registrationNo;
        EntityName = entityName;
        IsSubjectToEInvoice = isSubjectToEInvoice;
    }

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string RegistrationNo { get; private set; } = string.Empty;

    [Required, MaxLength(200)]
    public string EntityName { get; private set; } = string.Empty;

    [MaxLength(100)]
    public string? TaxIdentificationNumber { get; private set; }

    public bool IsSubjectToEInvoice { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

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
}