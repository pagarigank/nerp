// <copyright file="AllocatorRunJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Jobs;

public record AllocatorRunProjectResult(
    Guid ProjectId,
    string ProjectCode,
    int TransactionsAllocated,
    decimal BaseCost,
    decimal BurdenApplied);

public record AllocatorRunReport(
    int ProjectsProcessed,
    int TransactionsAllocated,
    decimal TotalBaseCost,
    decimal TotalBurdenApplied,
    IReadOnlyList<AllocatorRunProjectResult> Results);

public interface IAllocatorRunJob
{
    Task<AllocatorRunReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Nightly allocator run: resolves the burden/markup engine behind the
/// calculate-burden endpoint (<c>IProjectAllocator</c>) for every posted cost
/// transaction not yet allocated, and persists the computed overhead/markup onto
/// the transaction via <see cref="CostTransaction.SetBurden"/>. Projects whose rules
/// are absent or inactive receive zero burden and are still marked allocated so they
/// are not rescanned nightly; the report makes that visible through BurdenApplied.
/// No new tables are written — only existing CostTransaction columns change.
/// </summary>
public sealed partial class AllocatorRunJob : IAllocatorRunJob
{
    private readonly ProjDbContext _context;
    private readonly Domain.Services.IProjectAllocator _allocator;
    private readonly ILogger<AllocatorRunJob> _logger;

    public AllocatorRunJob(
        ProjDbContext context,
        Domain.Services.IProjectAllocator allocator,
        ILogger<AllocatorRunJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AllocatorRunReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var unallocated = await _context.CostTransactions
            .Where(t => t.Status == TransactionStatus.Posted && !t.IsAllocated && t.Amount != 0m)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        var projectIds = unallocated
            .Select(t => t.ProjectId)
            .Distinct()
            .ToList();

        var projectsById = await _context.Projects
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var totalsByProject = new Dictionary<Guid, (string ProjectCode, int Transactions, decimal BaseCost, decimal BurdenApplied)>();
        var transactionsAllocated = 0;
        var totalBaseCost = 0m;
        var totalBurden = 0m;

        foreach (var txn in unallocated)
        {
            if (!projectsById.TryGetValue(txn.ProjectId, out var project))
            {
                continue;
            }

            var result = await _allocator.CalculateAsync(project.Id, txn.Category, txn.Amount, cancellationToken);
            txn.SetBurden(result.Burden, result.BillableCost);

            transactionsAllocated++;
            totalBaseCost += txn.Amount;
            totalBurden += result.Burden;

            var entry = totalsByProject.TryGetValue(project.Id, out var existing)
                ? (project.ProjectCode, existing.Transactions + 1, existing.BaseCost + txn.Amount, existing.BurdenApplied + result.Burden)
                : (project.ProjectCode, 1, txn.Amount, result.Burden);
            totalsByProject[project.Id] = entry;
        }

        if (unallocated.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var results = totalsByProject
            .OrderBy(kvp => kvp.Value.ProjectCode, StringComparer.Ordinal)
            .Select(kvp => new AllocatorRunProjectResult(kvp.Key, kvp.Value.ProjectCode, kvp.Value.Transactions, kvp.Value.BaseCost, kvp.Value.BurdenApplied))
            .ToList();

        foreach (var result in results)
        {
            LogProjectAllocated(result.ProjectCode, result.TransactionsAllocated, result.BaseCost, result.BurdenApplied);
        }

        LogCompleted(totalsByProject.Count, transactionsAllocated, totalBurden);

        return new AllocatorRunReport(totalsByProject.Count, transactionsAllocated, totalBaseCost, totalBurden, results);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting nightly allocator run")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Allocator applied burden {Burden} on base cost {BaseCost} across {Transactions} transaction(s) for project {ProjectCode}")]
    private partial void LogProjectAllocated(string projectCode, int transactions, decimal baseCost, decimal burden);

    [LoggerMessage(Level = LogLevel.Information, Message = "Allocator run completed: {Projects} projects, {Transactions} transactions allocated, {Burden} total burden")]
    private partial void LogCompleted(int projects, int transactions, decimal burden);
}
