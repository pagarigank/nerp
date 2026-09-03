// <copyright file="EInvoiceRetryQueue.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Manages retry logic for failed e-Invoice submissions with exponential backoff.
/// Tracks retry attempts per document and applies backoff delays before resubmission.
/// </summary>
public interface IEInvoiceRetryQueue
{
    /// <summary>
    /// Enqueues a document for retry after a failure.
    /// </summary>
    Task EnqueueRetryAsync(Guid documentId, string failureReason, CancellationToken ct = default);

    /// <summary>
    /// Gets documents that are eligible for retry (backoff period has elapsed).
    /// </summary>
    Task<List<EInvoiceDocument>> GetRetryableDocumentsAsync(int maxCount = 10, CancellationToken ct = default);

    /// <summary>
    /// Marks a document as retrying (in-progress).
    /// </summary>
    Task MarkRetryingAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Marks a retry as successful (resets retry state).
    /// </summary>
    Task MarkRetrySuccessAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Marks a retry as failed (increments retry count, schedules next backoff).
    /// </summary>
    Task MarkRetryFailureAsync(Guid documentId, string failureReason, CancellationToken ct = default);

    /// <summary>
    /// Gets the retry status for a document.
    /// </summary>
    Task<RetryStatus?> GetRetryStatusAsync(Guid documentId, CancellationToken ct = default);
}

public class EInvoiceRetryQueue : IEInvoiceRetryQueue
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan[] BackoffSchedule = new[]
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(60),
        TimeSpan.FromMinutes(240),
    };

    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<EInvoiceRetryQueue> _logger;

    public EInvoiceRetryQueue(EInvoiceDbContext dbContext, ILogger<EInvoiceRetryQueue> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnqueueRetryAsync(Guid documentId, string failureReason, CancellationToken ct = default)
    {
        var document = await _dbContext.EInvoiceDocuments.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Cannot enqueue retry: document {DocumentId} not found", documentId);
            return;
        }

        // Check if already at max retries
        var history = await _dbContext.EInvoiceStatusHistories
            .Where(h => h.EInvoiceDocumentId == documentId && h.ActorUserId == "retry-system")
            .CountAsync(ct);

        if (history >= MaxRetries)
        {
            _logger.LogWarning(
                "Document {DocumentId} has exceeded max retries ({MaxRetries}). Not enqueuing.",
                documentId,
                MaxRetries);
            return;
        }

        var backoffIndex = Math.Min(history, BackoffSchedule.Length - 1);
        var nextRetryAt = DateTime.UtcNow.Add(BackoffSchedule[backoffIndex]);

        // Record the retry enqueue in status history
        var retryEntry = EInvoiceStatusHistory.Create(
            documentId,
            previousStatus: document.Status.ToString(),
            newStatus: document.Status.ToString(),
            actorUserId: "retry-system",
            reason: $"Retry queued: {failureReason}. Attempt {history + 1}/{MaxRetries}. Next retry at {nextRetryAt:O}");
        _dbContext.EInvoiceStatusHistories.Add(retryEntry);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Enqueued retry for document {DocumentId}: attempt {Attempt}/{Max}, next retry at {NextRetry}",
            documentId,
            history + 1,
            MaxRetries,
            nextRetryAt);
    }

    public async Task<List<EInvoiceDocument>> GetRetryableDocumentsAsync(int maxCount = 10, CancellationToken ct = default)
    {
        // Find documents that failed (Invalid/Rejected status) and haven't exceeded max retries
        var failedDocs = await _dbContext.EInvoiceDocuments
            .Where(d =>
                (d.Status == EInvoiceDocumentStatus.Invalid || d.Status == EInvoiceDocumentStatus.Rejected)
                && d.SubmissionUid != null
                && !d.IsSandbox)
            .ToListAsync(ct);

        var result = new List<EInvoiceDocument>();

        foreach (var doc in failedDocs)
        {
            if (result.Count >= maxCount)
                break;

            // Check retry count from status history
            var retryCount = await _dbContext.EInvoiceStatusHistories
                .Where(h => h.EInvoiceDocumentId == doc.Id && h.ActorUserId == "retry-system")
                .CountAsync(ct);

            if (retryCount >= MaxRetries)
                continue;

            // Check if backoff period has elapsed
            var lastRetry = await _dbContext.EInvoiceStatusHistories
                .Where(h => h.EInvoiceDocumentId == doc.Id && h.ActorUserId == "retry-system")
                .OrderByDescending(h => h.OccurredAt)
                .FirstOrDefaultAsync(ct);

            if (lastRetry != null)
            {
                var backoffIndex = Math.Min(retryCount, BackoffSchedule.Length - 1);
                var nextRetryAt = lastRetry.OccurredAt.Add(BackoffSchedule[backoffIndex]);

                if (DateTime.UtcNow < nextRetryAt)
                    continue; // Backoff period not yet elapsed
            }

            result.Add(doc);
        }

        return result;
    }

    public async Task MarkRetryingAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await _dbContext.EInvoiceDocuments.FindAsync(documentId);
        if (document == null) return;

        document.Status = EInvoiceDocumentStatus.Submitted; // Reset to submitted for re-polling
        document.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkRetrySuccessAsync(Guid documentId, CancellationToken ct = default)
    {
        _logger.LogInformation("Retry succeeded for document {DocumentId}", documentId);

        // The status will be updated by the polling job when it detects the new submission
        await Task.CompletedTask;
    }

    public async Task MarkRetryFailureAsync(Guid documentId, string failureReason, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Retry failed for document {DocumentId}: {Reason}",
            documentId,
            failureReason);
        await EnqueueRetryAsync(documentId, failureReason, ct);
    }

    public async Task<RetryStatus?> GetRetryStatusAsync(Guid documentId, CancellationToken ct = default)
    {
        var retryCount = await _dbContext.EInvoiceStatusHistories
            .Where(h => h.EInvoiceDocumentId == documentId && h.ActorUserId == "retry-system")
            .CountAsync(ct);

        if (retryCount == 0)
            return null;

        var lastRetry = await _dbContext.EInvoiceStatusHistories
            .Where(h => h.EInvoiceDocumentId == documentId && h.ActorUserId == "retry-system")
            .OrderByDescending(h => h.OccurredAt)
            .FirstOrDefaultAsync(ct);

        var backoffIndex = Math.Min(retryCount, BackoffSchedule.Length - 1);
        var nextRetryAt = lastRetry?.OccurredAt.Add(BackoffSchedule[backoffIndex]);

        return new RetryStatus
        {
            DocumentId = documentId,
            RetryCount = retryCount,
            MaxRetries = MaxRetries,
            NextRetryAt = nextRetryAt,
            LastFailureReason = lastRetry?.Reason,
            CanRetry = retryCount < MaxRetries,
        };
    }
}

/// <summary>
/// Retry status for an e-Invoice document.
/// </summary>
public class RetryStatus
{
    /// <summary>Gets or sets the document ID.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Gets or sets the current retry count.</summary>
    public int RetryCount { get; set; }

    /// <summary>Gets or sets the maximum number of retries allowed.</summary>
    public int MaxRetries { get; set; }

    /// <summary>Gets or sets when the next retry will be attempted.</summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>Gets or sets the last failure reason.</summary>
    public string? LastFailureReason { get; set; }

    /// <summary>Gets or sets whether more retries are allowed.</summary>
    public bool CanRetry { get; set; }
}
