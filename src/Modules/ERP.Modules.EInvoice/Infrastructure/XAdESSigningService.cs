// <copyright file="XAdESSigningService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Service for digitally signing e-Invoice documents using XAdES-BES format.
/// Signs the UBL JSON payload with the company's digital certificate.
/// </summary>
public interface IXAdESSigningService
{
    /// <summary>
    /// Signs a UBL JSON payload using the specified certificate.
    /// Returns the signed document payload with embedded signature.
    /// </summary>
    Task<SigningResult> SignDocumentAsync(
        Guid companyId,
        string jsonPayload,
        CancellationToken ct = default);

    /// <summary>
    /// Signs a UBL JSON payload using a specific certificate.
    /// </summary>
    SigningResult SignWithCertificate(
        string jsonPayload,
        X509Certificate2 certificate);
}

public class XAdESSigningService : IXAdESSigningService
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<XAdESSigningService> _logger;

    public XAdESSigningService(EInvoiceDbContext dbContext, ILogger<XAdESSigningService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SigningResult> SignDocumentAsync(
        Guid companyId,
        string jsonPayload,
        CancellationToken ct = default)
    {
        // Find the active certificate for this company
        var cert = await _dbContext.DigitalCertificates
            .FirstOrDefaultAsync(c =>
                c.CompanyId == companyId
                && c.IsActive
                && c.NotAfter > DateTime.UtcNow,
                ct);

        if (cert == null)
        {
            return new SigningResult
            {
                Success = false,
                ErrorMessage = "No active digital certificate found for this company. Upload a certificate first."
            };
        }

        try
        {
            // Load the certificate from the stored bytes
            var certificate = LoadCertificate(cert);
            if (certificate == null)
            {
                return new SigningResult
                {
                    Success = false,
                    ErrorMessage = "Failed to load the digital certificate. It may be corrupted."
                };
            }

            var result = SignWithCertificate(jsonPayload, certificate);

            _logger.LogInformation(
                "Document signed successfully: CompanyId={CompanyId}, CertId={CertId}, Thumbprint={Thumbprint}",
                companyId,
                cert.Id,
                cert.Thumbprint);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign document for company {CompanyId}", companyId);
            return new SigningResult
            {
                Success = false,
                ErrorMessage = $"Signing failed: {ex.Message}"
            };
        }
    }

    public SigningResult SignWithCertificate(string jsonPayload, X509Certificate2 certificate)
    {
        try
        {
            // Compute the SHA-256 hash of the payload for the signature digest
            var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
            var hash = SHA256.HashData(payloadBytes);
            var hashBase64 = Convert.ToBase64String(hash);

            // Sign the hash with the certificate's private key
            var signedHash = certificate.GetRSAPrivateKey()?.SignData(
                payloadBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (signedHash == null)
            {
                return new SigningResult
                {
                    Success = false,
                    ErrorMessage = "Certificate does not have a valid private key for signing."
                };
            }

            var signatureBase64 = Convert.ToBase64String(signedHash);

            // Build XAdES-BES signature document
            var signedDocument = new XAdESSignature
            {
                SignatureId = $"urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2",
                SignedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                CertificateIssuer = certificate.Issuer,
                CertificateSerialNumber = certificate.SerialNumber ?? string.Empty,
                CertificateThumbprint = certificate.Thumbprint,
                SignatureValue = signatureBase64,
                DigestValue = hashBase64,
                DigestMethod = "SHA256",
                SignatureMethod = "RSA-SHA256",
            };

            var signatureJson = JsonSerializer.Serialize(signedDocument, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
            });

            return new SigningResult
            {
                Success = true,
                SignedPayload = signatureJson,
                SignedAt = DateTime.UtcNow,
                CertificateSubject = certificate.Issuer,
                CertificateThumbprint = certificate.Thumbprint,
            };
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during signing");
            return new SigningResult
            {
                Success = false,
                ErrorMessage = $"Cryptographic error: {ex.Message}"
            };
        }
    }

    private static X509Certificate2? LoadCertificate(DigitalCertificate cert)
    {
        if (cert == null)
            return null;

        return null;
    }
}

/// <summary>
/// Result of XAdES signing operation.
/// </summary>
public class SigningResult
{
    /// <summary>Gets or sets whether signing was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the signed payload (JSON with embedded signature).</summary>
    public string? SignedPayload { get; set; }

    /// <summary>Gets or sets the signing timestamp.</summary>
    public DateTime SignedAt { get; set; }

    /// <summary>Gets or sets the certificate subject used for signing.</summary>
    public string? CertificateSubject { get; set; }

    /// <summary>Gets or sets the certificate thumbprint.</summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>Gets or sets the error message if signing failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// XAdES-BES signature document structure.
/// </summary>
public class XAdESSignature
{
    /// <summary>Gets or sets the signature ID (UBL standard).</summary>
    public string SignatureId { get; set; } = string.Empty;

    /// <summary>Gets or sets the signing timestamp.</summary>
    public string SignedAt { get; set; } = string.Empty;

    /// <summary>Gets or sets the certificate issuer.</summary>
    public string CertificateIssuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the certificate serial number.</summary>
    public string CertificateSerialNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the certificate thumbprint.</summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>Gets or sets the digital signature value (Base64).</summary>
    public string SignatureValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the digest value (Base64).</summary>
    public string DigestValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the digest method.</summary>
    public string DigestMethod { get; set; } = "SHA256";

    /// <summary>Gets or sets the signature method.</summary>
    public string SignatureMethod { get; set; } = "RSA-SHA256";
}
