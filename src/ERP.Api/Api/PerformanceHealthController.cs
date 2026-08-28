// <copyright file="PerformanceHealthController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, S1118, S6960

using Asp.Versioning;
using ERP.Api.Performance;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/performance")]
public class PerformanceHealthController : ControllerBase
{
    private readonly IQueryOptimizationAuditService queryAuditService;
    private readonly IDatabaseArchivalService archivalService;
    private readonly IBatchOperationOptimizer batchOptimizer;

    public PerformanceHealthController(
        IQueryOptimizationAuditService queryAuditService,
        IDatabaseArchivalService archivalService,
        IBatchOperationOptimizer batchOptimizer)
    {
        this.queryAuditService = queryAuditService;
        this.archivalService = archivalService;
        this.batchOptimizer = batchOptimizer;
    }

    // Query Optimization
    [HttpGet("query-audit")]
    [ProducesResponseType(typeof(QueryAuditReport), 200)]
    public async Task<IActionResult> RunQueryAudit(CancellationToken cancellationToken)
    {
        var report = await this.queryAuditService.RunAuditAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("query-audit/slow-queries")]
    [ProducesResponseType(typeof(IReadOnlyList<SlowQueryEntry>), 200)]
    public async Task<IActionResult> GetSlowQueries([FromQuery] int top = 50, CancellationToken cancellationToken = default)
    {
        var queries = await this.queryAuditService.GetSlowQueriesAsync(top, cancellationToken);
        return Ok(queries);
    }

    [HttpGet("query-audit/missing-indexes")]
    [ProducesResponseType(typeof(IReadOnlyList<MissingIndexEntry>), 200)]
    public async Task<IActionResult> GetMissingIndexes(CancellationToken cancellationToken)
    {
        var indexes = await this.queryAuditService.GetMissingIndexesAsync(cancellationToken);
        return Ok(indexes);
    }

    [HttpGet("query-audit/table-scans")]
    [ProducesResponseType(typeof(IReadOnlyList<TableScanEntry>), 200)]
    public async Task<IActionResult> GetTableScans(CancellationToken cancellationToken)
    {
        var scans = await this.queryAuditService.GetTableScansAsync(cancellationToken);
        return Ok(scans);
    }

    [HttpGet("query-audit/n-plus-one")]
    [ProducesResponseType(typeof(IReadOnlyList<NPlusOneCandidate>), 200)]
    public async Task<IActionResult> GetNPlusOneCandidates(CancellationToken cancellationToken)
    {
        var candidates = await this.queryAuditService.DetectNPlusOneCandidatesAsync(cancellationToken);
        return Ok(candidates);
    }

    // Archival
    [HttpGet("archival/estimate")]
    [ProducesResponseType(typeof(ArchivalEstimate), 200)]
    public async Task<IActionResult> EstimateArchivalSize([FromQuery] int yearsToRetain = 3, CancellationToken cancellationToken = default)
    {
        var estimate = await this.archivalService.EstimateArchivalSizeAsync(yearsToRetain, cancellationToken);
        return Ok(estimate);
    }

    [HttpPost("archival/run")]
    [ProducesResponseType(typeof(ArchivalReport), 200)]
    public async Task<IActionResult> RunArchival([FromQuery] int yearsToRetain = 3, CancellationToken cancellationToken = default)
    {
        var report = await this.archivalService.RunArchivalAsync(yearsToRetain, cancellationToken);
        return Ok(report);
    }

    // Batch Operations
    [HttpGet("batch/stats")]
    [ProducesResponseType(typeof(BatchOperationStats), 200)]
    public async Task<IActionResult> GetBatchStats(CancellationToken cancellationToken)
    {
        var stats = await this.batchOptimizer.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }
}
