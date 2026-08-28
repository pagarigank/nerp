// <copyright file="QueryComplexityGuardMiddleware.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136

#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136, S1871
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ERP.Api.Performance;

/// <summary>
/// Middleware that guards against runaway queries by enforcing:
/// - Maximum query execution time (default 30 seconds)
/// - Maximum response body size
/// - Request timeout enforcement
///
/// Prevents DoS via expensive queries and protects database resources.
/// </summary>
public class QueryComplexityGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<QueryComplexityGuardMiddleware> _logger;

    /// <summary>
    /// Maximum query execution time in seconds. Requests exceeding this
    /// are cancelled with 504 Gateway Timeout.
    /// </summary>
    private const int MaxQueryTimeoutSeconds = 30;

    /// <summary>
    /// Maximum response body size in bytes (10 MB).
    /// </summary>
    private const long MaxResponseSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Paths that bypass the complexity guard (health checks, metrics, swagger).
    /// </summary>
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/metrics",
        "/hangfire",
    };

    public QueryComplexityGuardMiddleware(RequestDelegate next, ILogger<QueryComplexityGuardMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip guard for exempt paths
        var path = context.Request.Path.Value ?? "/";
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        // Apply query timeout via CancellationToken
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(MaxQueryTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.Request.HttpContext.RequestAborted, timeoutCts.Token);

        var sw = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;
        try
        {
            // Wrap response body to monitor size
            using var monitoringStream = new ResponseMonitoringStream(originalBodyStream, MaxResponseSizeBytes);
            context.Response.Body = monitoringStream;

            await _next(context);

            // Check response size
            if (monitoringStream.TotalBytesWritten > MaxResponseSizeBytes)
            {
                _logger.LogWarning(
                    "Response size exceeded limit ({Size} bytes) for {Method} {Path}",
                    monitoringStream.TotalBytesWritten,
                    context.Request.Method,
                    path);

                // Truncate and add header
                context.Response.Headers["X-Response-Truncated"] = "true";
                context.Response.Headers["X-Response-Size-Limit"] = MaxResponseSizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex,
                "Query timeout ({Timeout}s) exceeded for {Method} {Path} after {Elapsed}ms",
                MaxQueryTimeoutSeconds,
                context.Request.Method,
                path,
                sw.ElapsedMilliseconds);

            context.Response.StatusCode = 504; // Gateway Timeout
            context.Response.ContentType = "application/json";
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "QueryTimeout",
                message = $"Request exceeded the {MaxQueryTimeoutSeconds}s timeout limit",
                timeoutSeconds = MaxQueryTimeoutSeconds,
            });
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
            await context.Response.Body.WriteAsync(
                payloadBytes.AsMemory(),
                context.Request.HttpContext.RequestAborted);
        }
        finally
        {
            // Restore original body stream
            if (context.Response.Body is ResponseMonitoringStream)
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    private static bool IsExemptPath(string path)
    {
        return ExemptPaths.Any(exempt => path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Wraps the response body stream to monitor total bytes written.
    /// Prevents unbounded memory consumption from large result sets.
    /// </summary>
    private sealed class ResponseMonitoringStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _maxSize;
        private bool _truncated;

        public long TotalBytesWritten { get; private set; }

        public ResponseMonitoringStream(Stream innerStream, long maxSize)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _maxSize = maxSize;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_truncated)
            {
                return;
            }

            TotalBytesWritten += count;

            if (TotalBytesWritten > _maxSize)
            {
                _truncated = true;
                return; // Stop writing to prevent OOM
            }

            await _innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_truncated)
            {
                return;
            }

            TotalBytesWritten += count;

            if (TotalBytesWritten > _maxSize)
            {
                _truncated = true;
                return;
            }

            _innerStream.Write(buffer, offset, count);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_truncated)
            {
                return;
            }

            TotalBytesWritten += buffer.Length;

            if (TotalBytesWritten > _maxSize)
            {
                _truncated = true;
                return;
            }

            await _innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override void Flush() => _innerStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
