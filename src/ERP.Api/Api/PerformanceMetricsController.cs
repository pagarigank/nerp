// <copyright file="PerformanceMetricsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600

using ERP.Api.Performance;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Api;

[ApiController]
[Route("api/v1/performance/cache")]
public class CacheMetricsController : ControllerBase
{
    private readonly IDistributedCacheService _cacheService;

    public CacheMetricsController(IDistributedCacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Returns cache hit/miss statistics for monitoring.
    /// Target: >90% hit rate for lookups.
    /// </summary>
    [HttpGet("stats")]
    public ActionResult<ApiResponse<CacheHitStats>> GetCacheStats()
    {
        var stats = _cacheService.GetStats();
        return Ok(ApiResponse<CacheHitStats>.Success(stats));
    }

    /// <summary>
    /// Resets cache statistics counters.
    /// </summary>
    [HttpPost("stats/reset")]
    public ActionResult<ApiResponse> ResetCacheStats()
    {
        _cacheService.ResetStats();
        return Ok(ApiResponse.Success("Cache statistics reset"));
    }
}

[ApiController]
[Route("api/v1/performance/database")]
public class DatabaseMetricsController : ControllerBase
{
    private readonly IDatabaseIndexOptimizer _indexOptimizer;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseMetricsController(
        IDatabaseIndexOptimizer indexOptimizer,
        IServiceProvider serviceProvider)
    {
        _indexOptimizer = indexOptimizer ?? throw new ArgumentNullException(nameof(indexOptimizer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Creates all performance-critical composite indexes. Idempotent.
    /// </summary>
    [HttpPost("indexes/create")]
    public async Task<ActionResult<ApiResponse<int>>> CreatePerformanceIndexes(
        CancellationToken cancellationToken)
    {
        var db = _serviceProvider.GetRequiredService<ERP.Modules.GeneralLedger.Infrastructure.GlDbContext>();
        var count = await _indexOptimizer.CreatePerformanceIndexesAsync(db, cancellationToken);
        return Ok(ApiResponse<int>.Success(count, $"Created {count} performance indexes"));
    }

    /// <summary>
    /// Returns index usage statistics across all schemas.
    /// </summary>
    [HttpGet("indexes/stats")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IndexStats>>>> GetIndexStats(
        CancellationToken cancellationToken)
    {
        var db = _serviceProvider.GetRequiredService<ERP.Modules.GeneralLedger.Infrastructure.GlDbContext>();
        var stats = await _indexOptimizer.GetIndexStatsAsync(db, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<IndexStats>>.Success(stats));
    }

    /// <summary>
    /// Detects missing indexes based on query plan analysis.
    /// </summary>
    [HttpGet("indexes/missing")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MissingIndex>>>> DetectMissingIndexes(
        CancellationToken cancellationToken)
    {
        var db = _serviceProvider.GetRequiredService<ERP.Modules.GeneralLedger.Infrastructure.GlDbContext>();
        var missing = await _indexOptimizer.DetectMissingIndexesAsync(db, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MissingIndex>>.Success(missing));
    }

    /// <summary>
    /// Returns index fragmentation statistics for maintenance planning.
    /// </summary>
    [HttpGet("indexes/fragmentation")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IndexFragmentation>>>> GetFragmentation(
        CancellationToken cancellationToken)
    {
        var db = _serviceProvider.GetRequiredService<ERP.Modules.GeneralLedger.Infrastructure.GlDbContext>();
        var frag = await _indexOptimizer.GetFragmentationAsync(db, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<IndexFragmentation>>.Success(frag));
    }

    /// <summary>
    /// Updates table statistics for the query optimizer.
    /// </summary>
    [HttpPost("statistics/update")]
    public async Task<ActionResult<ApiResponse>> UpdateStatistics(
        [FromQuery] string? schema = null,
        CancellationToken cancellationToken = default)
    {
        var db = _serviceProvider.GetRequiredService<ERP.Modules.GeneralLedger.Infrastructure.GlDbContext>();
        await _indexOptimizer.UpdateStatisticsAsync(db, schema, cancellationToken);
        return Ok(ApiResponse.Success($"Statistics updated{(string.IsNullOrEmpty(schema) ? " for all schemas" : $" for schema {schema}")}"));
    }
}
