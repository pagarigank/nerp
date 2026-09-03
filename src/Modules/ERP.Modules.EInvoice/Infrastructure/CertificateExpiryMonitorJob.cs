// <copyright file="CertificateExpiryMonitorJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Background job that monitors digital certificate expiry dates.
/// Generates warnings at 30, 14, and 7 days before expiry.
/// Runs daily via Hangfire.
/// </summary>
public class CertificateExpiryMonitorJob
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<CertificateExpiryMonitorJob> _logger;

    public CertificateExpiryMonitorJob(EInvoiceDbContext dbContext, ILogger<CertificateExpiryMonitorJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Checks all active certificates for upcoming expiry.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting certificate expiry monitor job");

        var now = DateTime.UtcNow;
        var warningThresholds = new[] { 30, 14, 7 };

        var activeCerts = await _dbContext.DigitalCertificates
            .Where(c => c.IsActive && c.NotAfter > now)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(ct);

        if (activeCerts.Count == 0)
        {
            _logger.LogWarning("No active digital certificates found — e-Invoice signing may fail");
            return;
        }

        foreach (var cert in activeCerts)
        {
            var daysUntilExpiry = (cert.NotAfter - now).Days;

            foreach (var threshold in warningThresholds)
            {
                if (daysUntilExpiry <= threshold && daysUntilExpiry > threshold - 1)
                {
                    _logger.LogWarning(
                        "Certificate EXPIRY WARNING: {Name} (ID: {Id}, Issuer: {Issuer}) expires in {Days} days on {ExpiryDate}. " +
                        "Please renew the certificate before expiry to avoid e-Invoice signing failures.",
                        cert.CertificateName,
                        cert.Id,
                        cert.Issuer,
                        daysUntilExpiry,
                        cert.NotAfter.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            if (daysUntilExpiry <= 0)
            {
                _logger.LogCritical(
                    "Certificate EXPIRED: {Name} (ID: {Id}, Issuer: {Issuer}) expired on {ExpiryDate}. " +
                    "e-Invoice signing will fail until a new certificate is uploaded.",
                    cert.CertificateName,
                    cert.Id,
                    cert.Issuer,
                    cert.NotAfter.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        _logger.LogInformation(
            "Certificate expiry monitor completed: checked {Count} active certificates",
            activeCerts.Count);
    }
}
