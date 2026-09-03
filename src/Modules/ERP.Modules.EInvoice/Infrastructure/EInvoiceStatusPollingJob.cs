// <copyright file="EInvoiceStatusPollingJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Background job that polls MyInvois for submission status of pending documents.
/// Runs on a schedule (every 2 minutes) via Hangfire.
/// Polls documents in Submitted/Validating status until they reach a terminal state.
/// </summary>
public class EInvoiceStatusPollingJob
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly IEInvoiceService _eInvoiceService;
    private readonly ILogger<EInvoiceStatusPollingJob> _logger;

    public EInvoiceStatusPollingJob(
        EInvoiceDbContext dbContext,
        IEInvoiceService eInvoiceService,
        ILogger<EInvoiceStatusPollingJob> logger)
    {
        _dbContext = dbContext;
        _eInvoiceService = eInvoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Polls all pending documents for their current status.
    /// Called by Hangfire on a recurring schedule.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting e-Invoice status polling job");

        // Find all documents that are in a non-terminal state (Submitted or Validating)
        var pendingDocuments = await _dbContext.EInvoiceDocuments
            .Where(d => (d.Status == EInvoiceDocumentStatus.Submitted || d.Status == EInvoiceDocumentStatus.Validating)
                && d.SubmissionUid != null
                && !d.IsSandbox)
            .OrderBy(d => d.SubmittedAt)
            .Take(50) // Process 50 at a time to respect rate limits
            .ToListAsync(ct);

        if (pendingDocuments.Count == 0)
        {
            _logger.LogDebug("No pending e-Invoice documents to poll");
            return;
        }

        _logger.LogInformation("Polling {Count} pending e-Invoice documents", pendingDocuments.Count);

        var polled = 0;
        var completed = 0;
        var failed = 0;

#pragma warning disable S3267 // Loop has async operations with counters — cannot simplify to LINQ
        foreach (var document in pendingDocuments)
        {
#pragma warning restore S3267
            if (ct.IsCancellationRequested)
                break;

            try
            {
                var result = await _eInvoiceService.PollSubmissionStatusAsync(document.Id, ct);
                polled++;

                if (result.IsComplete)
                {
                    completed++;
                    _logger.LogInformation(
                        "Document {DocumentId} reached terminal status: {Status}",
                        document.Id,
                        result.Status);
                }

                // Rate limit: ~300 RPM for Get Submission, so ~200ms between calls is safe
                await Task.Delay(200, ct);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(
                    ex,
                    "Error polling document {DocumentId}",
                    document.Id);
            }
        }

        _logger.LogInformation(
            "e-Invoice status polling completed: polled={Polled}, completed={Completed}, failed={Failed}",
            polled,
            completed,
            failed);
    }
}
