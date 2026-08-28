// <copyright file="ReportCatalogController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Api;

[ApiController]
[Route("api/v1/reporting/catalog")]
public class ReportCatalogController : ControllerBase
{
    private readonly ReportingDbContext _db;

    public ReportCatalogController(ReportingDbContext db)
    {
        _db = db;
    }

    [HttpGet("usage-report")]
    public async Task<IActionResult> GetUsageReport(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? module)
    {
        var startDate = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = to ?? DateTimeOffset.UtcNow;

        var query = _db.ReportUsageLogs
            .Where(x => x.CompanyId == companyId && x.ExecutedOn >= startDate && x.ExecutedOn <= endDate);

        if (!string.IsNullOrEmpty(module))
        {
            var reportIds = await _db.ReportDefinitions
                .Where(x => x.CompanyId == companyId && x.Module == module && !x.DeletedOn.HasValue)
                .Select(x => x.Id)
                .ToListAsync();
            query = query.Where(x => x.ReportDefinitionId.HasValue && reportIds.Contains(x.ReportDefinitionId.Value));
        }

        var logs = await query.ToListAsync();

        var totalRuns = logs.Count;
        var avgExecutionTime = logs.Count > 0 ? (long)logs.Average(x => x.ExecutionTimeMs) : 0;
        var failedRuns = logs.Count(x => x.Status == "Failed");

        var mostRun = logs
            .Where(x => x.ReportDefinitionId.HasValue)
            .GroupBy(x => x.ReportDefinitionId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new
            {
                ReportDefinitionId = g.Key,
                RunCount = g.Count(),
                AvgExecutionTime = (long)g.Average(x => x.ExecutionTimeMs),
                LastRun = g.Max(x => x.ExecutedOn)
            })
            .ToList();

        var slowest = logs
            .Where(x => x.ReportDefinitionId.HasValue)
            .GroupBy(x => x.ReportDefinitionId!.Value)
            .OrderByDescending(g => g.Average(x => x.ExecutionTimeMs))
            .Take(10)
            .Select(g => new
            {
                ReportDefinitionId = g.Key,
                AvgExecutionTime = (long)g.Average(x => x.ExecutionTimeMs),
                MaxExecutionTime = g.Max(x => x.ExecutionTimeMs),
                RunCount = g.Count()
            })
            .ToList();

        var exportActivity = logs
            .GroupBy(x => x.ExportFormat)
            .Select(g => new
            {
                Format = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var usageByModule = logs
            .GroupBy(x => x.ReportType)
            .Select(g => new
            {
                Module = g.Key,
                RunCount = g.Count(),
                AvgExecutionTime = (long)g.Average(x => x.ExecutionTimeMs)
            })
            .OrderByDescending(x => x.RunCount)
            .ToList();

        var dailyTrend = logs
            .GroupBy(x => x.ExecutedOn.Date)
            .Select(g => new
            {
                Date = g.Key,
                RunCount = g.Count(),
                FailedCount = g.Count(x => x.Status == "Failed")
            })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            Summary = new
            {
                TotalRuns = totalRuns,
                AvgExecutionTimeMs = avgExecutionTime,
                FailedRuns = failedRuns,
                SuccessRate = totalRuns > 0 ? Math.Round((double)(totalRuns - failedRuns) / totalRuns * 100, 1) : 0
            },
            MostRunReports = mostRun,
            SlowestReports = slowest,
            ExportActivity = exportActivity,
            UsageByModule = usageByModule,
            DailyTrend = dailyTrend
        }));
    }

    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        var watermarks = await _db.SyncWatermarks
            .OrderBy(x => x.SourceTable)
            .ToListAsync();

        var recentRuns = await _db.SyncRunLogs
            .OrderByDescending(x => x.StartedOn)
            .Take(50)
            .ToListAsync();

        var sourceModules = watermarks
            .GroupBy(x => x.SourceTable.Split('.')[0])
            .Select(g => new
            {
                Module = g.Key,
                TableCount = g.Count(),
                LastSynced = g.Max(x => x.LastSyncOn),
                TotalRowsSynced = g.Sum(x => x.TotalRowsSynced)
            })
            .OrderBy(x => x.Module)
            .ToList();

        var syncErrors = recentRuns
            .Where(x => x.Status == "Failed")
            .GroupBy(x => x.SourceTable)
            .Select(g => new
            {
                SourceTable = g.Key,
                ErrorCount = g.Count(),
                LastError = g.OrderByDescending(x => x.StartedOn).First().ErrorMessage,
                LastErrorOn = g.OrderByDescending(x => x.StartedOn).First().StartedOn
            })
            .ToList();

        var completedRuns = recentRuns.Where(x => x.CompletedOn.HasValue && x.Status == "Success").ToList();
        var avgSyncDuration = completedRuns.Count > 0
            ? completedRuns.Average(x => (x.CompletedOn!.Value - x.StartedOn).TotalMilliseconds)
            : 0;

        return Ok(ApiResponse<object>.Success(new
        {
            SourceModules = sourceModules,
            Tables = watermarks.Select(w => new
            {
                w.SourceTable,
                w.StagingTable,
                LastSyncOn = w.LastSyncOn,
                w.TotalRowsSynced
            }),
            RecentRuns = recentRuns.Select(r => new
            {
                r.SourceTable,
                r.Status,
                r.RowsSynced,
                r.StartedOn,
                r.CompletedOn,
                DurationMs = r.CompletedOn.HasValue ? (r.CompletedOn.Value - r.StartedOn).TotalMilliseconds : 0,
                r.ErrorMessage
            }),
            SyncErrors = syncErrors,
            AvgSyncDurationMs = avgSyncDuration
        }));
    }
}
