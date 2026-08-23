// <copyright file="PerformanceAlertJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Jobs;

public record PerformanceAlert(Guid ProjectId, string ProjectCode, string AlertType, string Detail);

public record PerformanceAlertReport(
    DateTimeOffset RunAtUtc,
    int ProjectsScanned,
    IReadOnlyList<PerformanceAlert> Alerts);

public interface IPerformanceAlertJob
{
    Task<PerformanceAlertReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Weekly performance alert scan across active projects: over-budget (costs exceed
/// budget, falling back original → revised), negative margin (recognized revenue
/// below incurred cost), and schedule slip (planned end date past while still active).
/// Alerts are logged as warnings for the owning project managers and returned in the
/// report; no email/notification infrastructure is invoked.
/// </summary>
public sealed partial class PerformanceAlertJob : IPerformanceAlertJob
{
    private readonly ProjDbContext _context;
    private readonly ILogger<PerformanceAlertJob> _logger;

    public PerformanceAlertJob(ProjDbContext context, ILogger<PerformanceAlertJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PerformanceAlertReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var today = DateTime.UtcNow.Date;

        var projects = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .OrderBy(p => p.ProjectCode)
            .Select(p => new
            {
                p.Id,
                p.ProjectCode,
                p.Name,
                p.RevisedBudget,
                p.OriginalBudget,
                p.CostsToDate,
                p.RevenueToDate,
                p.PlannedEndDate,
            })
            .ToListAsync(cancellationToken);

        var alerts = new List<PerformanceAlert>();

        foreach (var project in projects)
        {
            var budget = project.RevisedBudget > 0 ? project.RevisedBudget : project.OriginalBudget;
            if (project.CostsToDate > budget && project.CostsToDate > 0)
            {
                alerts.Add(new PerformanceAlert(
                    project.Id,
                    project.ProjectCode,
                    "OverBudget",
                    $"Costs {project.CostsToDate} exceed budget {budget}."));
                LogOverBudget(project.ProjectCode, project.CostsToDate, budget);
            }

            var margin = project.RevenueToDate - project.CostsToDate;
            if (margin < 0m && project.RevenueToDate > 0m)
            {
                alerts.Add(new PerformanceAlert(
                    project.Id,
                    project.ProjectCode,
                    "NegativeMargin",
                    $"Margin {margin} is negative on recognized revenue {project.RevenueToDate}."));
                LogNegativeMargin(project.ProjectCode, margin);
            }

            if (project.PlannedEndDate.HasValue && project.PlannedEndDate.Value.Date < today)
            {
                var daysLate = (today - project.PlannedEndDate.Value.Date).Days;
                alerts.Add(new PerformanceAlert(
                    project.Id,
                    project.ProjectCode,
                    "ScheduleSlip",
                    $"Planned end {project.PlannedEndDate.Value:yyyy-MM-dd} passed {daysLate} days ago and the project is still active."));
                LogScheduleSlip(project.ProjectCode, daysLate);
            }
        }

        LogCompleted(projects.Count, alerts.Count);

        return new PerformanceAlertReport(DateTimeOffset.UtcNow, projects.Count, alerts);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting weekly performance alert scan")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Warning, Message = "PERFORMANCE ALERT over-budget: project {ProjectCode} costs {Costs} exceed budget {Budget}")]
    private partial void LogOverBudget(string projectCode, decimal costs, decimal budget);

    [LoggerMessage(Level = LogLevel.Warning, Message = "PERFORMANCE ALERT negative-margin: project {ProjectCode} margin {Margin}")]
    private partial void LogNegativeMargin(string projectCode, decimal margin);

    [LoggerMessage(Level = LogLevel.Warning, Message = "PERFORMANCE ALERT schedule-slip: project {ProjectCode} is {DaysLate} days past planned end")]
    private partial void LogScheduleSlip(string projectCode, int daysLate);

    [LoggerMessage(Level = LogLevel.Information, Message = "Weekly performance alert scan completed: {Projects} projects scanned, {Alerts} alerts raised")]
    private partial void LogCompleted(int projects, int alerts);
}
