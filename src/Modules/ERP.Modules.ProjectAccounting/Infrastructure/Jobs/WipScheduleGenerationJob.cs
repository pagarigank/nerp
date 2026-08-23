// <copyright file="WipScheduleGenerationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Jobs;

public record WipScheduleRow(
    Guid ProjectId,
    string ProjectCode,
    decimal ContractValue,
    decimal CostsToDate,
    decimal PercentComplete,
    decimal EarnedRevenue,
    decimal BilledToDate,
    decimal OverUnderBilling,
    decimal RetainageHeld);

public record WipScheduleGenerationReport(
    DateTimeOffset GeneratedAtUtc,
    int ProjectsIncluded,
    decimal TotalContractValue,
    decimal TotalCostsToDate,
    decimal TotalEarnedRevenue,
    decimal TotalBilledToDate,
    decimal TotalOverUnderBilling,
    IReadOnlyList<WipScheduleRow> Rows);

public interface IWipScheduleGenerationJob
{
    Task<WipScheduleGenerationReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Monthly work-in-progress schedule generation mirroring the analysis WIP endpoint's
/// math: earned revenue = contract value × % complete (falling back to costs-to-date
/// when the project has no contract value), over/under billing = earned − billed.
/// Report-only by design: totals per project are logged and returned for financial
/// close review; nothing is persisted.
/// </summary>
public sealed partial class WipScheduleGenerationJob : IWipScheduleGenerationJob
{
    private readonly ProjDbContext _context;
    private readonly ILogger<WipScheduleGenerationJob> _logger;

    public WipScheduleGenerationJob(ProjDbContext context, ILogger<WipScheduleGenerationJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WipScheduleGenerationReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var projects = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Completed)
            .OrderBy(p => p.ProjectCode)
            .Select(p => new
            {
                p.Id,
                p.ProjectCode,
                p.ContractValue,
                p.CostsToDate,
                p.PercentComplete,
                p.RevenueToDate,
                p.RetainageHeld,
            })
            .ToListAsync(cancellationToken);

        var rows = new List<WipScheduleRow>();

        foreach (var project in projects)
        {
            var contractValue = project.ContractValue ?? 0;
            var earned = project.ContractValue.HasValue
                ? project.ContractValue.Value * (project.PercentComplete / 100m)
                : project.CostsToDate;
            var overUnder = earned - project.RevenueToDate;

            rows.Add(new WipScheduleRow(
                project.Id,
                project.ProjectCode,
                contractValue,
                project.CostsToDate,
                project.PercentComplete,
                earned,
                project.RevenueToDate,
                overUnder,
                project.RetainageHeld));

            LogProjectWip(project.ProjectCode, contractValue, project.CostsToDate, earned, project.RevenueToDate, overUnder);
        }

        LogCompleted(rows.Count, rows.Sum(r => r.EarnedRevenue));

        return new WipScheduleGenerationReport(
            DateTimeOffset.UtcNow,
            rows.Count,
            rows.Sum(r => r.ContractValue),
            rows.Sum(r => r.CostsToDate),
            rows.Sum(r => r.EarnedRevenue),
            rows.Sum(r => r.BilledToDate),
            rows.Sum(r => r.OverUnderBilling),
            rows);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting monthly WIP schedule generation")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "WIP {ProjectCode}: contract {Contract}, costs {Costs}, earned {Earned}, billed {Billed}, over/under {OverUnder}")]
    private partial void LogProjectWip(string projectCode, decimal contract, decimal costs, decimal earned, decimal billed, decimal overUnder);

    [LoggerMessage(Level = LogLevel.Information, Message = "Monthly WIP schedule generation completed: {Projects} projects, {TotalEarned} total earned revenue")]
    private partial void LogCompleted(int projects, decimal totalEarned);
}
