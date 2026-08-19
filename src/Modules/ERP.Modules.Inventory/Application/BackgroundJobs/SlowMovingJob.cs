// <copyright file="SlowMovingJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Application.BackgroundJobs;

public class SlowMovingJob
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<SlowMovingJob> _logger;

    public SlowMovingJob(InventoryDbContext context, ILogger<SlowMovingJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting SlowMovingJob");

            var companies = await _context.Items
                .Select(i => i.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companies)
            {
                await ProcessCompanySlowMovingAsync(companyId, cancellationToken);
            }

            _logger.LogInformation("SlowMovingJob completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SlowMovingJob");
            throw;
        }
    }

    private async Task ProcessCompanySlowMovingAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var itemsWithStock = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && s.OnHandQuantity > 0)
            .Include(s => s.Item)
            .Include(s => s.Warehouse)
            .ToListAsync(cancellationToken);

        foreach (var stock in itemsWithStock)
        {
            var lastIssue = await _context.InventoryTransactions
                .Where(t => t.CompanyId == companyId
                         && t.ItemId == stock.ItemId
                         && t.WarehouseId == stock.WarehouseId
                         && t.TransactionType == TransactionType.Issue)
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefaultAsync(cancellationToken);

            bool isSlowMoving = false;
            int daysSinceLastMovement = 0;

            if (lastIssue != null)
            {
                daysSinceLastMovement = (int)(DateTime.UtcNow - lastIssue.TransactionDate).TotalDays;
                isSlowMoving = daysSinceLastMovement > 365; // No movement in 12 months
            }
            else
            {
                // Check if item was received more than 12 months ago
                var lastReceipt = await _context.InventoryTransactions
                    .Where(t => t.CompanyId == companyId
                             && t.ItemId == stock.ItemId
                             && t.WarehouseId == stock.WarehouseId
                             && t.TransactionType == TransactionType.Receipt)
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastReceipt != null && (DateTime.UtcNow - lastReceipt.TransactionDate).TotalDays > 365)
                {
                    isSlowMoving = true;
                    daysSinceLastMovement = (int)(DateTime.UtcNow - lastReceipt.TransactionDate).TotalDays;
                }
            }

            // Check if slow moving alert already exists
            var existingAlert = await _context.SlowMovingAlerts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId
                                     && a.ItemId == stock.ItemId
                                     && a.WarehouseId == stock.WarehouseId
                                     && !a.IsAcknowledged
                                     && a.AlertDate >= DateTime.UtcNow.AddDays(-30), cancellationToken);

            if (isSlowMoving && existingAlert == null)
            {
                var alert = new SlowMovingAlert(
                                    companyId,
                                    stock.ItemId,
                                    stock.WarehouseId,
                                    stock.OnHandQuantity,
                                    daysSinceLastMovement,
                                    $"Item {stock.Item?.ItemCode ?? "Unknown"} has not moved for {daysSinceLastMovement} days. On hand: {stock.OnHandQuantity}");

                _context.SlowMovingAlerts.Add(alert);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processed slow moving analysis for company {CompanyId}", companyId);
    }
}