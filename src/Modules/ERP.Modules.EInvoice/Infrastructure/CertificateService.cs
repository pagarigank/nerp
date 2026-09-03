// <copyright file="CertificateService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Security;
using System.Security.Cryptography.X509Certificates;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Service for managing digital certificates used for XAdES signing of e-Invoice documents.
/// Stores certificate metadata in the database; the private key is held in a secure vault.
/// </summary>
public class CertificateService : ICertificateService
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(EInvoiceDbContext dbContext, ILogger<CertificateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<CertificateDto>> GetAllByCompanyIdAsync(Guid companyId)
    {
        var certs = await _dbContext.DigitalCertificates
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return certs.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<CertificateDto?> GetByIdAsync(Guid id)
    {
        var cert = await _dbContext.DigitalCertificates.FindAsync(id);
        return cert == null ? null : MapToDto(cert);
    }

    /// <inheritdoc/>
    public async Task<CertificateDto> UploadAsync(Guid companyId, byte[] fileContent, string fileName, string name, bool isActive)
    {
        X509Certificate2 certificate;

        // Try loading as PKCS#12 (PFX) — empty password first, then null
        try
        {
            certificate = new X509Certificate2(fileContent, string.Empty, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch
        {
            try
            {
                certificate = new X509Certificate2(
                    fileContent,
                    (SecureString?)null,
                    X509KeyStorageFlags.EphemeralKeySet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse certificate file {FileName}", fileName);
                throw new InvalidOperationException(
                    $"Unable to parse the certificate file '{fileName}'. " +
                    "Ensure it is a valid PKCS#12 (PFX) or PEM certificate. " +
                    $"Error: {ex.Message}");
            }
        }

        // Validate it has a private key (required for signing)
        if (!certificate.HasPrivateKey)
        {
            _logger.LogWarning("Certificate {FileName} does not contain a private key", fileName);
            throw new InvalidOperationException(
                "The uploaded certificate does not contain a private key. " +
                "A certificate with a private key is required for XAdES digital signing.");
        }

        // Check for duplicate thumbprint (within the same company)
        var existing = await _dbContext.DigitalCertificates
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Thumbprint == certificate.Thumbprint && c.IsActive);
        if (existing != null)
        {
            _logger.LogWarning("Certificate with thumbprint {Thumbprint} already exists (ID: {Id})", certificate.Thumbprint, existing.Id);
            throw new InvalidOperationException(
                $"A certificate with the same thumbprint '{certificate.Thumbprint}' already exists " +
                $"(ID: {existing.Id}, Name: {existing.CertificateName}).");
        }

        // Deactivate other certificates for the same company if activating this one
        if (isActive)
        {
            var activeCerts = await _dbContext.DigitalCertificates
                .Where(c => c.CompanyId == companyId && c.IsActive)
                .ToListAsync();
            foreach (var active in activeCerts)
            {
                active.IsActive = false;
                active.UpdatedAt = DateTime.UtcNow;
            }
        }

        var entity = new DigitalCertificate
        {
            CompanyId = companyId,
            CertificateName = name,
            Issuer = certificate.Issuer,
            SerialNumber = certificate.SerialNumber,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            Thumbprint = certificate.Thumbprint,
            IsActive = isActive,
            IsSandbox = false, // Caller should set this based on context
            VaultObjectId = $"cert-upload-{fileName}-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        _dbContext.DigitalCertificates.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Certificate uploaded: Name={Name}, Issuer={Issuer}, Thumbprint={Thumbprint}, Expiry={Expiry}, Active={Active}", name, entity.Issuer, entity.Thumbprint, entity.NotAfter, isActive);

        return MapToDto(entity);
    }

    /// <inheritdoc/>
    public async Task<CertificateDto> ActivateAsync(Guid id)
    {
        var cert = await _dbContext.DigitalCertificates.FindAsync(id)
            ?? throw new InvalidOperationException($"Certificate {id} not found.");

        // Deactivate all other certificates first
        var otherCerts = await _dbContext.DigitalCertificates
            .Where(c => c.Id != id && c.IsActive)
            .ToListAsync();
        foreach (var other in otherCerts)
        {
            other.IsActive = false;
            other.UpdatedAt = DateTime.UtcNow;
        }

        cert.IsActive = true;
        cert.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Certificate activated: Id={Id}, Name={Name}", id, cert.CertificateName);

        return MapToDto(cert);
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeAsync(Guid id, string reason)
    {
        var cert = await _dbContext.DigitalCertificates.FindAsync(id);
        if (cert == null)
        {
            _logger.LogWarning("Certificate {Id} not found for revocation", id);
            return false;
        }

        cert.IsActive = false;
        cert.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Certificate revoked: Id={Id}, Name={Name}, Reason={Reason}", id, cert.CertificateName, reason);

        return true;
    }

    private static CertificateDto MapToDto(DigitalCertificate entity) => new()
    {
        Id = entity.Id,
        Name = entity.CertificateName,
        SerialNumber = entity.SerialNumber,
        NotBefore = entity.NotBefore,
        NotAfter = entity.NotAfter,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt ?? entity.CreatedAt
    };
}
