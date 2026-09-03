// <copyright file="CancellationWindowMonitorJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Background job that monitors documents approaching the 72-hour cancellation window expiry.
/// Alerts when valid documents are about to become un-cancellable.
/// Runs every 30 minutes via Hangfire.
/// </summary>
public class CancellationWindowMonitorJob
{
    private static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(72);

    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<CancellationWindowMonitorJob> _logger;

    public CancellationWindowMonitorJob(EInvoiceDbContext dbContext, ILogger<CancellationWindowMonitorJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Starting cancellation window monitor job");

        var now = DateTime.UtcNow;

        // Find valid documents submitted within the last 72 hours that haven't been cancelled
        var recentDocs = await _dbContext.EInvoiceDocuments
            .Where(d =>
                d.Status == EInvoiceDocumentStatus.Valid
                && d.SubmittedAt != null
                && d.SubmittedAt > now.AddHours(-72)
                && !d.IsSandbox)
            .ToListAsync(ct);

        var expiringSoon = 0;
        var expired = 0;

        foreach (var doc in recentDocs)
        {
            var submittedAt = doc.SubmittedAt!.Value;
            var timeElapsed = now - submittedAt;
            var timeRemaining = CancellationWindow - timeElapsed;

            if (timeRemaining <= TimeSpan.Zero)
            {
                expired++;
            }
            else if (timeRemaining <= TimeSpan.FromHours(24))
            {
                expiringSoon++;
                _logger.LogWarning(
                    "Cancellation window EXPIRING SOON: Document {DocumentId} (UUID={Uuid}), " +
                    "submitted at {SubmittedAt}, {HoursRemaining:F1} hours remaining",
                    doc.Id,
                    doc.MyInvoisUuid,
                    submittedAt,
                    timeRemaining.TotalHours);
            }
        }

        if (expiringSoon > 0 || expired > 0)
        {
            _logger.LogWarning(
                "Cancellation window monitor: {ExpiringSoon} document(s) expiring soon, {Expired} already expired",
                expiringSoon,
                expired);
        }
        else
        {
            _logger.LogDebug("No documents approaching cancellation window expiry");
        }
    }
}
