// <copyright file="ReorderAlertJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Application.BackgroundJobs;

public class ReorderAlertJob
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ReorderAlertJob> _logger;

    public ReorderAlertJob(InventoryDbContext context, ILogger<ReorderAlertJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ReorderAlertJob");

            var companies = await _context.Items
                .Select(i => i.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companies)
            {
                await ProcessCompanyReorderAlertsAsync(companyId, cancellationToken);
            }

            _logger.LogInformation("ReorderAlertJob completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReorderAlertJob");
            throw;
        }
    }

    private async Task ProcessCompanyReorderAlertsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var itemsBelowReorder = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId)
            .Include(s => s.Item)
            .Include(s => s.Warehouse)
            .ToListAsync(cancellationToken);

        // Filter in memory to avoid null reference issues in LINQ translation
        itemsBelowReorder = itemsBelowReorder
            .Where(s => s.Item != null
                     && s.OnHandQuantity <= (s.Item.ReorderPoint ?? 0)
                     && (s.Item.ReorderPoint ?? 0) > 0
                     && s.Item.Status == ItemStatus.Active)
            .ToList();

        foreach (var stock in itemsBelowReorder)
        {
            // Null check for Item (should not be null due to Include, but compiler needs it)
            if (stock.Item == null)
            {
                continue;
            }

            // Check if alert already exists for this item/warehouse
            var existingAlert = await _context.ReorderAlerts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId
                                     && a.ItemId == stock.ItemId
                                     && a.WarehouseId == stock.WarehouseId
                                     && !a.IsAcknowledged
                                     && a.AlertDate >= DateTime.UtcNow.AddDays(-7), cancellationToken);

            if (existingAlert == null)
            {
                var reorderPoint = stock.Item.ReorderPoint ?? 0;
                var itemCode = stock.Item.ItemCode ?? "Unknown";
                var alert = new ReorderAlert(
                                    companyId,
                                    stock.ItemId,
                                    stock.WarehouseId,
                                    stock.OnHandQuantity,
                                    reorderPoint,
                                    $"Item {itemCode} is below reorder point. On hand: {stock.OnHandQuantity}, Reorder point: {reorderPoint}");

                _context.ReorderAlerts.Add(alert);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processed reorder alerts for company {CompanyId}: {Count} alerts created",
            companyId, itemsBelowReorder.Count);
    }
}