// <copyright file="GlReconciliationCheckJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Jobs;

public record ReconciliationVarianceRow(
    Guid CompanyId,
    Guid ProjectId,
    string ProjectCode,
    decimal ProjectLedgerCost,
    decimal GlNetPosting,
    decimal Variance);

public record GlReconciliationCheckReport(
    DateTimeOffset RunAtUtc,
    int CompaniesChecked,
    int ProjectsChecked,
    decimal Tolerance,
    IReadOnlyList<ReconciliationVarianceRow> Variances);

public interface IGlReconciliationCheckJob
{
    Task<GlReconciliationCheckReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Daily project-to-GL reconciliation gate per company, mirroring the analysis
/// reconcile endpoint: posted project-ledger cost versus GL debit postings carrying
/// the PROJECT segment. Variances beyond tolerance log an alert warning for the
/// project accounting manager; all variances are returned in the report.
/// </summary>
public sealed partial class GlReconciliationCheckJob : IGlReconciliationCheckJob
{
    public const decimal Tolerance = 1.00m;

    private readonly ProjDbContext _context;
    private readonly GlDbContext _glContext;
    private readonly ILogger<GlReconciliationCheckJob> _logger;

    public GlReconciliationCheckJob(
        ProjDbContext context,
        GlDbContext glContext,
        ILogger<GlReconciliationCheckJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _glContext = glContext ?? throw new ArgumentNullException(nameof(glContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GlReconciliationCheckReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var companyIds = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .Select(p => p.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var variances = new List<ReconciliationVarianceRow>();
        var projectsChecked = 0;

        foreach (var companyId in companyIds)
        {
            var projects = await _context.Projects
                .Include(p => p.CostTransactions)
                .Where(p => p.CompanyId == companyId && p.Status == ProjectStatus.Active)
                .OrderBy(p => p.ProjectCode)
                .ToListAsync(cancellationToken);

            foreach (var project in projects)
            {
                projectsChecked++;

                var ledgerCost = project.CostTransactions
                    .Where(t => t.Status == TransactionStatus.Posted)
                    .Sum(t => t.Amount);

                var projectIdStr = project.Id.ToString("D");
                var glDebits = await _glContext.JournalEntryLines
                    .Where(l => l.SegmentsJson != null && l.SegmentsJson.Contains(projectIdStr))
                    .Select(l => l.Debit)
                    .ToListAsync(cancellationToken);
                var glNet = glDebits.Sum();

                var variance = ledgerCost - glNet;
                if (Math.Abs(variance) > Tolerance)
                {
                    variances.Add(new ReconciliationVarianceRow(
                        companyId,
                        project.Id,
                        project.ProjectCode,
                        ledgerCost,
                        glNet,
                        variance));

                    LogVarianceAlert(project.ProjectCode, variance, Tolerance);
                }
            }
        }

        LogCompleted(companyIds.Count, projectsChecked, variances.Count);

        return new GlReconciliationCheckReport(
            DateTimeOffset.UtcNow,
            companyIds.Count,
            projectsChecked,
            Tolerance,
            variances);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting daily project-to-GL reconciliation check")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RECONCILIATION ALERT: project {ProjectCode} has project-to-GL variance {Variance} beyond tolerance {Tolerance}")]
    private partial void LogVarianceAlert(string projectCode, decimal variance, decimal tolerance);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project-to-GL reconciliation completed: {Companies} companies, {Projects} projects checked, {Variances} variance alerts")]
    private partial void LogCompleted(int companies, int projects, int variances);
}
