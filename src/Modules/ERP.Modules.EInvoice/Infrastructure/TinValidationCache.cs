// <copyright file="TinValidationCache.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Caches TIN validation results from MyInvois API.
/// Reduces API calls by storing validated TINs with a configurable TTL.
/// </summary>
public interface ITinValidationCache
{
    /// <summary>
    /// Gets a cached TIN validation result, or null if not cached / expired.
    /// </summary>
    TinValidationResult? Get(string tin);

    /// <summary>
    /// Stores a TIN validation result in the cache.
    /// </summary>
    void Set(string tin, TinValidationResult result);

    /// <summary>
    /// Gets all cached TINs for cache refresh purposes.
    /// </summary>
    IReadOnlyList<string> GetAllCachedTins();

    /// <summary>
    /// Removes a specific TIN from the cache.
    /// </summary>
    void Remove(string tin);

    /// <summary>
    /// Clears all cached TIN validation results.
    /// </summary>
    void Clear();
}

public class TinValidationCache : ITinValidationCache
{
    /// <summary>Cache key prefix for TIN validation entries.</summary>
    private const string CacheKeyPrefix = "tin:validation:";

    /// <summary>Cache key for the tracking set of all cached TINs.</summary>
    private const string TinsSetKey = "tin:validation:_all_tins";

    /// <summary>Default cache duration for TIN validation results.</summary>
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(24);

    private readonly IMemoryCache _cache;
    private readonly ILogger<TinValidationCache> _logger;

    public TinValidationCache(
        IMemoryCache cache,
        ILogger<TinValidationCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public TinValidationResult? Get(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
        {
            return null;
        }

        var key = CacheKeyPrefix + NormalizeTin(tin);
        if (_cache.TryGetValue<TinValidationResult>(key, out var cached))
        {
            _logger.LogDebug("TIN cache hit: {TIN}", tin);
            return cached;
        }

        _logger.LogDebug("TIN cache miss: {TIN}", tin);
        return null;
    }

    /// <inheritdoc />
    public void Set(string tin, TinValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(tin) || result == null)
        {
            return;
        }

        var key = CacheKeyPrefix + NormalizeTin(tin);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DefaultCacheDuration)
            .SetSize(1);

        _cache.Set(key, result, cacheOptions);
        TrackTin(tin);

        _logger.LogDebug(
            "TIN cached: {TIN}, valid={IsValid}",
            tin,
            result.IsValid);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllCachedTins()
    {
        if (_cache.TryGetValue<HashSet<string>>(TinsSetKey, out var tins) && tins != null)
        {
            return tins.ToList().AsReadOnly();
        }

        return Array.Empty<string>();
    }

    /// <inheritdoc />
    public void Remove(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
        {
            return;
        }

        var key = CacheKeyPrefix + NormalizeTin(tin);
        _cache.Remove(key);

        if (_cache.TryGetValue<HashSet<string>>(TinsSetKey, out var tins) && tins != null)
        {
            tins.Remove(NormalizeTin(tin));
        }

        _logger.LogDebug("TIN removed from cache: {TIN}", tin);
    }

    /// <inheritdoc />
    public void Clear()
    {
        var allTins = GetAllCachedTins();
        foreach (var tin in allTins)
        {
            _cache.Remove(CacheKeyPrefix + tin);
        }

        _cache.Remove(TinsSetKey);
        _logger.LogInformation("TIN cache cleared: {Count} entries removed", allTins.Count);
    }

    private static string NormalizeTin(string tin)
    {
        return tin.Trim().ToUpperInvariant();
    }

    private void TrackTin(string tin)
    {
        var normalized = NormalizeTin(tin);
        var tins = _cache.GetOrCreate(TinsSetKey, entry =>
        {
            entry.SetSize(1);
            entry.SetSlidingExpiration(TimeSpan.FromHours(48));
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        });

        tins?.Add(normalized);
    }
}
