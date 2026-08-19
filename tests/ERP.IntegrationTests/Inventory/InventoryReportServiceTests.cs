// <copyright file="InventoryReportServiceTests.cs" company="ERP Project">
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
public class InventoryReportServiceTests : IntegrationTestBase
{
    [Fact]
    public async Task Valuation_ComputesExtendedValue_FromCostLayers()
    {
        Guid companyId = Guid.Empty;
        Guid itemId = Guid.Empty;
        Guid warehouseId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"VAL-{Guid.NewGuid():N}", "Val Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Widget", company.Id, ItemType.Inventory, "EA", CostingMethod.FIFO, category.Id);
            item.UpdateReorderParameters(0, 0, 0, 0);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            await inv.SaveChangesAsync();

            inv.ItemCostLayers.Add(new ItemCostLayer(company.Id, item.Id, warehouse.Id, 10m, 5m, DateTime.UtcNow, null, "SEED"));
            inv.ItemStocks.Add(new ItemStock(company.Id, item.Id, warehouse.Id));

            await inv.SaveChangesAsync();
            // Set on-hand to 4 after creation (ItemStock starts at 0).
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id);
            stock.SetOnHand(4m);
            await inv.SaveChangesAsync();

            companyId = company.Id;
            itemId = item.Id;
            warehouseId = warehouse.Id;
        });

        using var scope = ServiceProvider.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<InventoryReportService>();
        var rows = await reports.GetValuationAsync(companyId);

        var row = rows.Single(r => r.ItemId == itemId);
        row.OnHandQuantity.Should().Be(4m);
        row.UnitCost.Should().Be(5m, "single layer at $5");
        row.ExtendedValue.Should().Be(20m, "4 x $5 = $20");
    }

    [Fact]
    public async Task LotTraceability_ShowsReceivedIssuedAndRemaining()
    {
        Guid companyId = Guid.Empty;
        Guid itemId = Guid.Empty;
        Guid warehouseId = Guid.Empty;
        Guid lotId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"LT-{Guid.NewGuid():N}", "LT Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Widget", company.Id, ItemType.Inventory, "EA", CostingMethod.FIFO, category.Id);
            item.UpdateReorderParameters(0, 0, 0, 0);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            var lot = new Lot("LOT-1", item.Id, warehouse.Id, DateTime.UtcNow);
            await inv.Lots.AddAsync(lot);
            await inv.SaveChangesAsync();

            inv.ItemCostLayers.Add(new ItemCostLayer(company.Id, item.Id, warehouse.Id, 10m, 5m, DateTime.UtcNow, lot.Id, "SEED"));
            inv.InventoryTransactions.Add(new InventoryTransaction(
                company.Id, item.Id, warehouse.Id, TransactionType.Issue, 3m, "EA", 5m, DateTime.UtcNow,
                null, lot.Id, null, "REF", null, null));
            await inv.SaveChangesAsync();

            companyId = company.Id;
            itemId = item.Id;
            warehouseId = warehouse.Id;
            lotId = lot.Id;
        });

        using var scope = ServiceProvider.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<InventoryReportService>();
        var rows = await reports.GetLotTraceabilityAsync(companyId);

        var row = rows.Single(r => r.LotId == lotId);
        row.ReceivedQuantity.Should().Be(10m);
        row.IssuedQuantity.Should().Be(3m);
        row.RemainingQuantity.Should().Be(7m);
    }

    [Fact]
    public async Task SerialTraceability_ReturnsSerialHistory()
    {
        Guid companyId = Guid.Empty;
        Guid itemId = Guid.Empty;
        Guid serialId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"ST-{Guid.NewGuid():N}", "ST Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Gizmo", company.Id, ItemType.Inventory, "EA", CostingMethod.Standard, category.Id);
            item.UpdateReorderParameters(0, 0, 0, 0);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            var sn = new SerialNumber("SN-001", item.Id, warehouse.Id, DateTime.UtcNow, "5yr warranty", null, null, SerialStatus.InStock);
            await inv.SerialNumbers.AddAsync(sn);
            await inv.SaveChangesAsync();

            companyId = company.Id;
            itemId = item.Id;
            serialId = sn.Id;
        });

        using var scope = ServiceProvider.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<InventoryReportService>();
        var rows = await reports.GetSerialTraceabilityAsync(companyId);

        var row = rows.Single(r => r.SerialId == serialId);
        row.SerialNo.Should().Be("SN-001");
        row.Status.Should().Be("InStock");
    }

    [Fact]
    public async Task InventoryTurnover_ComputesFromIssueCogs()
    {
        Guid companyId = Guid.Empty;
        Guid itemId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"TO-{Guid.NewGuid():N}", "TO Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Widget", company.Id, ItemType.Inventory, "EA", CostingMethod.Standard, category.Id);
            item.UpdateStandardCost(10m);
            item.UpdateReorderParameters(0, 0, 0, 0);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            await inv.SaveChangesAsync();

            var stock = new ItemStock(company.Id, item.Id, warehouse.Id);
            stock.SetOnHand(100m);
            inv.ItemStocks.Add(stock);
            inv.InventoryTransactions.Add(new InventoryTransaction(
                company.Id, item.Id, warehouse.Id, TransactionType.Issue, 50m, "EA", 10m, DateTime.UtcNow,
                null, null, null, "REF", null, null));
            await inv.SaveChangesAsync();

            companyId = company.Id;
            itemId = item.Id;
        });

        using var scope = ServiceProvider.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<InventoryReportService>();
        var from = DateTime.UtcNow.AddYears(-1);
        var to = DateTime.UtcNow;
        var rows = await reports.GetInventoryTurnoverAsync(companyId, from, to);

        rows.Should().Contain(r => r.ItemId == itemId);
        rows.Single(r => r.ItemId == itemId).Cogs.Should().Be(500m);
    }

    [Fact]
    public async Task CycleCountVariance_ComputesVarianceFromCountedQty()
    {
        Guid companyId = Guid.Empty;
        Guid itemId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"CC-{Guid.NewGuid():N}", "CC Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Widget", company.Id, ItemType.Inventory, "EA", CostingMethod.Standard, category.Id);
            item.UpdateStandardCost(10m);
            item.UpdateReorderParameters(0, 0, 0, 0);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            await inv.SaveChangesAsync();

            var count = new CycleCount(company.Id, warehouse.Id, $"CC-{Guid.NewGuid():N}", DateTime.UtcNow, CycleCountStatus.Completed);
            var line = new CycleCountLine(count.Id, item.Id, null, 100m, 95m, null, null, "damage");
            count.AddLine(line);
            inv.CycleCounts.Add(count);
            await inv.SaveChangesAsync();

            companyId = company.Id;
            itemId = item.Id;
        });

        using var scope = ServiceProvider.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<InventoryReportService>();
        var rows = await reports.GetCycleCountVarianceAsync(companyId);

        var row = rows.Single(r => r.ItemId == itemId);
        row.SystemQuantity.Should().Be(100m);
        row.CountedQuantity.Should().Be(95m);
        row.VarianceQuantity.Should().Be(-5m);
        row.VarianceValue.Should().Be(-50m);
    }

    [Fact]
    public async Task CycleCountSummary_RollsUpVarianceByWarehouse()
    {
        Guid companyId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"CS-{Guid.NewGuid():N}", "CS Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "Gen", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            var item = new Item($"ITEM-{Guid.NewGuid():N}", "Widget", company.Id, ItemType.Inventory, "EA", CostingMethod.Standard, category.Id);
            item.UpdateStandardCost(10m);
            item.UpdateReorderParameters(0, 0, 0, 0);
            await inv.Items.AddAsync(item);
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            await inv.SaveChangesAsync();

            var count = new CycleCount(company.Id, warehouse.Id, $"CC-{Guid.NewGuid():N}", DateTime.UtcNow, CycleCountStatus.Completed);
            count.AddLine(new CycleCountLine(count.Id, item.Id, null, 100m, 95m, null, null, null));
            inv.CycleCounts.Add(count);
            await inv.SaveChangesAsync();

            companyId = company.Id;
        });

        using var scope = ServiceProvider.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<InventoryReportService>();
        var rows = await reports.GetCycleCountSummaryAsync(companyId);

        rows.Should().HaveCount(1);
        rows[0].LineCount.Should().Be(1);
        rows[0].TotalVarianceQuantity.Should().Be(-5m);
    }
}
