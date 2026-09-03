// <copyright file="DigitalCertificate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// Digital certificate for XAdES signing.
/// Stores metadata - private key is stored in secure vault.
/// </summary>
[Table("digital_certificate", Schema = "int")]
public class DigitalCertificate
{
    public DigitalCertificate()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        IsSandbox = false;
        CreatedAt = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string CertificateName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Issuer { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    public DateTime NotBefore { get; set; }

    [Required]
    public DateTime NotAfter { get; set; }

    [Required, MaxLength(20)]
    public string Thumbprint { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? VaultObjectId { get; set; }

    public bool IsActive { get; set; }

    public bool IsSandbox { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedByUserId { get; set; }

    [MaxLength(100)]
    public string? UpdatedByUserId { get; set; }
}