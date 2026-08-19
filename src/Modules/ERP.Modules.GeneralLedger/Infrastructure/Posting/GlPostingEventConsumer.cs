// <copyright file="GlPostingEventConsumer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.GeneralLedger.Infrastructure.Posting;

/// <summary>
/// Consumes canonical posting events and materializes them as a posted
/// <see cref="JournalBatch"/> in the General Ledger. This is the single
/// inbound path into GL from every sub-ledger (AP, AR, Cash, Inventory,
/// Purchasing, Project Accounting) per architecture.md §5.1.
/// </summary>
public sealed class GlPostingEventConsumer : IPostingEventConsumer
{
    private readonly GlDbContext _glContext;
    private readonly ILogger<GlPostingEventConsumer> _logger;

    public GlPostingEventConsumer(
        GlDbContext glContext,
        ILogger<GlPostingEventConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(glContext);
        ArgumentNullException.ThrowIfNull(logger);

        _glContext = glContext;
        _logger = logger;
    }

    public async Task<Guid> ConsumeAsync(CanonicalPostingEvent postingEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(postingEvent);

        var companyId = postingEvent.CompanyGuid
            ?? throw new InvalidOperationException(
                $"Posting from {postingEvent.SourceModule} ({postingEvent.SourceDocumentId}) must supply a resolved CompanyGuid.");
        var fiscalPeriodId = postingEvent.FiscalPeriodGuid
            ?? throw new InvalidOperationException(
                $"Posting from {postingEvent.SourceModule} ({postingEvent.SourceDocumentId}) must supply a resolved FiscalPeriodGuid.");

        // Idempotency: a source document must not create duplicate GL batches.
        var existing = await _glContext.JournalBatches
            .Where(b => b.CompanyId == companyId && b.BatchNumber == postingEvent.SourceDocumentId)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != Guid.Empty)
        {
            _logger.LogWarning("GL posting for {SourceModule}/{SourceDocumentId} already exists; skipping duplicate.", postingEvent.SourceModule, postingEvent.SourceDocumentId);
            return existing;
        }

        var batch = new JournalBatch(
            companyId,
            postingEvent.SourceDocumentId,
            $"{postingEvent.SourceModule} posting {postingEvent.SourceDocumentId}",
            postingEvent.PostingDate,
            fiscalPeriodId);

        foreach (var line in postingEvent.Lines)
        {
            var accountId = line.AccountId
                ?? throw new InvalidOperationException(
                    $"Posting line for account '{line.Account}' must supply a resolved AccountId.");

            var segmentsJson = line.Segments.Segments.Count > 0
                ? JsonSerializer.Serialize(line.Segments.Segments)
                : null;

            var projectReference = line.Segments["PROJECT"];
            var reference = projectReference is not null
                ? $"{postingEvent.SourceDocumentId} / {projectReference}"
                : postingEvent.SourceDocumentId;

            batch.AddLine(
                accountId,
                line.Debit > 0 ? line.Debit : null,
                line.Credit > 0 ? line.Credit : null,
                reference,
                segmentsJson);
        }

        batch.Release();
        batch.Post();

        _glContext.JournalBatches.Add(batch);
        await _glContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created and posted GL JournalBatch {BatchNumber} from {SourceModule} ({Lines} lines).", postingEvent.SourceDocumentId, postingEvent.SourceModule, postingEvent.Lines.Count);

        return batch.Id;
    }
}
