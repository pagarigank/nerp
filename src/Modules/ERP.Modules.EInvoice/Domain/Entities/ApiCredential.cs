// <copyright file="ApiCredential.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// API credential for MyInvois authentication.
/// Stores Client ID/Secret per environment (sandbox/production).
/// The secret is write-once and never returned in plaintext after creation.
/// </summary>
[Table("api_credential", Schema = "int")]
public class ApiCredential
{
    public ApiCredential()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the company this credential belongs to (multi-company isolation).
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Gets or sets the environment (sandbox or production).
    /// </summary>
    [Required, MaxLength(20)]
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Gets or sets the OAuth Client ID issued by MyInvois.
    /// </summary>
    [Required, MaxLength(200)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted Client Secret.
    /// Never returned in API responses after creation.
    /// </summary>
    [Required, MaxLength(500)]
    public string EncryptedClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this credential is active.
    /// Only one active credential per company per environment.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date when this credential expires (if applicable).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the secret was last rotated.
    /// </summary>
    public DateTime? LastRotatedAt { get; set; }

    /// <summary>
    /// Gets or sets the created-at timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated-at timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a description (e.g., "Production Client ID from MyInvois Portal").
    /// </summary>
    [MaxLength(250)]
    public string? Description { get; set; }

    /// <summary>
    /// Updates the secret (rotate).
    /// </summary>
    public void RotateSecret(string encryptedNewSecret)
    {
        EncryptedClientSecret = encryptedNewSecret;
        LastRotatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this credential.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates this credential and deactivates others for the same company/environment.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
