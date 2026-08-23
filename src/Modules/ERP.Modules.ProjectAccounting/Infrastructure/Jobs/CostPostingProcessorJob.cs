// <copyright file="CostPostingProcessorJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Jobs;

public record CostPostingSourceCount(string Source, int Count);

public record CostPostingHealthReport(
    DateTimeOffset RunAtUtc,
    int WindowHours,
    string Mode,
    bool QueueBacked,
    IReadOnlyList<CostPostingSourceCount> ProcessedBySourceLast24h,
    int GlDualPostingsLast24h);

public interface ICostPostingProcessorJob
{
    Task<CostPostingHealthReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Nightly cost-posting pass. Cross-module cost events (AP voucher, payroll labor,
/// inventory issue) are delivered synchronously by the in-process domain-event
/// dispatcher at post time; there is no durable event queue to re-drive, and handler
/// failures already abort the originating post. This job is therefore the honest
/// health/report variant: it reports project-cost postings over the last 24 hours
/// grouped by source transaction type plus the matching count of PROJ-sourced GL
/// dual-post batches, so a silent drop in any source leg surfaces in operations.
/// </summary>
public sealed partial class CostPostingProcessorJob : ICostPostingProcessorJob
{
    public const string GlProjectBatchPrefix = "PROJ-COST-";
    public const int WindowHours = 24;

    private readonly ProjDbContext _context;
    private readonly GlDbContext _glContext;
    private readonly ILogger<CostPostingProcessorJob> _logger;

    public CostPostingProcessorJob(
        ProjDbContext context,
        GlDbContext glContext,
        ILogger<CostPostingProcessorJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _glContext = glContext ?? throw new ArgumentNullException(nameof(glContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CostPostingHealthReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var since = DateTimeOffset.UtcNow.AddHours(-WindowHours);

        var grouped = await _context.CostTransactions
            .Where(t => t.CreatedOn >= since)
            .GroupBy(t => t.TransactionType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var glDualPostings = await _glContext.JournalBatches
            .Where(b => b.BatchNumber.StartsWith(GlProjectBatchPrefix) && b.CreatedOn >= since)
            .CountAsync(cancellationToken);

        var counts = grouped
            .Select(g => new CostPostingSourceCount(g.Key.ToString(), g.Count))
            .OrderBy(c => c.Source, StringComparer.Ordinal)
            .ToList();

        foreach (var count in counts)
        {
            LogSourceCount(count.Source, count.Count);
        }

        LogCompleted(counts.Sum(c => c.Count), glDualPostings);

        return new CostPostingHealthReport(
            DateTimeOffset.UtcNow,
            WindowHours,
            "inline-dispatch",
            false,
            counts,
            glDualPostings);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting nightly cost posting health check")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Cost postings last 24h: source {Source} count {Count}")]
    private partial void LogSourceCount(string source, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cost posting health check completed: {Total} project cost transactions and {GlBatches} PROJ dual-posted GL batches in window")]
    private partial void LogCompleted(int total, int glBatches);
}
