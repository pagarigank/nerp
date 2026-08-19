// <copyright file="ValuationSnapshotJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Application.BackgroundJobs;

public class ValuationSnapshotJob
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ValuationSnapshotJob> _logger;

    public ValuationSnapshotJob(InventoryDbContext context, ILogger<ValuationSnapshotJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ValuationSnapshotJob");

            var companies = await _context.Items
                .Select(i => i.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companies)
            {
                await ProcessCompanyValuationSnapshotAsync(companyId, cancellationToken);
            }

            _logger.LogInformation("ValuationSnapshotJob completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValuationSnapshotJob");
            throw;
        }
    }

    private async Task ProcessCompanyValuationSnapshotAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var snapshotDate = DateTime.UtcNow.Date;

        var stocks = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && s.OnHandQuantity > 0)
            .Include(s => s.Item)
            .Include(s => s.Warehouse)
            .ToListAsync(cancellationToken);

        foreach (var stock in stocks)
        {
            // Get current cost from cost layers
            var avgCost = await _context.ItemCostLayers
                .Where(cl => cl.CompanyId == companyId
                          && cl.ItemId == stock.ItemId
                          && cl.WarehouseId == stock.WarehouseId
                          && cl.RemainingQuantity > 0)
                .AverageAsync(cl => (decimal?)cl.UnitCost, cancellationToken) ?? 0;

            // Get standard cost
            var standardCost = stock.Item?.StandardCost ?? 0;

            // Create valuation snapshot
            var snapshot = new InventoryValuationSnapshot(
                companyId,
                stock.ItemId,
                stock.WarehouseId,
                snapshotDate,
                stock.OnHandQuantity,
                standardCost,
                avgCost);

            _context.InventoryValuationSnapshots.Add(snapshot);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created valuation snapshots for company {CompanyId}: {Count} items",
            companyId, stocks.Count);
    }
}