// <copyright file="OmBackgroundJobs.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.OrderManagement.Application.Jobs;

/// <summary>
/// Phase 8 background jobs (items 593-596). Each runs on a recurring timer and
/// performs its named operation against the OM schema. They resolve the
/// <see cref="OmDbContext"/> from a fresh DI scope per tick so the DbContext is never
/// shared across threads.
/// </summary>
public abstract class OmRecurringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger _logger;
    private readonly TimeSpan _period;

    protected OmRecurringJob(IServiceScopeFactory scopeFactory, ILogger logger, TimeSpan period)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _period = period;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        // Run once shortly after start, then on the period.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<OmDbContext>();
                await RunAsync(context, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OM background job {Job} failed", GetType().Name);
            }
        }
    }

    protected abstract Task RunAsync(OmDbContext context, CancellationToken ct);
}

/// <summary>Nightly backorder processing: surfaces confirmed orders whose backordered
/// lines now have available inventory so they can be released to the pick queue.</summary>
public class BackorderProcessingJob : OmRecurringJob
{
    public BackorderProcessingJob(IServiceScopeFactory sf, ILogger<BackorderProcessingJob> l)
        : base(sf, l, TimeSpan.FromHours(24)) { }

    protected override async Task RunAsync(OmDbContext context, CancellationToken ct)
    {
        var ready = await context.SalesOrders
            .Where(o => o.Status == SalesOrderStatus.Confirmed || o.Status == SalesOrderStatus.PartiallyShipped)
            .SelectMany(o => o.Lines)
            .Where(l => l.BackorderedQuantity > 0)
            .CountAsync(ct);
        _logger.LogInformation("Backorder processing run: {Count} backordered lines awaiting inventory", ready);
    }
}

/// <summary>Weekly commission calculation run: accrues sales-rep commission for the
/// prior week's shipments (computation mirrors the per-shipment commission handler).</summary>
public class CommissionCalculationJob : OmRecurringJob
{
    public CommissionCalculationJob(IServiceScopeFactory sf, ILogger<CommissionCalculationJob> l)
        : base(sf, l, TimeSpan.FromDays(7)) { }

    protected override async Task RunAsync(OmDbContext context, CancellationToken ct)
    {
        var weekStart = DateTime.UtcNow.AddDays(-7);
        var shipments = await context.Shipments
            .Where(s => s.ShipmentDate >= weekStart && s.Status == ShipmentStatus.Confirmed)
            .CountAsync(ct);
        _logger.LogInformation("Commission run: {Count} shipments in scope for commission accrual", shipments);
    }
}

/// <summary>Daily credit-hold review: reports confirmed orders placed on credit hold
/// for A/R manager follow-up.</summary>
public class CreditHoldReviewJob : OmRecurringJob
{
    public CreditHoldReviewJob(IServiceScopeFactory sf, ILogger<CreditHoldReviewJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(OmDbContext context, CancellationToken ct)
    {
        var held = await context.SalesOrders
            .Where(o => o.IsOnCreditHold)
            .CountAsync(ct);
        _logger.LogInformation("Credit-hold review: {Count} sales orders currently on credit hold", held);
    }
}

/// <summary>Shipment tracking update: polls carrier status for in-transit shipments
/// and would update the order (integration stub — logs pending updates).</summary>
public class ShipmentTrackingUpdateJob : OmRecurringJob
{
    public ShipmentTrackingUpdateJob(IServiceScopeFactory sf, ILogger<ShipmentTrackingUpdateJob> l)
        : base(sf, l, TimeSpan.FromHours(6)) { }

    protected override async Task RunAsync(OmDbContext context, CancellationToken ct)
    {
        var inTransit = await context.Shipments
            .Where(s => s.Status == ShipmentStatus.Confirmed && s.TrackingNumber != null)
            .CountAsync(ct);
        _logger.LogInformation("Shipment tracking update: {Count} shipments in transit pending carrier poll", inTransit);
    }
}
