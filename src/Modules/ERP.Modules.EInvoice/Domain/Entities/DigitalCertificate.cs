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
        CertificateBytes = Array.Empty<byte>();
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the company this certificate belongs to (multi-company isolation).
    /// </summary>
    public Guid CompanyId { get; set; }

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

    /// <summary>Gets or sets the certificate PFX/PKCS12 bytes (for sandbox/dev). Production should use vault.</summary>
    #pragma warning disable CA1819 // Properties should not return arrays
    public byte[] CertificateBytes { get; set; }
    #pragma warning restore CA1819

    /// <summary>Gets or sets the password for the PFX file (encrypted in production).</summary>
    [MaxLength(200)]
    public string? CertificatePassword { get; set; }

    public bool IsActive { get; set; }

    public bool IsSandbox { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedByUserId { get; set; }

    [MaxLength(100)]
    public string? UpdatedByUserId { get; set; }
}