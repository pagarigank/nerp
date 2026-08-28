// <copyright file="DeadlockRetryMiddleware.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136

#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136, S1871
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ERP.Api.Performance;

/// <summary>
/// Middleware that catches SQL Server deadlock exceptions (error 1205)
/// and retries the request with exponential backoff. This prevents
/// transient deadlock failures from reaching the client.
///
/// Retry schedule: 100ms → 200ms → 400ms → 800ms → 1600ms (5 attempts max)
/// </summary>
public class DeadlockRetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeadlockRetryMiddleware> _logger;

    /// <summary>
    /// SQL Server deadlock error number.
    /// </summary>
    private const int DeadlockErrorNumber = 1205;

    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    private const int MaxRetries = 5;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff.
    /// </summary>
    private const int BaseDelayMs = 100;

    public DeadlockRetryMiddleware(RequestDelegate next, ILogger<DeadlockRetryMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // Reset the response body for retry attempts
                if (attempt > 0)
                {
                    context.Response.StatusCode = 200;
                    context.Response.Body = new MemoryStream();
                }

                await _next(context);
                return; // Success
            }
            catch (Exception ex) when (IsDeadlock(ex) && attempt < MaxRetries)
            {
                var delayMs = BaseDelayMs * (int)Math.Pow(2, attempt);

                _logger.LogWarning(ex, "Deadlock detected on attempt {Attempt}/{MaxRetries} for {Method} {Path}. Retrying in {Delay}ms", attempt + 1, MaxRetries, context.Request.Method, context.Request.Path, delayMs);

                // Wait before retrying
                await Task.Delay(delayMs, context.Request.HttpContext.RequestAborted);

                // Reset response for retry
                if (context.Response.Body is MemoryStream ms)
                {
                    ms.SetLength(0);
                    ms.Position = 0;
                }
            }
            catch (Exception ex) when (IsDeadlock(ex) && attempt >= MaxRetries)
            {
                _logger.LogError(ex,
                    "Deadlock retry exhausted after {MaxRetries} attempts for {Method} {Path}",
                    MaxRetries,
                    context.Request.Method,
                    context.Request.Path);
                throw; // Let global exception handler deal with it
            }
        }
    }

    private static bool IsDeadlock(Exception ex)
    {
        // Check for SqlException with deadlock error number
        if (ex is SqlException sqlEx && sqlEx.Number == DeadlockErrorNumber)
        {
            return true;
        }

        // Check inner exceptions (EF Core wraps SqlException)
        if (ex.InnerException is SqlException innerSql && innerSql.Number == DeadlockErrorNumber)
        {
            return true;
        }

        // Check for DbUpdateConcurrencyException which can result from deadlocks
        if (ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return true;
        }

        return false;
    }
}
