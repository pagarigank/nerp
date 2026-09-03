// <copyright file="EInvoiceRetryJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Background job that processes the e-Invoice retry queue.
/// Retries failed submissions with exponential backoff.
/// Runs every 5 minutes via Hangfire.
/// </summary>
public class EInvoiceRetryJob
{
    private readonly IEInvoiceRetryQueue _retryQueue;
    private readonly IEInvoiceService _eInvoiceService;
    private readonly ILogger<EInvoiceRetryJob> _logger;

    public EInvoiceRetryJob(
        IEInvoiceRetryQueue retryQueue,
        IEInvoiceService eInvoiceService,
        ILogger<EInvoiceRetryJob> logger)
    {
        _retryQueue = retryQueue;
        _eInvoiceService = eInvoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Processes documents eligible for retry.
    /// Called by Hangfire on a recurring schedule (every 5 minutes).
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting e-Invoice retry queue processing");

        var retryableDocs = await _retryQueue.GetRetryableDocumentsAsync(maxCount: 5, ct);

        if (retryableDocs.Count == 0)
        {
            _logger.LogDebug("No documents eligible for retry");
            return;
        }

        _logger.LogInformation("Processing {Count} documents for retry", retryableDocs.Count);

        foreach (var doc in retryableDocs)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                // Mark as retrying (resets status to Submitted for re-polling)
                await _retryQueue.MarkRetryingAsync(doc.Id, ct);

                _logger.LogInformation(
                    "Retrying document {DocumentId} (UUID: {Uuid})",
                    doc.Id,
                    doc.MyInvoisUuid);

                // Poll the existing submission status — the document was already submitted
                // before it failed, so we just need to check if MyInvois has updated it
                var result = await _eInvoiceService.PollSubmissionStatusAsync(doc.Id, ct);

                if (result.IsComplete && result.Status == "Valid")
                {
                    await _retryQueue.MarkRetrySuccessAsync(doc.Id, ct);
                    _logger.LogInformation(
                        "Retry succeeded for document {DocumentId}: status={Status}",
                        doc.Id,
                        result.Status);
                }
                else if (result.IsComplete)
                {
                    // Still failed — enqueue for another retry
                    await _retryQueue.MarkRetryFailureAsync(
                        doc.Id,
                        result.ErrorMessage ?? $"Status: {result.Status}",
                        ct);
                }
                else
                {
                    _logger.LogDebug(
                        "Document {DocumentId} still processing after retry poll",
                        doc.Id);
                }

                // Rate limit between retries
                await Task.Delay(500, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing retry for document {DocumentId}", doc.Id);
                await _retryQueue.MarkRetryFailureAsync(doc.Id, ex.Message, ct);
            }
        }

        _logger.LogInformation("e-Invoice retry queue processing completed");
    }
}
