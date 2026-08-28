// <copyright file="ReportOutputCacheService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Modules.Reporting.Services;

/// <summary>
/// Provides parameter-keyed output caching for report results. Each unique
/// combination of report ID + parameters produces a deterministic cache key.
/// Entries expire after a configurable TTL (default 15 minutes). Cache is
/// invalidated when the reporting data-mart is refreshed (by invalidation
/// signal or TTL expiry).
/// </summary>
public interface IReportOutputCacheService
{
    /// <summary>
    /// Attempts to retrieve a cached report result for the given report and parameters.
    /// Returns null if no cached entry exists or if the entry has expired.
    /// </summary>
    CachedReportResult? Get(string reportId, string? parametersJson);

    /// <summary>
    /// Stores a report result in the cache with the given TTL.
    /// </summary>
    void Set(string reportId, string? parametersJson, CachedReportResult result, TimeSpan? ttl = null);

    /// <summary>
    /// Invalidates all cached entries for a specific report (e.g., when data changes).
    /// </summary>
    void InvalidateReport(string reportId);

    /// <summary>
    /// Invalidates all cached entries (e.g., after a full data-mart refresh).
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Returns the number of active cached entries (for monitoring/diagnostics).
    /// </summary>
    int GetCacheSize();

    /// <summary>
    /// Returns cache statistics (hits, misses, evictions) for monitoring.
    /// </summary>
    CacheStatistics GetStatistics();
}

public class CachedReportResult
{
    public string ReportId { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];
    public int TotalCount { get; set; }
    public DateTimeOffset GeneratedOn { get; set; }
    public string? ExportUrl { get; set; }
}

public class CacheStatistics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Evictions { get; set; }
    public int EntryCount { get; set; }
    public double HitRate => Hits + Misses > 0
        ? (double)Hits / (Hits + Misses) * 100
        : 0;
}

public class ReportOutputCacheService : IReportOutputCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl;
    private long _hits;
    private long _misses;
    private long _evictions;

    public ReportOutputCacheService(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(15);
    }

    public CachedReportResult? Get(string reportId, string? parametersJson)
    {
        var key = ComputeCacheKey(reportId, parametersJson);

        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
    {
            Interlocked.Increment(ref _hits);
            entry.LastAccessedOn = DateTimeOffset.UtcNow;
            return entry.Result;
        }

        Interlocked.Increment(ref _misses);

        // Clean up expired entry if it exists
        if (entry != null)
        {
            _cache.TryRemove(key, out _);
            Interlocked.Increment(ref _evictions);
        }

        return null;
    }

    public void Set(string reportId, string? parametersJson, CachedReportResult result, TimeSpan? ttl = null)
    {
        var key = ComputeCacheKey(reportId, parametersJson);
        var effectiveTtl = ttl ?? _defaultTtl;

        var entry = new CacheEntry
        {
            Result = result,
            ExpiresOn = DateTimeOffset.UtcNow.Add(effectiveTtl),
            LastAccessedOn = DateTimeOffset.UtcNow,
        };

        _cache[key] = entry;
    }

    public void InvalidateReport(string reportId)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.StartsWith(reportId + ":", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
            Interlocked.Increment(ref _evictions);
        }
    }

    public void InvalidateAll()
    {
        _cache.Clear();
        Interlocked.Increment(ref _evictions);
    }

    public int GetCacheSize() => _cache.Count;

    public CacheStatistics GetStatistics()
    {
        // Trigger cleanup of expired entries
        CleanupExpiredEntries();

        return new CacheStatistics
        {
            Hits = Interlocked.Read(ref _hits),
            Misses = Interlocked.Read(ref _misses),
            Evictions = Interlocked.Read(ref _evictions),
            EntryCount = _cache.Count,
        };
    }

    private void CleanupExpiredEntries()
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Computes a deterministic cache key from report ID + parameters.
    /// Uses SHA-256 of the parameters JSON to keep keys a fixed length.
    /// </summary>
    private static string ComputeCacheKey(string reportId, string? parametersJson)
    {
        if (string.IsNullOrEmpty(parametersJson))
        {
            return reportId;
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(parametersJson));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return $"{reportId}:{hashString}";
    }

    private class CacheEntry
    {
        public CachedReportResult Result { get; set; } = new();
        public DateTimeOffset ExpiresOn { get; set; }
        public DateTimeOffset LastAccessedOn { get; set; }
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresOn;
    }
}
