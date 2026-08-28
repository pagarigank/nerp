// <copyright file="ScheduledDeliveryService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Reporting.Services;

/// <summary>
/// Handles scheduled report delivery. When a subscription is triggered (by
/// Hangfire or on-demand), this service:
/// 1. Resolves the report definition and parameters
/// 2. Executes the report via the report engine
/// 3. Exports to the requested format (PDF/Excel/CSV)
/// 4. Delivers to the configured recipients (email or portal notification)
/// 5. Records the delivery result in the subscription and usage log
/// </summary>
public interface IScheduledDeliveryService
{
    /// <summary>
    /// Delivers a single subscription: executes the report and sends to recipients.
    /// Returns the delivery result with success/failure status.
    /// </summary>
    Task<DeliveryResult> DeliverSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes all subscriptions that are due for delivery based on their schedule.
    /// Called by the Hangfire job on the configured cron schedule.
    /// </summary>
    Task<int> ProcessDueSubscriptionsAsync(
        CancellationToken cancellationToken = default);
}

public class DeliveryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RecipientCount { get; set; }
    public string? ExportFormat { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTimeOffset DeliveredOn { get; set; }
}

public class ScheduledDeliveryService : IScheduledDeliveryService
{
    private readonly ReportingDbContext _rptDb;
    private readonly ILogger<ScheduledDeliveryService> _logger;

    public ScheduledDeliveryService(
        ReportingDbContext rptDb,
        ILogger<ScheduledDeliveryService> logger)
    {
        _rptDb = rptDb ?? throw new ArgumentNullException(nameof(rptDb));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeliveryResult> DeliverSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var subscription = await _rptDb.ReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);

        if (subscription == null)
        {
            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = $"Subscription {subscriptionId} not found",
            };
        }

        if (!subscription.IsActive)
        {
            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = "Subscription is inactive",
            };
        }

        try
        {
            // 1. Resolve report definition
            var reportDef = await _rptDb.ReportDefinitions
                .FirstOrDefaultAsync(r => r.Id == subscription.ReportDefinitionId, cancellationToken);

            if (reportDef == null)
            {
                throw new InvalidOperationException(
                    $"Report definition {subscription.ReportDefinitionId} not found");
            }

            // 2. Parse recipients
            var recipients = ParseRecipients(subscription.RecipientsJson);

            // 3. Execute the report (placeholder — real implementation would call the report engine)
            var rowCount = await ExecuteReportAsync(
                reportDef, subscription.ParametersJson, cancellationToken);

            // 4. Export to format
            var exportResult = await ExportReportAsync(
                reportDef, subscription.ExportFormat, subscription.ParametersJson, cancellationToken);

            // 5. Deliver to recipients
            var deliveredCount = await DeliverToRecipientsAsync(
                recipients, reportDef.Name, subscription.ExportFormat, exportResult, cancellationToken);

            sw.Stop();

            // 6. Record success
            subscription.RecordRun("Success");

            // 7. Log usage
            var usageLog = new ReportUsageLog(
                reportDef.CompanyId,
                reportDef.ReportType,
                reportDef.Id,
                null,
                "scheduler",
                subscription.ParametersJson,
                subscription.ExportFormat,
                sw.ElapsedMilliseconds,
                rowCount);

            _rptDb.ReportUsageLogs.Add(usageLog);
            await _rptDb.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Report subscription {SubscriptionId} delivered successfully. " +
                "Report: {ReportName}, Format: {Format}, Recipients: {Count}, " +
                "Rows: {Rows}, Time: {Time}ms",
                subscriptionId, reportDef.Name, subscription.ExportFormat,
                deliveredCount, rowCount, sw.ElapsedMilliseconds);

            return new DeliveryResult
            {
                Success = true,
                RecipientCount = deliveredCount,
                ExportFormat = subscription.ExportFormat,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                DeliveredOn = DateTimeOffset.UtcNow,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            subscription.RecordRun("Failed", ex.Message);

            await _rptDb.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex,
                "Report subscription {SubscriptionId} delivery failed: {Error}",
                subscriptionId, ex.Message);

            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                DeliveredOn = DateTimeOffset.UtcNow,
            };
        }
    }

    public async Task<int> ProcessDueSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeSubscriptions = await _rptDb.ReportSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var deliveredCount = 0;

        foreach (var subscription in activeSubscriptions)
        {
            if (IsDueForDelivery(subscription))
    {
                var result = await DeliverSubscriptionAsync(subscription.Id, cancellationToken);
                if (result.Success)
                {
                    deliveredCount++;
                }
            }
        }

        return deliveredCount;
    }

    private static bool IsDueForDelivery(ReportSubscription subscription)
    {
        if (subscription.LastRunOn == null)
        {
            return true; // Never run, always due
        }

        var now = DateTimeOffset.UtcNow;
        var lastRun = subscription.LastRunOn.Value;

        return subscription.ScheduleType switch
        {
            "Daily" => (now - lastRun).TotalHours >= 23,
            "Weekly" => (now - lastRun).TotalDays >= 6,
            "Monthly" => (now - lastRun).TotalDays >= 28,
            "OnDemand" => false,
            _ => false,
        };
    }

    private static List<string> ParseRecipients(string? recipientsJson)
    {
        if (string.IsNullOrEmpty(recipientsJson))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(recipientsJson) ?? [];
        }
        catch
        {
            // Fallback: treat as comma-separated
            return recipientsJson
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }

    private static async Task<int> ExecuteReportAsync(
        ReportDefinition reportDef,
        string? parametersJson,
        CancellationToken cancellationToken)
    {
        // Placeholder: real implementation would invoke the report engine
        // against the GL/AP/AR/etc. data sources based on reportDef.DataSource
        await Task.CompletedTask;
        return 0;
    }

    private static async Task<byte[]> ExportReportAsync(
        ReportDefinition reportDef,
        string format,
        string? parametersJson,
        CancellationToken cancellationToken)
    {
        // Placeholder: real implementation would generate PDF/Excel/CSV
        await Task.CompletedTask;
        return [];
    }

    private static async Task<int> DeliverToRecipientsAsync(
        IReadOnlyList<string> recipients,
        string reportName,
        string format,
        byte[] exportData,
        CancellationToken cancellationToken)
    {
        // Placeholder: real implementation would send email via SMTP/SendGrid
        // or post to the portal notification system
        await Task.CompletedTask;
        return recipients.Count;
    }
}
