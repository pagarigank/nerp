// <copyright file="InvToGlPostingTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.Inventory;

/// <summary>
/// Proves Inventory transactions now post to the General Ledger through the
/// canonical posting contract (architecture.md §5.1), closing the previously
/// missing Inventory -> GL integration. A receipt should debit the inventory
/// asset account and credit goods-received (GRNI); an issue should debit COGS
/// and credit inventory.
/// </summary>
public class InvToGlPostingTests : IntegrationTestBase
{
    [Fact]
    public async Task PostReceipt_ShouldCreateBalancedPostedGlJournalBatch()
    {
        await CleanDatabaseAsync();

        var company = new Company($"INVGL-{Guid.NewGuid():N}", "Inventory->GL Co", "USD", null, null, null);
        var fiscalYear = new FiscalYear(company.Id, 2026, "FY2026", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var period = new FiscalPeriod(fiscalYear.Id, company.Id, 1, "2026-01", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        var inventoryAssetId = Guid.NewGuid();
        var cogsId = Guid.NewGuid();
        var grniId = Guid.NewGuid();
        var varianceId = Guid.NewGuid();

        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);
            await platform.FiscalYears.AddAsync(fiscalYear);
            await platform.FiscalPeriods.AddAsync(period);

            // Seed the four inventory GL accounts in platform.Accounts (queried by
            // the handler) and gl.Account (target of the JournalLine FK).
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '1400', 'Inventory Asset', 0, 0, 1, SYSUTCDATETIME(), 'system');", inventoryAssetId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '5000', 'Cost of Goods Sold', 4, 0, 1, SYSUTCDATETIME(), 'system');", cogsId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '2010', 'Goods Received Not Invoiced', 1, 1, 1, SYSUTCDATETIME(), 'system');", grniId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '5900', 'Inventory Variance', 4, 0, 1, SYSUTCDATETIME(), 'system');", varianceId, company.Id);

            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '1400', 'Inventory Asset', 0, 0, 1, SYSUTCDATETIME(), 'system');", inventoryAssetId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '5000', 'Cost of Goods Sold', 4, 0, 1, SYSUTCDATETIME(), 'system');", cogsId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '2010', 'Goods Received Not Invoiced', 1, 1, 1, SYSUTCDATETIME(), 'system');", grniId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '5900', 'Inventory Variance', 4, 0, 1, SYSUTCDATETIME(), 'system');", varianceId, company.Id);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory(
                $"CAT-{Guid.NewGuid():N}", "General", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            await inv.SaveChangesAsync();

            await inv.Items.AddAsync(new Item(
                $"ITEM-{Guid.NewGuid():N}", "Test Widget", company.Id,
                ItemType.Inventory, "EA", CostingMethod.Standard, category.Id));
            await inv.Warehouses.AddAsync(new Warehouse(
                $"WH-{Guid.NewGuid():N}", "Main WH", company.Id, WarehouseType.Distribution));
            await inv.SaveChangesAsync();
        });

        // Post a receipt outside an ambient transaction (the GL consumer opens a
        // separate connection that must not be entangled with the test tx).
        Guid transactionId;
        using (var postScope = ServiceProvider.CreateScope())
        {
            var inv = postScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var item = await inv.Items.FirstAsync();
            var warehouse = await inv.Warehouses.FirstAsync();

            var transaction = new InventoryTransaction(
                company.Id, item.Id, warehouse.Id, TransactionType.Receipt,
                10m, "EA", 50m, new DateTime(2026, 1, 15), referenceNumber: "GR-1");

            inv.InventoryTransactions.Add(transaction);
            await inv.SaveChangesAsync();

            transactionId = transaction.Id;

            var dispatcher = postScope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
                transaction.Id, company.Id, item.Id, warehouse.Id,
                TransactionType.Receipt.ToString(), transaction.Quantity, transaction.UnitCost,
                transaction.ExtendedCost, transaction.TransactionDate, null));
        }

        // Assert (separate read): a balanced, posted GL JournalBatch exists.
        var glBatchNumber = $"INV-TXN-{transactionId:N}";
        var glBatch = await ExecuteInTransactionAsync(async sp =>
        {
            var gl = sp.GetRequiredService<GlDbContext>();
            return await gl.JournalBatches
                .Where(b => b.BatchNumber == glBatchNumber)
                .Include(b => b.Lines)
                .FirstOrDefaultAsync();
        });

        glBatch.Should().NotBeNull("posting an inventory receipt must create a GL journal batch");
        glBatch!.Status.Should().Be(JournalBatchStatus.Posted, "the GL batch must be posted");
        glBatch.Lines.Should().HaveCount(2, "Dr Inventory asset + Cr GRNI");
        glBatch.IsBalanced().Should().BeTrue("the GL batch must balance (debits = credits)");
        // Receipt: Dr Inventory (1400) 500, Cr GRNI (2010) 500.
        glBatch.Lines.Sum(l => l.Debit).Should().Be(500m);
        glBatch.Lines.Sum(l => l.Credit).Should().Be(500m);
    }
}
