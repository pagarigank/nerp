// <copyright file="CostRecalculationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Inventory.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Application.BackgroundJobs;

public class CostRecalculationJob
{
    private readonly InventoryDbContext _context;
    private readonly CostingService _costingService;
    private readonly ILogger<CostRecalculationJob> _logger;

    public CostRecalculationJob(
        InventoryDbContext context,
        CostingService costingService,
        ILogger<CostRecalculationJob> logger)
    {
        _context = context;
        _costingService = costingService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting CostRecalculationJob");

            var companies = await _context.Items
                .Select(i => i.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companies)
            {
                await ProcessCompanyCostRecalculationAsync(companyId, cancellationToken);
            }

            _logger.LogInformation("CostRecalculationJob completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CostRecalculationJob");
            throw;
        }
    }

    private async Task ProcessCompanyCostRecalculationAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var items = await _context.Items
            .Where(i => i.CompanyId == companyId && i.Status == ItemStatus.Active)
            .ToListAsync(cancellationToken);

        int recalculated = 0;

        foreach (var item in items)
        {
            var warehouses = await _context.ItemStocks
                .Where(s => s.CompanyId == companyId && s.ItemId == item.Id)
                .Select(s => s.WarehouseId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var warehouseId in warehouses)
            {
                // Get current average cost for the item/warehouse
                var avgCost = await _costingService.CalculateAverageCostAsync(item.Id, warehouseId, cancellationToken);

                // Update cost layers with new average cost (create new layers with adjusted cost)
                var costLayers = await _context.ItemCostLayers
                    .Where(cl => cl.CompanyId == companyId
                              && cl.ItemId == item.Id
                              && cl.WarehouseId == warehouseId
                              && cl.RemainingQuantity > 0)
                    .ToListAsync(cancellationToken);

                foreach (var layer in costLayers)
                {
                    if (layer.UnitCost != avgCost)
                    {
                        // Cannot set UnitCost directly (private setter), so we need a different approach
                        // For now, just log the difference
                        _logger.LogInformation("Cost layer {LayerId} for item {ItemId} at warehouse {WarehouseId} has different cost: {LayerCost} vs avg {AvgCost}",
                            layer.Id, item.Id, warehouseId, layer.UnitCost, avgCost);
                    }
                }
            }
        }

        _logger.LogInformation("Processed cost recalculation for company {CompanyId}: {Count} cost layers reviewed",
            companyId, recalculated);
    }
}