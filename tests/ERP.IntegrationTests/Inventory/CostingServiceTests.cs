// <copyright file="CostingServiceTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Modules.Inventory.Application.Services;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.Inventory;

[Collection("Inventory Integration")]
public class CostingServiceTests : IntegrationTestBase
{
    private async Task<(Guid CompanyId, Guid ItemId, Guid WarehouseId)> SeedItemWithLayersAsync(
        CostingMethod method, (decimal qty, decimal cost, int dayOffset)[] layers)
    {
        Guid companyId = Guid.Empty, itemId = Guid.Empty, warehouseId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"CST-{Guid.NewGuid():N}", "Costing Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Costed", company.Id, ItemType.Inventory, "EA", method, category.Id);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            await inv.SaveChangesAsync();

            foreach (var (qty, cost, dayOffset) in layers)
            {
                inv.ItemCostLayers.Add(new ItemCostLayer(
                    company.Id, item.Id, warehouse.Id, qty, cost, DateTime.UtcNow.AddDays(dayOffset), null, "SEED"));
                // Average costing reads from InventoryTransactions with Quantity > 0.
                inv.InventoryTransactions.Add(new InventoryTransaction(
                    company.Id, item.Id, warehouse.Id, TransactionType.Receipt, qty, "EA", cost, DateTime.UtcNow.AddDays(dayOffset)));
            }
            await inv.SaveChangesAsync();

            companyId = company.Id;
            itemId = item.Id;
            warehouseId = warehouse.Id;
        });
        return (companyId, itemId, warehouseId);
    }

    [Fact]
    public async Task Fifo_CostMatchesOldestLayerFirst()
    {
        var (_, itemId, warehouseId) = await SeedItemWithLayersAsync(
            CostingMethod.FIFO, new[] { (10m, 5m, 0), (10m, 6m, 1) });

        using var scope = ServiceProvider.CreateScope();
        var costing = scope.ServiceProvider.GetRequiredService<CostingService>();
        var consumption = await costing.GetFifoConsumptionAsync(itemId, warehouseId, 15m);

        consumption.Sum(c => c.Quantity * c.UnitCost).Should().Be(80m, "10@5 + 5@6 = 80");
        consumption.Sum(c => c.Quantity).Should().Be(15m);
    }

    [Fact]
    public async Task Lifo_CostMatchesNewestLayerFirst()
    {
        var (_, itemId, warehouseId) = await SeedItemWithLayersAsync(
            CostingMethod.LIFO, new[] { (10m, 5m, 0), (10m, 6m, 1) });

        using var scope = ServiceProvider.CreateScope();
        var costing = scope.ServiceProvider.GetRequiredService<CostingService>();
        var consumption = await costing.GetLifoConsumptionAsync(itemId, warehouseId, 15m);

        consumption.Sum(c => c.Quantity * c.UnitCost).Should().Be(85m, "10@6 + 5@5 = 85");
        consumption.Sum(c => c.Quantity).Should().Be(15m);
    }

    [Fact]
    public async Task Average_CostIsWeightedMean()
    {
        var (_, itemId, warehouseId) = await SeedItemWithLayersAsync(
            CostingMethod.Average, new[] { (10m, 5m, 0), (10m, 6m, 1) });

        using var scope = ServiceProvider.CreateScope();
        var costing = scope.ServiceProvider.GetRequiredService<CostingService>();
        var unitCost = await costing.CalculateAverageCostAsync(itemId, warehouseId, System.Threading.CancellationToken.None);

        unitCost.Should().Be(5.5m, "(10*5 + 10*6) / 20 = 5.5");
    }
}
