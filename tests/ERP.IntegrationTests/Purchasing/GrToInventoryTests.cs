// <copyright file="GrToInventoryTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.Purchasing;

/// <summary>
/// Proves the Purchasing -&gt; Inventory integration: posting a goods receipt
/// raises GoodsReceivedEvent (previously never raised) and the inventory
/// consumer creates the corresponding inventory receipt transaction. This closes
/// the goods-receipt -&gt; stock-update integration gap.
/// </summary>
public class GrToInventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task PostReceipt_ShouldCreateInventoryReceiptTransaction()
    {
        await CleanDatabaseAsync();

        var company = new Company($"GRINV-{Guid.NewGuid():N}", "GR->Inventory Co", "USD", null, null, null);

        Guid itemId = Guid.Empty;
        Guid warehouseId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "General", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            await inv.SaveChangesAsync();

            var item = new Item(
                $"ITEM-{Guid.NewGuid():N}", "Test Widget", company.Id,
                ItemType.Inventory, "EA", CostingMethod.Standard, category.Id);
            await inv.Items.AddAsync(item);

            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "Main WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);

            await inv.SaveChangesAsync();
            itemId = item.Id;
            warehouseId = warehouse.Id;
        });

        // Post a goods receipt (outside an ambient transaction; the consumer opens
        // the inventory connection, which must not be entangled with this tx).
        using (var postScope = ServiceProvider.CreateScope())
        {
            var purchasing = postScope.ServiceProvider.GetRequiredService<PurchasingDbContext>();

            var receipt = new Receipt(
                $"GR-{Guid.NewGuid():N}", company.Id, null, null, DateTime.UtcNow, "tester", null, null);
            receipt.AddLine(new ReceiptLine(
                receipt.Id, 1, null, itemId.ToString(), "Test Widget", 5m, "EA", null, null, false, warehouseId, null));

            purchasing.Receipts.Add(receipt);
            await purchasing.SaveChangesAsync();

            receipt.Post();
            await purchasing.SaveChangesAsync();
        }

        // Assert: an inventory receipt transaction was created for the item.
        var invTransaction = await ExecuteInTransactionAsync(async sp =>
        {
            var inv = sp.GetRequiredService<InventoryDbContext>();
            return await inv.InventoryTransactions
                .Where(t => t.ItemId == itemId && t.TransactionType == TransactionType.Receipt)
                .FirstOrDefaultAsync();
        });

        invTransaction.Should().NotBeNull("posting a goods receipt must create an inventory receipt transaction");
        invTransaction!.Quantity.Should().Be(5m, "the received quantity should flow into inventory");
    }
}
