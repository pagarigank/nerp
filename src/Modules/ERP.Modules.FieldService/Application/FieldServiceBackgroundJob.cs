// <copyright file="FieldServiceBackgroundJobs.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.FieldService.Domain.Entities;
using ERP.Modules.FieldService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.FieldService.Application;

/// <summary>
/// Background processing for Field Service: generates preventive-maintenance
/// work orders when they are due, and flags SLA breaches on overdue open work
/// orders. Runs on a timer inside the API process.
/// </summary>
public class FieldServiceBackgroundJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FieldServiceBackgroundJob> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(15);

    public FieldServiceBackgroundJob(IServiceScopeFactory scopeFactory, ILogger<FieldServiceBackgroundJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        do
        {
            try
            {
                await RunPendingWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Field Service background job failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunPendingWorkAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FieldServiceDbContext>();

        // Generate PM work orders for due schedules.
        var due = await context.PreventiveMaintenances
            .Where(p => p.IsActive && p.NextDue <= now)
            .ToListAsync(cancellationToken);

        foreach (var pm in due)
        {
            var wo = new WorkOrder(
                pm.CompanyId,
                $"PM-{pm.Code}",
                null,
                null,
                pm.ServiceContractId,
                pm.EquipmentAssetId,
                null,
                pm.Id,
                WorkOrderType.PreventiveMaintenance,
                SlaPriority.Medium,
                pm.DefaultTechnicianId,
                null,
                pm.NextDue,
                pm.NextDue,
                null,
                null,
                false,
                pm.Checklist);
            context.WorkOrders.Add(wo);

            pm.MarkGenerated(now);
        }

        // Flag SLA breaches on overdue, still-open work orders.
        var breached = await context.WorkOrders
            .Where(w => w.ResolutionDue.HasValue && w.ResolutionDue.Value < now &&
                        w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Closed &&
                        w.Status != WorkOrderStatus.Cancelled)
            .ToListAsync(cancellationToken);

        foreach (var wo in breached)
        {
            wo.MarkSlaBreached();
        }

        if (due.Count > 0 || breached.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Field Service job: generated {PmCount} PM work orders, flagged {BreachedCount} SLA breaches.",
                due.Count,
                breached.Count);
        }
    }
}
