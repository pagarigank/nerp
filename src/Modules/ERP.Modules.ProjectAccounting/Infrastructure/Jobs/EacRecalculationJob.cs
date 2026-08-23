// <copyright file="EacRecalculationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Jobs;

public record EacRecalculationSummary(
    Guid ProjectId,
    string ProjectCode,
    decimal BudgetAtCompletion,
    decimal EstimateAtCompletion,
    decimal EstimateToComplete,
    decimal VarianceAtCompletion);

public record EacRecalculationReport(int ProjectsEvaluated, IReadOnlyList<EacRecalculationSummary> Summaries, int SnapshotsCaptured = 0);

public interface IEacRecalculationJob
{
    Task<EacRecalculationReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Weekly estimate-at-completion recalculation across active projects using the same
/// forecast basis as the analysis forecast endpoint: budget falls back revised →
/// original → contract value, EAC = costs-to-date ÷ physical % complete (budget when
/// no progress is reported). Each run also persists one EAC snapshot per active project
/// per UTC day through <see cref="IEacSnapshotService"/> to build the profit-fade trend.
/// </summary>
public sealed partial class EacRecalculationJob : IEacRecalculationJob
{
    private readonly ProjDbContext _context;
    private readonly IEacSnapshotService _snapshotService;
    private readonly ILogger<EacRecalculationJob> _logger;

    public EacRecalculationJob(ProjDbContext context, IEacSnapshotService snapshotService, ILogger<EacRecalculationJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EacRecalculationReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var projects = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .OrderBy(p => p.ProjectCode)
            .Select(p => new
            {
                p.Id,
                p.ProjectCode,
                p.RevisedBudget,
                p.OriginalBudget,
                p.ContractValue,
                p.CostsToDate,
                p.PercentComplete,
            })
            .ToListAsync(cancellationToken);

        var summaries = new List<EacRecalculationSummary>();

        foreach (var project in projects)
        {
            var budget = project.RevisedBudget;
            if (budget <= 0)
            {
                budget = project.OriginalBudget;
            }

            if (budget <= 0)
            {
                budget = project.ContractValue ?? 0;
            }

            var eac = project.PercentComplete > 0
                ? project.CostsToDate / (project.PercentComplete / 100m)
                : budget;
            var etc = eac - project.CostsToDate;
            var variance = budget - eac;

            summaries.Add(new EacRecalculationSummary(project.Id, project.ProjectCode, budget, eac, etc, variance));
            LogProjectEac(project.ProjectCode, budget, eac, etc, variance);
        }

        var snapshotsCaptured = await _snapshotService.CaptureForAllAsync(cancellationToken);

        LogCompleted(summaries.Count);
        LogSnapshotsCaptured(snapshotsCaptured);

        return new EacRecalculationReport(summaries.Count, summaries, snapshotsCaptured);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting weekly EAC recalculation")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "EAC summary {ProjectCode}: BAC {Budget}, EAC {Eac}, ETC {Etc}, VAC {Variance}")]
    private partial void LogProjectEac(string projectCode, decimal budget, decimal eac, decimal etc, decimal variance);

    [LoggerMessage(Level = LogLevel.Information, Message = "Weekly EAC recalculation completed: {Projects} active projects evaluated")]
    private partial void LogCompleted(int projects);

    [LoggerMessage(Level = LogLevel.Information, Message = "EAC snapshots captured this run: {SnapshotsCaptured}")]
    private partial void LogSnapshotsCaptured(int snapshotsCaptured);
}
