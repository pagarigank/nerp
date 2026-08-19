// <copyright file="BatchPostingQueueProcessor.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class BatchPostingQueueProcessor
{
    private readonly GlDbContext _context;
    private readonly ILogger<BatchPostingQueueProcessor> _logger;

    public BatchPostingQueueProcessor(GlDbContext context, ILogger<BatchPostingQueueProcessor> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batch posting queue processing");

        try
        {
            var batches = await _context.JournalBatches
                .Where(b => b.Status == JournalBatchStatus.Balanced)
                .Include(b => b.Lines)
                .ToListAsync(cancellationToken);

            if (batches.Count == 0)
            {
                _logger.LogInformation("No balanced batches found to post");
                return;
            }

            _logger.LogInformation("Found {Count} balanced batches to post", batches.Count);

            foreach (var batch in batches)
            {
                _logger.LogInformation("Posting batch {BatchNumber} (Id: {BatchId})", batch.BatchNumber, batch.Id);
                batch.Post();
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Batch posting queue processing completed. Posted {Count} batches.", batches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch posting queue processing failed");
            throw;
        }
    }
}
