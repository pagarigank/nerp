// <copyright file="ReportExecutionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Diagnostics;
using Asp.Versioning;
using ERP.Modules.Reporting.Infrastructure;
using ERP.Modules.Reporting.Services;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reporting/execution")]
public class ReportExecutionController : ControllerBase
{
    private readonly ReportingDbContext _context;
    private readonly IReportOutputCacheService _cacheService;
    private readonly ReportDeliveryJob _deliveryJob;

    public ReportExecutionController(
        ReportingDbContext context,
        IReportOutputCacheService cacheService,
        ReportDeliveryJob deliveryJob)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _deliveryJob = deliveryJob ?? throw new ArgumentNullException(nameof(deliveryJob));
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<ReportExecutionResult>>> ExecuteReport(
        [FromBody] ExecuteReportRequest request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var log = new Domain.Entities.ReportUsageLog(
            Guid.Empty,
            request.ReportType ?? "Custom",
            request.ReportDefinitionId,
            null,
            User?.Identity?.Name ?? "system",
            request.ParametersJson,
            request.ExportFormat ?? "Screen",
            0,
            0);

        _context.ReportUsageLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        sw.Stop();
        return Ok(ApiResponse<ReportExecutionResult>.Success(
            new ReportExecutionResult(
                true,
                null,
                [],
                0,
                sw.ElapsedMilliseconds,
                null)));
    }

    [HttpGet("usage-stats")]
    public async Task<ActionResult<ApiResponse<ReportUsageStatsDto>>> GetUsageStats(
        [FromQuery] Guid companyId,
        [FromQuery] int topN = 20,
        CancellationToken cancellationToken = default)
    {
        var logs = await _context.ReportUsageLogs
            .OrderByDescending(l => l.ExecutedOn)
            .Take(topN)
            .ToListAsync(cancellationToken);

        var totalRuns = await _context.ReportUsageLogs.CountAsync(cancellationToken);
        var uniqueReports = await _context.ReportUsageLogs
            .Select(l => l.ReportDefinitionId)
            .Distinct()
            .CountAsync(cancellationToken);
        var avgTime = await _context.ReportUsageLogs
            .AverageAsync(l => (double?)l.ExecutionTimeMs, cancellationToken) ?? 0;

        return Ok(ApiResponse<ReportUsageStatsDto>.Success(new ReportUsageStatsDto(
            totalRuns,
            uniqueReports,
            avgTime,
            logs.Select(l => new ReportUsageLogDto(
                l.Id,
                l.ReportType,
                l.ReportDefinitionId,
                l.SavedQueryId,
                l.ExecutedByUser,
                l.ParametersJson,
                l.ExportFormat,
                l.ExecutionTimeMs,
                l.RowCount,
                l.Status,
                l.ErrorMessage,
                l.ExecutedOn)).ToList())));
    }

    [HttpGet("drill-back")]
    public ActionResult<ApiResponse<DrillBackResult>> DrillBack(
        [FromQuery] string sourceModule,
        [FromQuery] string sourceType,
        [FromQuery] Guid sourceId)
    {
        var result = new DrillBackResult(
            sourceModule,
            sourceType,
            sourceId,
            new Uri($"/{sourceModule.ToUpperInvariant()}/detail/{sourceId}", UriKind.Relative),
            DateTimeOffset.UtcNow);

        return Ok(ApiResponse<DrillBackResult>.Success(result));
    }

    [HttpPost("run-delivery")]
    public async Task<ActionResult<ApiResponse<DeliveryResult>>> RunDelivery(
        [FromQuery] Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        var result = await _deliveryJob.DeliverOneAsync(subscriptionId, cancellationToken);
        return Ok(ApiResponse<DeliveryResult>.Success(result));
    }

    [HttpGet("cache-stats")]
    public ActionResult<ApiResponse<CacheStatisticsDto>> GetCacheStats()
    {
        var stats = _cacheService.GetStatistics();
        return Ok(ApiResponse<CacheStatisticsDto>.Success(new CacheStatisticsDto(
            stats.Hits,
            stats.Misses,
            stats.Evictions,
            stats.EntryCount,
            Math.Round(stats.HitRate, 2))));
    }

    [HttpPost("cache/invalidate")]
    public ActionResult<ApiResponse<bool>> InvalidateCache(
        [FromQuery] Guid? reportId = null)
    {
        if (reportId.HasValue)
        {
            _cacheService.InvalidateReport(reportId.Value.ToString());
        }
        else
        {
            _cacheService.InvalidateAll();
        }

        return Ok(ApiResponse<bool>.Success(true));
    }
}

public record DrillBackResult(
    string SourceModule,
    string SourceType,
    Guid SourceId,
    Uri DrillBackUrl,
    DateTimeOffset ResolvedOn);
