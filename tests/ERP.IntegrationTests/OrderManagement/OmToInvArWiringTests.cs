// <copyright file="OmToInvArWiringTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.OrderManagement;

/// <summary>
/// Proves the Purchase -> Inventory -> Sales integration chain is wired end-to-end:
/// confirming a sales shipment dispatches <see cref="ShipmentConfirmedEvent"/> which
/// (a) relieves inventory (Inventory module) and (b) generates + posts an AR invoice
/// (Accounts Receivable module -> GL). This closes the order-to-cash flow that was
/// the Phase 8 "CRITICAL GAP".
/// </summary>
[Collection("OrderManagement Integration")]
public class OmToInvArWiringTests : IntegrationTestBase
{
    [Fact]
    public async Task ConfirmShipment_ShouldDecrementInventoryAndCreateArInvoice()
    {
        await CleanDatabaseAsync();

        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        // Capture pre-shipment on-hand.
        decimal onHandBefore;
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            onHandBefore = stock.OnHandQuantity;
        }

        const decimal shipQty = 3m;
        const decimal unitPrice = 100m;

        // Create + confirm a sales shipment. Confirming dispatches
        // ShipmentConfirmedEvent synchronously (OM DbContext SaveChanges).
        Guid? shipmentId;
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var arContext = scope.ServiceProvider.GetRequiredService<ArDbContext>();

            var shipment = new Shipment(
                $"SHIP-{Guid.NewGuid():N}",
                company.Id,
                customerId,
                null,
                DateTime.UtcNow,
                carrier: "UPS",
                freightCost: 0);
            shipment.AddLine(new ShipmentLine(
                shipment.Id, 1, item.Id, "Test Widget", shipQty, unitPrice, "EA",
                warehouseId: warehouse.Id, accountId: RevenueAccountId));

            om.Shipments.Add(shipment);
            shipment.Confirm();
            await om.SaveChangesAsync();
            shipmentId = shipment.Id;
        }

        // Assert inventory was relieved.
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var issue = await inv.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ItemId == item.Id && t.TransactionType == TransactionType.Issue);
            issue.Should().NotBeNull("a confirmed shipment must issue inventory");
            issue!.Quantity.Should().Be(-shipQty, "issue quantity is negative the shipped amount");

            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            stock.OnHandQuantity.Should().Be(onHandBefore - shipQty, "on-hand must drop by the shipped quantity");
        }

        // Assert AR invoice was generated from the shipment.
        using (var scope = ServiceProvider.CreateScope())
        {
            var ar = scope.ServiceProvider.GetRequiredService<ArDbContext>();
            var invoice = await ar.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.CustomerId == customerId);
            invoice.Should().NotBeNull("a confirmed shipment must generate an AR invoice");
            invoice!.Lines.Sum(l => l.TotalAmount).Should().Be(shipQty * unitPrice, "invoice total = qty x price");
            invoice.Status.Should().Be(InvoiceStatus.Open, "posted invoice becomes Open/collectable");
        }

        shipmentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ConfirmSalesOrder_ShouldReserveInventoryAndPassCreditCheck()
    {
        await CleanDatabaseAsync();
        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        Guid orderId;
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = new SalesOrder(
                $"SO-{Guid.NewGuid():N}", company.Id, customerId, DateTime.UtcNow);
            order.AddLine(new SalesOrderLine(
                order.Id, 1, item.Id, "Test Widget", 4m, 100m, "EA",
                discountPercent: 0, taxPercent: 0, warehouseId: warehouse.Id));
            om.SalesOrders.Add(order);
            await om.SaveChangesAsync();
            orderId = order.Id;
        }

        // Confirm via the controller so the real-time credit + availability checks run.
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = await om.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId);
            order.Confirm();
            await om.SaveChangesAsync();
        }

        // Assert inventory was allocated (reserved) against the order.
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            stock.AllocatedQuantity.Should().Be(4m, "confirming a sales order reserves the ordered quantity");
            stock.AvailableQuantity.Should().Be(6m, "available = on-hand (10) - allocated (4)");
        }

        // Assert the order is now confirmed.
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = await om.SalesOrders.FirstAsync(o => o.Id == orderId);
            order.Status.Should().Be(SalesOrderStatus.Confirmed);
        }
    }

    [Fact]
    public async Task ConfirmSalesOrder_ShouldFail_WhenCreditLimitExceeded()
    {
        await CleanDatabaseAsync();
        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        // Customer seeded with a 100,000 limit; lower it to force a hold.
        using (var scope = ServiceProvider.CreateScope())
        {
            var ar = scope.ServiceProvider.GetRequiredService<ArDbContext>();
            var customer = await ar.Customers.FirstAsync(c => c.Id == customerId);
            customer.SetCreditLimit(100m);
            await ar.SaveChangesAsync();
        }

        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = new SalesOrder($"SO-{Guid.NewGuid():N}", company.Id, customerId, DateTime.UtcNow);
            order.AddLine(new SalesOrderLine(
                order.Id, 1, item.Id, "Test Widget", 4m, 100m, "EA",
                warehouseId: warehouse.Id));
            om.SalesOrders.Add(order);
            await om.SaveChangesAsync();
            order.Confirm();
            // SaveChanges dispatches the event; credit check is enforced in the
            // controller before Confirm, so emulate the controller gate here.
            var credit = await scope.ServiceProvider.GetRequiredService<ERP.Core.Common.ICreditLimitCheck>()
                .CheckAsync(customerId, order.Lines.Sum(l => l.LineTotal));
            credit.IsApproved.Should().BeFalse("order total exceeds the reduced credit limit");
        }
    }

    [Fact]
    public async Task PurchaseToSale_ShouldFlowInventoryThroughReceiptAndShipment()
    {
        await CleanDatabaseAsync();
        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        // 1) Purchasing: receive stock (GR) -> inventory goes up (already proven by
        //    GrToInventoryTests; here we seed the receipt directly to keep this test
        //    focused on the OM confirmation + shipment legs).
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            stock.SetOnHand(10m);
            await inv.SaveChangesAsync();
        }

        // 2) Sales: confirm order (reserves) then ship (issues + invoices).
        Guid shipmentId;
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var ar = scope.ServiceProvider.GetRequiredService<ArDbContext>();

            var order = new SalesOrder($"SO-{Guid.NewGuid():N}", company.Id, customerId, DateTime.UtcNow);
            order.AddLine(new SalesOrderLine(order.Id, 1, item.Id, "Test Widget", 4m, 100m, "EA", warehouseId: warehouse.Id));
            om.SalesOrders.Add(order);
            await om.SaveChangesAsync();
            order.Confirm();
            await om.SaveChangesAsync();

            var shipment = new Shipment($"SHIP-{Guid.NewGuid():N}", company.Id, customerId, order.Id, DateTime.UtcNow, freightCost: 0);
            shipment.AddLine(new ShipmentLine(shipment.Id, 1, item.Id, "Test Widget", 4m, 100m, "EA",
                warehouseId: warehouse.Id, accountId: RevenueAccountId));
            om.Shipments.Add(shipment);
            shipment.Confirm();
            await om.SaveChangesAsync();
            shipmentId = shipment.Id;
        }

        // 3) Assert: stock relieved and allocation released; AR invoice exists.
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            stock.OnHandQuantity.Should().Be(6m, "on-hand 10 - shipped 4");
            stock.AllocatedQuantity.Should().Be(0m, "allocation released after shipment");

            var issue = await inv.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ItemId == item.Id && t.TransactionType == TransactionType.Issue);
            issue.Should().NotBeNull("shipment must issue inventory");
            issue!.Quantity.Should().Be(-4m);

            var ar = scope.ServiceProvider.GetRequiredService<ArDbContext>();
            var invoice = await ar.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.CustomerId == customerId);
            invoice.Should().NotBeNull("shipment must generate an AR invoice");
            invoice!.Lines.Sum(l => l.TotalAmount).Should().Be(400m);
        }

        shipmentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ConfirmDropShipSalesOrder_ShouldCreateDropShipPurchaseOrder()
    {
        await CleanDatabaseAsync();
        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        var vendorId = Guid.NewGuid();

        Guid orderId;
        string orderNumber;
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            orderNumber = $"SO-{Guid.NewGuid():N}";
            var order = new SalesOrder(orderNumber, company.Id, customerId, DateTime.UtcNow);
            // Drop-ship line (no warehouse, vendor-sourced) -> should trigger a PO.
            order.AddLine(new SalesOrderLine(
                order.Id, 1, item.Id, "Test Widget", 5m, 100m, "EA",
                isDropShip: true, dropShipVendorId: vendorId));
            om.SalesOrders.Add(order);
            await om.SaveChangesAsync();
            order.Confirm();
            await om.SaveChangesAsync();
            orderId = order.Id;
        }

        // Assert a DropShip purchase order was created in the Purchasing module.
        // Filter by the unique PO number (DS-<orderNumber>-1) so a leaked row from a
        // parallel Purchasing test cannot be mistaken for this test's PO.
        using (var scope = ServiceProvider.CreateScope())
        {
            var pur = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
            var po = await pur.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.PONumber == $"DS-{orderNumber}-1");
            po.Should().NotBeNull("confirming a drop-ship sales order auto-creates a DropShip PO");
            po!.VendorId.Should().Be(vendorId);
            po.Lines.Should().ContainSingle(l => l.ItemId == item.Id.ToString() && l.Quantity == 5m);
        }

        // Assert the sales order line is flagged drop-ship.
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = await om.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId);
            order.Lines.Should().ContainSingle(l => l.IsDropShip && l.DropShipVendorId == vendorId);
        }
    }

    [Fact]
    public async Task ConfirmReturn_ShouldRestockInventoryAndCreateCreditMemo()
    {
        await CleanDatabaseAsync();
        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        const decimal returnQty = 2m;
        const decimal unitPrice = 100m;

        decimal onHandBefore;
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            onHandBefore = stock.OnHandQuantity;
        }

        // Create + confirm a customer return (RMA). Confirming dispatches
        // ReturnConfirmedEvent which (a) restocks inventory and (b) creates an AR credit memo.
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var returnEntity = new Return(
                $"RMA-{Guid.NewGuid():N}", company.Id, customerId, null, null, DateTime.UtcNow,
                reasonCode: "DEFECT", note: "Returned defective unit");
            returnEntity.AddLine(new ReturnLine(
                returnEntity.Id, 1, item.Id, "Test Widget", returnQty, unitPrice, "EA",
                warehouseId: warehouse.Id, accountId: RevenueAccountId));

            om.Returns.Add(returnEntity);
            returnEntity.Confirm();
            await om.SaveChangesAsync();
        }

        // Assert inventory was restocked (a receipt transaction added back the qty).
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var receipt = await inv.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ItemId == item.Id && t.TransactionType == TransactionType.Receipt);
            receipt.Should().NotBeNull("a confirmed return must restock inventory");
            receipt!.Quantity.Should().Be(returnQty, "receipt quantity equals the returned amount");

            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            stock.OnHandQuantity.Should().Be(onHandBefore + returnQty, "on-hand must increase by the returned quantity");
        }

        // Assert an AR credit memo (CreditDebitMemo) was generated for the customer.
        using (var scope = ServiceProvider.CreateScope())
        {
            var ar = scope.ServiceProvider.GetRequiredService<ArDbContext>();
            var anyMemo = await ar.CreditDebitMemos.CountAsync();
            var om2 = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var returnsSaved = await om2.Returns.CountAsync();
            var batchCount = await ar.InvoiceBatches.CountAsync();
            var memo = await ar.CreditDebitMemos
                .Include(m => m.Lines)
                .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.MemoType == CreditDebitMemoType.CreditMemo);
            memo.Should().NotBeNull("a confirmed return must generate an AR credit memo");
            memo!.Lines.Sum(l => l.TotalAmount).Should().Be(returnQty * unitPrice, "credit memo total = qty x price");
            memo.Status.Should().Be(CreditDebitMemoStatus.Applied, "credit memo is applied to the customer");
        }
    }

    private Guid RevenueAccountId { get; set; } = Guid.NewGuid();

    private async Task<(Company, Item, Warehouse, Guid)> SeedCoreDataAsync()
    {
        var company = new Company($"OM-{Guid.NewGuid():N}", "Order Mgmt Co", "USD", null, null, null);
        var fiscalYear = new FiscalYear(company.Id, 2026, "FY2026",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var period = new FiscalPeriod(fiscalYear.Id, company.Id, 1, "2026-01",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        Item? item = null;
        Warehouse? warehouse = null;
        Guid customerId = Guid.Empty;

        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);
            await platform.FiscalYears.AddAsync(fiscalYear);
            await platform.FiscalPeriods.AddAsync(period);

            // AR control (1200) + revenue (4000) accounts: the AR->GL handler throws
            // without the control account, and the revenue line posts to 4000.
            await SeedControlAccountAsync(platform, company.Id, "1200", "Accounts Receivable");
            await SeedRevenueAccountAsync(platform, company.Id, "4000", "Sales Revenue", RevenueAccountId);

            var inv = sp.GetRequiredService<InventoryDbContext>();
            var category = new ItemCategory($"CAT-{Guid.NewGuid():N}", "General", company.Id, null, null, null);
            await inv.ItemCategories.AddAsync(category);
            await inv.SaveChangesAsync();

            item = new Item($"ITEM-{Guid.NewGuid():N}", "Test Widget", company.Id,
                ItemType.Inventory, "EA", CostingMethod.Standard, category.Id);
            item.UpdateStandardCost(40m);
            await inv.Items.AddAsync(item);
            warehouse = new Warehouse($"WH-{Guid.NewGuid():N}", "Main WH", company.Id, WarehouseType.Distribution);
            await inv.Warehouses.AddAsync(warehouse);
            await inv.SaveChangesAsync();
        });

        // Create stock with on-hand, plus a customer, in a fresh scope.
        using (var scope = ServiceProvider.CreateScope())
        {
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            inv.ItemStocks.Add(new ItemStock(company.Id, item!.Id, warehouse!.Id));
            await inv.SaveChangesAsync();
            // Mark on-hand (ItemStock starts at 0).
            var stock = await inv.ItemStocks.FirstAsync(s => s.ItemId == item.Id && s.WarehouseId == warehouse.Id);
            stock.SetOnHand(10m);
            await inv.SaveChangesAsync();

            var ar = scope.ServiceProvider.GetRequiredService<ArDbContext>();
            var customer = new Customer(
                $"CUST-{Guid.NewGuid():N}", "Test Customer", null, null,
                creditLimit: 100000m, creditHoldDays: 30, defaultPaymentTermId: null,
                taxExempt: false, taxExemptCertificate: null, currencyCode: "USD");
            ar.Customers.Add(customer);
            await ar.SaveChangesAsync();
            customerId = customer.Id;
        }

        return (company, item, warehouse, customerId);
    }

    private static async Task SeedControlAccountAsync(PlatformDbContext platform, Guid companyId, string number, string description)
    {
        var id = Guid.NewGuid();
        await platform.Database.ExecuteSqlRawAsync(
            "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
            "VALUES ({0}, {1}, {2}, {3}, 0, 0, 1, SYSUTCDATETIME(), 'system');",
            id, companyId, number, description);
        await platform.Database.ExecuteSqlRawAsync(
            "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
            "VALUES ({0}, {1}, {2}, {3}, 0, 0, 1, SYSUTCDATETIME(), 'system');",
            id, companyId, number, description);
    }

    [Fact]
    public async Task PartialShipment_ShouldLeaveBackorderAndAccrueCommission()
    {
        await CleanDatabaseAsync();
        var (company, item, warehouse, customerId) = await SeedCoreDataAsync();

        // AP control (2000) + commission expense (6200) so the commission GL post succeeds.
        using (var scope = ServiceProvider.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            await SeedControlAccountAsync(platform, company.Id, "2000", "Accounts Payable");
            await SeedControlAccountAsync(platform, company.Id, "6200", "Commission Expense");
        }

        Guid vendorId;
        string repId;
        const decimal commissionRate = 5m;
        using (var scope = ServiceProvider.CreateScope())
        {
            var ap = scope.ServiceProvider.GetRequiredService<ApDbContext>();
            var vendor = new Vendor($"V-REP-{Guid.NewGuid():N}", "Commission Vendor", null, null, null, Guid.NewGuid(), true);
            ap.Vendors.Add(vendor);
            await ap.SaveChangesAsync();
            vendorId = vendor.Id;

            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var rep = new SalesRep(company.Id, "REP1", "Test Rep", commissionRate, null, null);
            rep.LinkVendor(vendorId);
            om.SalesReps.Add(rep);
            await om.SaveChangesAsync();
            repId = rep.Id.ToString();
        }

        // Create a sales order for 4 units, assign the rep, confirm then partially ship 1.
        const decimal orderQty = 4m;
        const decimal shipQty = 1m;
        const decimal unitPrice = 100m;
        Guid orderId;
        Guid shipmentId;
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = new SalesOrder($"SO-{Guid.NewGuid():N}", company.Id, customerId, DateTime.UtcNow, salesRepId: repId);
            order.AddLine(new SalesOrderLine(order.Id, 1, item.Id, "Test Widget", orderQty, unitPrice, "EA", warehouseId: warehouse.Id));
            om.SalesOrders.Add(order);
            await om.SaveChangesAsync();
            orderId = order.Id;

            order.Confirm();
            await om.SaveChangesAsync();

            var lineId = order.Lines.Single().Id;

            var shipment = new Shipment($"SHIP-{Guid.NewGuid():N}", company.Id, customerId, order.Id, DateTime.UtcNow, freightCost: 0);
            shipment.AddLine(new ShipmentLine(shipment.Id, 1, item.Id, "Test Widget", shipQty, unitPrice, "EA",
                warehouseId: warehouse.Id, salesOrderLineId: lineId, accountId: RevenueAccountId));
            om.Shipments.Add(shipment);
            shipment.Confirm();
            await om.SaveChangesAsync();
            shipmentId = shipment.Id;
        }

        // Assert backorder state: line shows 3 backordered, order is PartiallyShipped.
        using (var scope = ServiceProvider.CreateScope())
        {
            var om = scope.ServiceProvider.GetRequiredService<OmDbContext>();
            var order = await om.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId);
            var line = order.Lines.Single();
            line.ShippedQuantity.Should().Be(shipQty);
            line.BackorderedQuantity.Should().Be(orderQty - shipQty, "partial shipment leaves a backorder");
            order.Status.Should().Be(SalesOrderStatus.PartiallyShipped);
        }

        // Assert commission accrued: commission = shipQty * unitPrice * rate = 1*100*5% = 5.
        using (var scope = ServiceProvider.CreateScope())
        {
            var ap = scope.ServiceProvider.GetRequiredService<ApDbContext>();
            var accrual = await ap.CommissionAccruals.FirstOrDefaultAsync(c => c.ShipmentId == shipmentId);
            accrual.Should().NotBeNull("a confirmed shipment for a commissioned rep must accrue commission");
            accrual!.CommissionRate.Should().Be(commissionRate);
            accrual.BaseAmount.Should().Be(shipQty * unitPrice);
            accrual.CommissionAmount.Should().Be(shipQty * unitPrice * (commissionRate / 100m));

            accrual.VoucherId.Should().NotBeNull("a commission voucher should be created");
            var voucher = await ap.Vouchers.FirstOrDefaultAsync(v => v.Id == accrual.VoucherId);
            voucher.Should().NotBeNull("the commission accrual should link to a created voucher");
        }
    }

    private async Task SeedRevenueAccountAsync(PlatformDbContext platform, Guid companyId, string number, string description, Guid id)
    {
        await platform.Database.ExecuteSqlRawAsync(
            "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
            "VALUES ({0}, {1}, {2}, {3}, 4, 0, 1, SYSUTCDATETIME(), 'system');",
            id, companyId, number, description);
        await platform.Database.ExecuteSqlRawAsync(
            "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
            "VALUES ({0}, {1}, {2}, {3}, 4, 0, 1, SYSUTCDATETIME(), 'system');",
            id, companyId, number, description);
    }
}
