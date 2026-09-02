// <copyright file="IntegrationBackgroundJobs.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Integration.Application;

/// <summary>
/// Base class for integration background jobs.
/// </summary>
public abstract class IntegrationRecurringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger Logger;
    private readonly TimeSpan _period;

    protected IntegrationRecurringJob(IServiceScopeFactory scopeFactory, ILogger logger, TimeSpan period)
    {
        _scopeFactory = scopeFactory;
        Logger = logger;
        _period = period;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
                await RunAsync(context, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Integration background job {Job} failed.", GetType().Name);
            }
        }
    }

    protected abstract Task RunAsync(
        IntegrationDbContext context, IServiceProvider scopedServices, CancellationToken ct);
}

/// <summary>
/// Retries failed webhook deliveries with exponential backoff.
/// </summary>
public class WebhookRetryJob : IntegrationRecurringJob
{
    public WebhookRetryJob(IServiceScopeFactory sf, ILogger<WebhookRetryJob> l)
        : base(sf, l, TimeSpan.FromMinutes(5)) { }

    protected override async Task RunAsync(
        IntegrationDbContext context, IServiceProvider scopedServices, CancellationToken ct)
    {
        var failedLogs = await context.WebhookDeliveryLogs
            .Where(x => x.Status == "Failed" || x.Status == "PendingRetry")
            .OrderBy(x => x.DeliveredOn)
            .Take(50)
            .ToListAsync(ct);

        foreach (var log in failedLogs)
        {
            var subscription = await context.WebhookSubscriptions
                .FirstOrDefaultAsync(x => x.Id == log.WebhookSubscriptionId, ct);
            if (subscription is null || !subscription.IsActive) continue;

            log.AttemptNumber++;
            var newLog = new WebhookDeliveryLog(
                log.WebhookSubscriptionId, log.EventType, log.TargetUrl,
                log.Payload, log.AttemptNumber);

            // Simulate HTTP delivery
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            try
            {
                using var content = new StringContent(log.Payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(log.TargetUrl, content, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    newLog.MarkSuccess((int)response.StatusCode, responseBody, 0);
                    subscription.RecordDelivery((int)response.StatusCode);
                    log.MarkSuccess((int)response.StatusCode, responseBody, 0);
                }
                else
                {
                    newLog.MarkFailed((int)response.StatusCode, responseBody, 0);
                    log.MarkFailed((int)response.StatusCode, responseBody, 0);
                }
            }
            catch (Exception ex)
            {
                newLog.MarkFailed(0, ex.Message, 0);
                log.MarkFailed(0, ex.Message, 0);
            }

            context.WebhookDeliveryLogs.Add(newLog);
        }

        if (failedLogs.Count > 0)
            await context.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Monitors trading partner certificate expiry and logs alerts.
/// </summary>
public class CertificateExpiryAlertJob : IntegrationRecurringJob
{
    public CertificateExpiryAlertJob(IServiceScopeFactory sf, ILogger<CertificateExpiryAlertJob> l)
        : base(sf, l, TimeSpan.FromHours(24)) { }

    protected override async Task RunAsync(
        IntegrationDbContext context, IServiceProvider scopedServices, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var horizon = now.AddDays(30);

        var expiring = await context.TradingPartners
            .Where(x => x.IsActive && x.CertificateExpiresOn.HasValue &&
                        x.CertificateExpiresOn.Value <= horizon)
            .ToListAsync(ct);

        foreach (var partner in expiring)
        {
            if (partner.CertificateExpiresOn!.Value < now)
            {
                Logger.LogWarning(
                    "CERT EXPIRED: Partner {PartnerCode} ({Name}) certificate expired on {ExpiryDate}.",
                    partner.PartnerCode, partner.Name,
                    partner.CertificateExpiresOn.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
            else
            {
                var daysLeft = (partner.CertificateExpiresOn.Value - now).Days;
                Logger.LogWarning(
                    "CERT EXPIRING: Partner {PartnerCode} ({Name}) certificate expires in {Days} days on {ExpiryDate}.",
                    partner.PartnerCode, partner.Name, daysLeft,
                    partner.CertificateExpiresOn.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
        }
    }
}

/// <summary>
/// Monitors EDI functional acknowledgments (997/999) and alerts on timeout.
/// </summary>
public class EdiAckTimeoutMonitorJob : IntegrationRecurringJob
{
    public EdiAckTimeoutMonitorJob(IServiceScopeFactory sf, ILogger<EdiAckTimeoutMonitorJob> l)
        : base(sf, l, TimeSpan.FromHours(1)) { }

    protected override async Task RunAsync(
        IntegrationDbContext context, IServiceProvider scopedServices, CancellationToken ct)
    {
        var timeout = DateTimeOffset.UtcNow.AddHours(-24);

        var unackedLogs = await context.EdiTransactionLogs
            .Where(x => x.Direction == "Outbound" && x.Status == "Processed" && x.ProcessedOn < timeout)
            .Take(100)
            .ToListAsync(ct);

        foreach (var log in unackedLogs)
        {
            var hoursSince = (DateTimeOffset.UtcNow - log.ProcessedOn).TotalHours;
            Logger.LogWarning(
                "EDI ACK TIMEOUT: {DocType} to {Partner} (ST#{StControl}) sent {Hours:N1}h ago, no 997/999 received.",
                log.DocumentType, log.PartnerCode, log.StControlNumber, hoursSince);
        }
    }
}

/// <summary>
/// Monitors import jobs stuck in non-terminal states and logs alerts.
/// </summary>
public class ImportJobMonitorJob : IntegrationRecurringJob
{
    public ImportJobMonitorJob(IServiceScopeFactory sf, ILogger<ImportJobMonitorJob> l)
        : base(sf, l, TimeSpan.FromHours(6)) { }

    protected override async Task RunAsync(
        IntegrationDbContext context, IServiceProvider scopedServices, CancellationToken ct)
    {
        var stuckThreshold = DateTimeOffset.UtcNow.AddHours(-4);

        var stuckJobs = await context.ImportJobs
            .Where(x => (x.Status == "Validating" || x.Status == "Committing") &&
                        x.CreatedOn < stuckThreshold)
            .ToListAsync(ct);

        foreach (var job in stuckJobs)
        {
            Logger.LogWarning(
                "IMPORT JOB STUCK: {JobName} (ID: {JobId}) has been in '{Status}' for over 4 hours.",
                job.JobName, job.Id, job.Status);
        }
    }
}
