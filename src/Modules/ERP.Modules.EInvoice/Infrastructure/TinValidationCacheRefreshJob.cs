// <copyright file="TinValidationCacheRefreshJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Hangfire background job that refreshes cached TIN validation results.
/// Runs daily to re-validate all TINs currently in the cache against the MyInvois API.
/// </summary>
public class TinValidationCacheRefreshJob
{
    /// <summary>Maximum number of TINs to refresh per run to avoid API rate limits.</summary>
    private const int MaxRefreshPerRun = 200;

    private readonly ITinValidationCache _cache;
    private readonly ITaxpayerProfileService _taxpayerProfileService;
    private readonly ILogger<TinValidationCacheRefreshJob> _logger;

    public TinValidationCacheRefreshJob(
        ITinValidationCache cache,
        ITaxpayerProfileService taxpayerProfileService,
        ILogger<TinValidationCacheRefreshJob> logger)
    {
        _cache = cache;
        _taxpayerProfileService = taxpayerProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Refreshes all cached TINs by re-validating them against the MyInvois API.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cachedTins = _cache.GetAllCachedTins();

        if (cachedTins.Count == 0)
        {
            _logger.LogInformation("No TINs in cache to refresh.");
            return;
        }

        _logger.LogInformation(
            "TIN cache refresh started. {Count} TINs to validate.",
            cachedTins.Count);

        var refreshed = 0;
        var failed = 0;
        var removed = 0;

        foreach (var tin in cachedTins.Take(MaxRefreshPerRun))
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var result = await _taxpayerProfileService.ValidateTinAsync(tin);

                if (result.Success)
                {
                    if (result.IsValid)
                    {
                        _cache.Set(tin, result);
                        refreshed++;
                    }
                    else
                    {
                        _cache.Remove(tin);
                        removed++;

                        _logger.LogWarning(
                            "TIN {TIN} is no longer valid ({EntityName}). Removed from cache.",
                            tin,
                            result.EntityName);
                    }
                }
                else
                {
                    failed++;

                    _logger.LogWarning(
                        "TIN {TIN} refresh failed: {Error}",
                        tin,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Unexpected error refreshing TIN {TIN}", tin);
            }
        }

        _logger.LogInformation(
            "TIN cache refresh completed. Refreshed: {Refreshed}, Removed (invalid): {Removed}, Failed: {Failed}",
            refreshed,
            removed,
            failed);
    }
}
