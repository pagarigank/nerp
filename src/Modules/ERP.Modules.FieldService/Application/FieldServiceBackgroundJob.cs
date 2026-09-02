// <copyright file="FieldServiceBackgroundJobs.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.FieldService.Domain.Entities;
using ERP.Modules.FieldService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.FieldService.Application;

/// <summary>
/// Background processing for Field Service module. Runs on a 15-minute timer and handles:
/// 1. PM auto-generation — creates work orders for due preventive-maintenance schedules.
/// 2. SLA breach monitoring — flags overdue open work orders and logs escalation contacts.
/// 3. Parts restock alerts — detects van-stock items below reorder point.
/// 4. Contract expiration — auto-expires past-end service contracts and logs warnings.
/// 5. Warranty expiration — alerts on equipment warranties expiring within 30 days.
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Field Service background job failed.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// 1. PM Auto-Generation — create work orders for due preventive-maintenance schedules.
    /// </summary>
    private static async Task<int> GeneratePmWorkOrdersAsync(
        FieldServiceDbContext context, DateTime now, CancellationToken ct)
    {
        var due = await context.PreventiveMaintenances
            .Where(p => p.IsActive && p.NextDue <= now)
            .ToListAsync(ct);

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

        return due.Count;
    }

    /// <summary>
    /// 4. Service Contract Expiration — auto-expire past-end contracts and log warnings.
    /// </summary>
    private static async Task<int> ExpireServiceContractsAsync(
        FieldServiceDbContext context, DateTime now, CancellationToken ct)
    {
        var expiring = await context.ServiceContracts
            .Where(c => c.Status == ServiceContractStatus.Active && c.EndDate < now)
            .ToListAsync(ct);

        foreach (var contract in expiring)
        {
            contract.Expire();
        }

        return expiring.Count;
    }

    private async Task RunPendingWorkAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FieldServiceDbContext>();

        var pmCount = await GeneratePmWorkOrdersAsync(context, now, cancellationToken);
        var breachedCount = await MonitorSlaBreachesAsync(context, now, cancellationToken);
        var restockCount = await CheckVanStockRestockAsync(context, cancellationToken);
        var expiredCount = await ExpireServiceContractsAsync(context, now, cancellationToken);
        var warrantyCount = await MonitorWarrantyExpirationAsync(context, now, cancellationToken);

        if (pmCount + breachedCount + restockCount + expiredCount + warrantyCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Field Service job: {PmCount} PM work orders, {BreachedCount} SLA breaches, " +
                "{RestockCount} restock alerts, {ExpiredCount} contracts expired, " +
                "{WarrantyCount} warranty alerts.",
                pmCount, breachedCount, restockCount, expiredCount, warrantyCount);
        }
    }

    /// <summary>
    /// 2. SLA Breach Monitoring — flag overdue work orders and log escalation contacts.
    /// </summary>
    private async Task<int> MonitorSlaBreachesAsync(
        FieldServiceDbContext context, DateTime now, CancellationToken ct)
    {
        var breached = await context.WorkOrders
            .Where(w => w.ResolutionDue.HasValue && w.ResolutionDue.Value < now &&
                        w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Closed &&
                        w.Status != WorkOrderStatus.Cancelled)
            .ToListAsync(ct);

        foreach (var wo in breached)
        {
            if (!wo.SlaBreached)
            {
                wo.MarkSlaBreached();

                // Look up the SLA definition to find escalation contact.
                var sla = await context.SlaDefinitions
                    .Where(s => s.CompanyId == wo.CompanyId && s.Priority == wo.Priority)
                    .FirstOrDefaultAsync(ct);

                if (sla != null && sla.Escalate && !string.IsNullOrEmpty(sla.EscalationTo))
                {
                    _logger.LogWarning(
                        "SLA ESCALATION: Work order {WoNumber} (priority {Priority}) " +
                        "breached resolution deadline. Escalating to {EscalationContact}.",
                        wo.WorkOrderNumber, wo.Priority, sla.EscalationTo);
                }
                else
                {
                    _logger.LogWarning(
                        "SLA BREACH: Work order {WoNumber} (priority {Priority}) " +
                        "breached resolution deadline at {DueAt}.",
                        wo.WorkOrderNumber, wo.Priority, wo.ResolutionDue);
                }
            }
        }

        return breached.Count;
    }

    /// <summary>
    /// 3. Parts Restock Alerts — detect van-stock items at or below reorder point.
    /// </summary>
    private async Task<int> CheckVanStockRestockAsync(
        FieldServiceDbContext context, CancellationToken ct)
    {
        var lowStock = await context.VanStocks
            .Where(v => v.QuantityOnHand <= v.ReorderPoint)
            .ToListAsync(ct);

        foreach (var item in lowStock)
        {
            _logger.LogWarning(
                "RESTOCK ALERT: Van stock item {ItemId} for technician {TechId} " +
                "is at {QtyOnHand} (reorder point: {ReorderPoint}).",
                item.ItemId, item.TechnicianId, item.QuantityOnHand, item.ReorderPoint);
        }

        return lowStock.Count;
    }

    /// <summary>
    /// 5. Warranty Expiration — alert on equipment warranties expiring within 30 days.
    /// </summary>
    private async Task<int> MonitorWarrantyExpirationAsync(
        FieldServiceDbContext context, DateTime now, CancellationToken ct)
    {
        var horizon = now.AddDays(30);

        var expiringWarranties = await context.EquipmentAssets
            .Where(e => e.UnderWarranty && e.WarrantyEnd.HasValue &&
                        e.WarrantyEnd.Value <= horizon && e.WarrantyEnd.Value > now)
            .ToListAsync(ct);

        foreach (var asset in expiringWarranties)
        {
            _logger.LogWarning(
                "WARRANTY EXPIRING: Asset {AssetTag} ({Description}) warranty ends on {WarrantyEnd}.",
                asset.AssetTag,
                asset.Description,
                asset.WarrantyEnd!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        return expiringWarranties.Count;
    }
}
