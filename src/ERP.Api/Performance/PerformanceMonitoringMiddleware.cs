// <copyright file="PerformanceMonitoringMiddleware.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136

#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136, S1871
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace ERP.Api.Performance;

/// <summary>
/// Middleware that instruments every HTTP request with timing metrics.
/// Tracks p50/p95/p99 latency, request counts, error rates, and
/// per-endpoint breakdowns. Exposes metrics via GET /metrics.
///
/// Target: &lt;2s response time for 95th percentile (spec §7).
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, EndpointMetrics> _endpointMetrics = new();
    private static readonly ConcurrentQueue<double> _recentDurations = new();
    private const int MaxRecentSamples = 10000;
    private static readonly JsonSerializerOptions MetricsJsonOptions = new() { WriteIndented = true };

    public PerformanceMonitoringMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Serve metrics endpoint directly
        if (context.Request.Path == "/metrics" && context.Request.Method == "GET")
        {
            await WriteMetricsResponseAsync(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var durationMs = sw.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            RecordMetrics(path, method, statusCode, durationMs);

            // Add performance headers only if the response has not already started.
            // On error paths (4xx/5xx) the downstream pipeline may have begun writing
            // the response, in which case mutating headers throws "Headers are read-only,
            // response has already started" and masks the real error with a 500.
            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-Response-Time-Ms"] = durationMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                context.Response.Headers["X-Request-Id"] = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
            }
        }
    }

    private static void RecordMetrics(string path, string method, int statusCode, double durationMs)
    {
        // Normalize path: strip IDs and query strings for grouping
        var normalizedPath = NormalizePath(path);
        var key = $"{method} {normalizedPath}";

        _endpointMetrics.AddOrUpdate(
            key,
            _ => new EndpointMetrics
            {
                Method = method,
                Path = normalizedPath,
                Count = 1,
                TotalDurationMs = durationMs,
                MinDurationMs = durationMs,
                MaxDurationMs = durationMs,
                ErrorCount = statusCode >= 400 ? 1 : 0,
                LastRequestOn = DateTimeOffset.UtcNow,
            },
            (_, existing) =>
            {
                existing.Count++;
                existing.TotalDurationMs += durationMs;
                existing.MinDurationMs = Math.Min(existing.MinDurationMs, durationMs);
                existing.MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs);
                if (statusCode >= 400)
                {
                    existing.ErrorCount++;
                }

                existing.LastRequestOn = DateTimeOffset.UtcNow;
                return existing;
            });

        // Track for percentile calculations
        _recentDurations.Enqueue(durationMs);
        while (_recentDurations.Count > MaxRecentSamples)
        {
            _recentDurations.TryDequeue(out _);
        }
    }

    private static string NormalizePath(string path)
    {
        // Replace GUID segments with {id}
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            if (Guid.TryParse(segments[i], out _) || int.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
        }

        return "/" + string.Join("/", segments);
    }

    private static async Task WriteMetricsResponseAsync(HttpContext context)
    {
        var sortedDurations = _recentDurations.ToArray();
        Array.Sort(sortedDurations);

        var totalCount = sortedDurations.Length;
        var p50 = GetPercentile(sortedDurations, 0.50);
        var p95 = GetPercentile(sortedDurations, 0.95);
        var p99 = GetPercentile(sortedDurations, 0.99);
        var avg = totalCount > 0 ? sortedDurations.Average() : 0;

        var topEndpoints = _endpointMetrics.Values
            .OrderByDescending(m => m.Count)
            .Take(50)
            .Select(m => new
            {
                endpoint = $"{m.Method} {m.Path}",
                count = m.Count,
                avgMs = m.Count > 0 ? Math.Round(m.TotalDurationMs / m.Count, 2) : 0,
                minMs = Math.Round(m.MinDurationMs, 2),
                maxMs = Math.Round(m.MaxDurationMs, 2),
                errorCount = m.ErrorCount,
                errorRate = m.Count > 0 ? Math.Round((double)m.ErrorCount / m.Count * 100, 2) : 0,
            })
            .ToList();

        var totalErrors = _endpointMetrics.Values.Sum(m => m.ErrorCount);
        var totalRequests = _endpointMetrics.Values.Sum(m => m.Count);

        var response = new
        {
            summary = new
            {
                totalRequests,
                totalErrors,
                errorRate = totalRequests > 0 ? Math.Round((double)totalErrors / totalRequests * 100, 2) : 0,
                sampleCount = totalCount,
                avgMs = Math.Round(avg, 2),
                p50Ms = Math.Round(p50, 2),
                p95Ms = Math.Round(p95, 2),
                p99Ms = Math.Round(p99, 2),
                maxMs = totalCount > 0 ? Math.Round(sortedDurations[^1], 2) : 0,
            },
            endpoints = topEndpoints,
            checkedOn = DateTimeOffset.UtcNow,
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, MetricsJsonOptions));
    }

    private static double GetPercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Length - 1))];
    }

    private sealed class EndpointMetrics
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Count { get; set; }
        public double TotalDurationMs { get; set; }
        public double MinDurationMs { get; set; }
        public double MaxDurationMs { get; set; }
        public long ErrorCount { get; set; }
        public DateTimeOffset LastRequestOn { get; set; }
    }
}
