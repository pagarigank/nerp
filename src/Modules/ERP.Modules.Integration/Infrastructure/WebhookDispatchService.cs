// <copyright file="WebhookDispatchService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Integration.Infrastructure;

/// <summary>
/// Dispatches webhook notifications with HMAC-SHA256 signature verification,
/// exponential backoff retry, and delivery logging.
/// </summary>
public class WebhookDispatchService
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDispatchService> _logger;

    private static readonly TimeSpan[] BackoffSchedule =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2),
    ];

    public WebhookDispatchService(
        IntegrationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatchService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Dispatch a webhook event to all matching subscribers.
    /// </summary>
    public async Task DispatchEventAsync(string eventType, object payload, Guid companyId, CancellationToken ct = default)
    {
        var subscribers = await _dbContext.WebhookSubscriptions
            .Where(s => s.IsActive && s.EventType == eventType && s.CompanyId == companyId)
            .ToListAsync(ct);

        if (subscribers.Count == 0)
        {
            _logger.LogDebug("No webhook subscribers for event {EventType} company {CompanyId}", eventType, companyId);
            return;
        }

        _logger.LogInformation("Dispatching {EventType} to {Count} subscribers", eventType, subscribers.Count);

        foreach (var subscriber in subscribers)
        {
            _ = Task.Run(() => DispatchToSubscriberAsync(subscriber, eventType, payload, ct), ct);
        }
    }

    private async Task DispatchToSubscriberAsync(
        WebhookSubscription subscriber,
        string eventType,
        object payload,
        CancellationToken ct)
    {
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var signature = ComputeHmacSignature(payloadJson, subscriber.Secret);

        // Use the entity constructor (respects private setters)
        var deliveryLog = new WebhookDeliveryLog(
            subscriber.Id,
            eventType,
            subscriber.TargetUrl,
            payloadJson,
            attemptNumber: 0);

        _dbContext.WebhookDeliveryLogs.Add(deliveryLog);
        await _dbContext.SaveChangesAsync(ct);

        var success = await AttemptDeliveryAsync(deliveryLog, signature, ct);

        if (!success)
        {
            deliveryLog.MarkFailed(0, "Max retries exceeded", 0);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Webhook delivery failed after retries: {EventType} → {TargetUrl}",
                eventType, subscriber.TargetUrl);
        }
    }

    private async Task<bool> AttemptDeliveryAsync(
        WebhookDeliveryLog deliveryLog,
        string signature,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("WebhookClient");
        client.Timeout = TimeSpan.FromSeconds(30);

        for (int attempt = 0; attempt < BackoffSchedule.Length; attempt++)
        {
            deliveryLog.AttemptNumber = attempt + 1;
            deliveryLog.MarkPendingRetry();
            await _dbContext.SaveChangesAsync(ct);

            if (attempt > 0)
            {
                var delay = BackoffSchedule[Math.Min(attempt - 1, BackoffSchedule.Length - 1)];
                _logger.LogInformation(
                    "Webhook retry {Attempt}/{Max} after {Delay}s: {EventType} → {TargetUrl}",
                    attempt + 1, BackoffSchedule.Length, delay.TotalSeconds,
                    deliveryLog.EventType, deliveryLog.TargetUrl);

                await Task.Delay(delay, ct);
            }

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var request = new HttpRequestMessage(HttpMethod.Post, deliveryLog.TargetUrl);
                request.Content = new StringContent(deliveryLog.Payload, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Webhook-Event", deliveryLog.EventType);
                request.Headers.Add("X-Webhook-Delivery-Id", deliveryLog.Id.ToString());
                request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                request.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                request.Headers.Add("User-Agent", "ERP-Webhook/1.0");

                var response = await client.SendAsync(request, ct);
                sw.Stop();
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    deliveryLog.MarkSuccess((int)response.StatusCode, responseBody, sw.ElapsedMilliseconds);
                    await _dbContext.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "Webhook delivered: {EventType} → {TargetUrl} ({StatusCode})",
                        deliveryLog.EventType, deliveryLog.TargetUrl, response.StatusCode);
                    return true;
                }

                deliveryLog.MarkFailed((int)response.StatusCode, responseBody, sw.ElapsedMilliseconds);
                _logger.LogWarning(
                    "Webhook delivery returned {StatusCode}: {EventType} → {TargetUrl}",
                    response.StatusCode, deliveryLog.EventType, deliveryLog.TargetUrl);
            }
            catch (Exception ex)
            {
                deliveryLog.MarkFailed(0, ex.Message, 0);
                _logger.LogWarning(ex,
                    "Webhook delivery exception: {EventType} → {TargetUrl}",
                    deliveryLog.EventType, deliveryLog.TargetUrl);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return false;
    }

    /// <summary>
    /// Manually retry a failed webhook delivery.
    /// </summary>
    public async Task<bool> RetryDeliveryAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var delivery = await _dbContext.WebhookDeliveryLogs.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null) return false;
        if (delivery.Status == "Success") return true;

        delivery.MarkPendingRetry();
        delivery.AttemptNumber = 0;
        await _dbContext.SaveChangesAsync(ct);

        var signature = ComputeHmacSignature(delivery.Payload, string.Empty);
        return await AttemptDeliveryAsync(delivery, signature, ct);
    }

    /// <summary>
    /// Verify an incoming webhook signature (for inbound webhook processing).
    /// </summary>
    public static bool VerifySignature(string payload, string providedSignature, string secret)
    {
        var expectedSignature = ComputeHmacSignature(payload, secret);
        return string.Equals(providedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHmacSignature(string payload, string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
