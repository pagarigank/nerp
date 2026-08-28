// <copyright file="HttpCachingMiddleware.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, S1118
#pragma warning disable SA1204, S6580, S1066

using System.Globalization;
using System.Security.Cryptography;

namespace ERP.Api.Performance;

/// <summary>
/// Adds HTTP caching headers (ETag, Last-Modified, Cache-Control) to GET responses.
/// Supports conditional requests (If-None-Match, If-Modified-Since) for 304 responses.
/// </summary>
public sealed class HttpCachingMiddleware
{
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/platform/audit-logs",
        "/api/v1/platform/notifications",
        "/api/v1/platform/users",
        "/api/v1/platform/roles",
        "/api/v1/platform/api-keys",
        "/api/v1/platform/auth",
        "/api/v1/platform/account",
        "/api/v1/performance",
        "/metrics",
        "/health",
        "/health/ready",
        "/health/live",
        "/hangfire",
        "/swagger",
    };

    private static readonly Dictionary<string, string> CacheControlByPath = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/api/v1/platform/companies"] = "public, max-age=3600",
        ["/api/v1/platform/segment-types"] = "public, max-age=3600",
        ["/api/v1/platform/segment-values"] = "public, max-age=3600",
        ["/api/v1/platform/fiscal-years"] = "public, max-age=3600",
        ["/api/v1/platform/fiscal-periods"] = "public, max-age=3600",
        ["/api/v1/platform/currencies"] = "public, max-age=3600",
        ["/api/v1/platform/exchange-rates"] = "public, max-age=1800",
        ["/api/v1/platform/holiday-calendars"] = "public, max-age=3600",
        ["/api/v1/platform/number-sequences"] = "public, max-age=3600",
        ["/api/v1/ap/vendors"] = "private, max-age=300",
        ["/api/v1/ar/customers"] = "private, max-age=300",
        ["/api/v1/inventory/items"] = "private, max-age=300",
        ["/api/v1/gl/accounts"] = "private, max-age=300",
        ["/api/v1/payroll/employees"] = "private, max-age=300",
        ["/api/v1/gl/journal-batches"] = "no-cache, no-store",
        ["/api/v1/ap/vouchers"] = "no-cache, no-store",
        ["/api/v1/ar/invoices"] = "no-cache, no-store",
        ["/api/v1/cash/bank-accounts"] = "private, max-age=60",
        ["/api/v1/pur/requisitions"] = "private, max-age=60",
        ["/api/v1/pur/purchase-orders"] = "private, max-age=60",
    };

    private readonly RequestDelegate next;

    public HttpCachingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await this.next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (IsExemptPath(path))
        {
            await this.next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await this.next(context);

        if (context.Response.StatusCode is < 200 or >= 300)
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
            return;
        }

        buffer.Position = 0;
        var bodyBytes = await ReadAllBytesAsync(buffer);

        var etag = ComputeETag(bodyBytes);
        context.Response.Headers["ETag"] = etag;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Cache-Control"] = GetCacheControl(path);

        if (!context.Response.Headers.ContainsKey("Last-Modified"))
        {
            context.Response.Headers["Last-Modified"] = DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture);
        }

        // Check If-None-Match (ETag conditional request)
        var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch))
        {
            var clientEtag = ifNoneMatch.Trim('"');
            var serverEtag = etag.Trim('"');
            if (string.Equals(clientEtag, serverEtag, StringComparison.Ordinal))
            {
                context.Response.StatusCode = 304;
                context.Response.Body = originalBody;
                return;
            }
        }

        // Check If-Modified-Since
        if (context.Request.Headers.TryGetValue("If-Modified-Since", out var ifModifiedSinceValues)
            && ifModifiedSinceValues.Count > 0
            && DateTimeOffset.TryParse(ifModifiedSinceValues.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var ifModifiedSince))
        {
            var lastModified = DateTimeOffset.UtcNow;
            if (DateTimeOffset.TryParse(context.Response.Headers["Last-Modified"].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                lastModified = parsed;
            }

            if (ifModifiedSince >= lastModified)
            {
                context.Response.StatusCode = 304;
                context.Response.Body = originalBody;
                return;
            }
        }

        buffer.Position = 0;
        context.Response.ContentLength = bodyBytes.Length;
        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }

    private static bool IsExemptPath(string path)
    {
        return ExemptPaths.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetCacheControl(string path)
    {
        var best = CacheControlByPath
            .Where(kvp => path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kvp => kvp.Key.Length)
            .FirstOrDefault();

        return string.IsNullOrEmpty(best.Value) ? "private, max-age=60" : best.Value;
    }

    private static string ComputeETag(byte[] content)
    {
        var hash = SHA256.HashData(content);
        var base64 = Convert.ToBase64String(hash);
        return $"\"{base64}\"";
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
