// <copyright file="ReportDeliveryJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace ERP.Modules.Reporting.Services;

/// <summary>
/// Hangfire recurring job that processes all due report subscriptions.
/// Registered as "reporting-scheduled-delivery" with a daily cron at 6:00 AM UTC.
/// Also supports manual trigger via POST /reporting/execution/run-delivery.
/// </summary>
public class ReportDeliveryJob
{
    private readonly IScheduledDeliveryService _deliveryService;
    private readonly ILogger<ReportDeliveryJob> _logger;

    public ReportDeliveryJob(
        IScheduledDeliveryService deliveryService,
        ILogger<ReportDeliveryJob> logger)
    {
        _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main entry point called by Hangfire on schedule. Processes all subscriptions
    /// that are due for delivery based on their individual schedule configuration.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Report delivery job starting at {Time}", DateTimeOffset.UtcNow);

        var deliveredCount = await _deliveryService.ProcessDueSubscriptionsAsync(cancellationToken);

        _logger.LogInformation(
            "Report delivery job completed. Delivered {Count} report(s) at {Time}",
            deliveredCount, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Delivers a specific subscription by ID. Called from the manual trigger endpoint.
    /// </summary>
    public async Task<DeliveryResult> DeliverOneAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Manual report delivery triggered for subscription {SubscriptionId}",
            subscriptionId);

        return await _deliveryService.DeliverSubscriptionAsync(subscriptionId, cancellationToken);
    }
}
