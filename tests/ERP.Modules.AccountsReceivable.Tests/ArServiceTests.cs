using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.AccountsReceivable.Tests;

public class ArServiceTests
{
    private static ArDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ArDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new ArDbContext(options);
    }

    private static InvoiceBatch CreatePostedBatch(ArDbContext context, Guid companyId, string batchNumber)
    {
        var batch = new InvoiceBatch(companyId, batchNumber, "Test batch", DateTimeOffset.UtcNow, Guid.NewGuid());
        context.InvoiceBatches.Add(batch);
        return batch;
    }

    [Fact]
    public async Task AutoCashApplicationAppliesToOldestInvoiceFirst()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var batch = CreatePostedBatch(context, companyId, "BATCH-AC-001");
        var olderInvoice = batch.AddInvoice(customerId, "INV-AC-001", DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-10), null, null, null, null);
        olderInvoice.AddLine(Guid.NewGuid(), "Item", 1, 400, 0, 0);

        var newerInvoice = batch.AddInvoice(customerId, "INV-AC-002", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(20), null, null, null, null);
        newerInvoice.AddLine(Guid.NewGuid(), "Item", 1, 300, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var receipt = new CashReceipt(companyId, customerId, "CR-AC-001", 500, DateTimeOffset.UtcNow, "Check", "USD", null);
        context.CashReceipts.Add(receipt);
        await context.SaveChangesAsync();

        var service = new AutoCashApplicationService(context);
        var applications = await service.AutoApplyAsync(receipt.Id);

        applications.Should().HaveCount(2);
        applications[0].InvoiceId.Should().Be(olderInvoice.Id);
        applications[0].AppliedAmount.Should().Be(400);
        applications[1].InvoiceId.Should().Be(newerInvoice.Id);
        applications[1].AppliedAmount.Should().Be(100);

        receipt.Status.Should().Be(CashReceiptStatus.FullyApplied);
        receipt.UnappliedAmount.Should().Be(0);
    }

    [Fact]
    public async Task AppliedPaymentPersistsTotalPaidAndBalanceAcrossReload()
    {
        var databaseName = Guid.NewGuid().ToString();

        using (var context = CreateContext(databaseName))
        {
            var companyId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var batch = CreatePostedBatch(context, companyId, "BATCH-PERSIST-001");
            var invoice = batch.AddInvoice(customerId, "INV-PERSIST-001", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
            invoice.AddLine(Guid.NewGuid(), "Item", 1, 1000, 0, 0);

            batch.Release();
            batch.Post();
            await context.SaveChangesAsync();

            var receipt = new CashReceipt(companyId, customerId, "CR-PERSIST-001", 400, DateTimeOffset.UtcNow, "Check", "USD", null);
            context.CashReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var savedInvoice = await context.Invoices.FirstAsync(i => i.Id == invoice.Id);
            receipt.ApplyToInvoice(savedInvoice, 400);
            await context.SaveChangesAsync();
        }

        using (var reloaded = CreateContext(databaseName))
        {
            var reloadedInvoice = await reloaded.Invoices.Include(i => i.Lines).FirstAsync(i => i.InvoiceNumber == "INV-PERSIST-001");
            reloadedInvoice.TotalPaid.Should().Be(400);
            reloadedInvoice.BalanceDue.Should().Be(600);
            reloadedInvoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        }
    }

    [Fact]
    public async Task FullPaymentPersistsPaidStatusAcrossReload()
    {
        var databaseName = Guid.NewGuid().ToString();

        using (var context = CreateContext(databaseName))
        {
            var companyId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var batch = CreatePostedBatch(context, companyId, "BATCH-PERSIST-002");
            var invoice = batch.AddInvoice(customerId, "INV-PERSIST-002", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
            invoice.AddLine(Guid.NewGuid(), "Item", 1, 1000, 0, 0);

            batch.Release();
            batch.Post();
            await context.SaveChangesAsync();

            var receipt = new CashReceipt(companyId, customerId, "CR-PERSIST-002", 1000, DateTimeOffset.UtcNow, "Check", "USD", null);
            context.CashReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var savedInvoice = await context.Invoices.FirstAsync(i => i.Id == invoice.Id);
            receipt.ApplyToInvoice(savedInvoice, 1000);
            await context.SaveChangesAsync();
        }

        using (var reloaded = CreateContext(databaseName))
        {
            var reloadedInvoice = await reloaded.Invoices.Include(i => i.Lines).FirstAsync(i => i.InvoiceNumber == "INV-PERSIST-002");
            reloadedInvoice.TotalPaid.Should().Be(1000);
            reloadedInvoice.BalanceDue.Should().Be(0);
            reloadedInvoice.Status.Should().Be(InvoiceStatus.Paid);
        }
    }

    [Fact]
    public async Task UnappliedPaymentRestoresBalanceAcrossReload()
    {
        var databaseName = Guid.NewGuid().ToString();

        using (var context = CreateContext(databaseName))
        {
            var companyId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var batch = CreatePostedBatch(context, companyId, "BATCH-PERSIST-003");
            var invoice = batch.AddInvoice(customerId, "INV-PERSIST-003", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
            invoice.AddLine(Guid.NewGuid(), "Item", 1, 1000, 0, 0);

            batch.Release();
            batch.Post();
            await context.SaveChangesAsync();

            var receipt = new CashReceipt(companyId, customerId, "CR-PERSIST-003", 400, DateTimeOffset.UtcNow, "Check", "USD", null);
            context.CashReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var savedInvoice = await context.Invoices.FirstAsync(i => i.Id == invoice.Id);
            receipt.ApplyToInvoice(savedInvoice, 400);
            await context.SaveChangesAsync();
        }

        using (var unapplyContext = CreateContext(databaseName))
        {
            var unapplyInvoice = await unapplyContext.Invoices.Include(i => i.Lines).FirstAsync(i => i.InvoiceNumber == "INV-PERSIST-003");
            var unapplyReceipt = await unapplyContext.CashReceipts.Include(r => r.Applications).FirstAsync(r => r.ReceiptReference == "CR-PERSIST-003");
            var application = unapplyReceipt.Applications.Single();
            unapplyReceipt.UnapplyInvoice(unapplyInvoice, application);
            unapplyContext.CashReceiptApplications.Remove(application);
            await unapplyContext.SaveChangesAsync();
        }

        using (var reloaded = CreateContext(databaseName))
        {
            var reloadedInvoice = await reloaded.Invoices.Include(i => i.Lines).FirstAsync(i => i.InvoiceNumber == "INV-PERSIST-003");
            reloadedInvoice.TotalPaid.Should().Be(0);
            reloadedInvoice.BalanceDue.Should().Be(1000);
            reloadedInvoice.Status.Should().Be(InvoiceStatus.Open);
        }
    }

    [Fact]
    public async Task AutoCashApplicationLeavesUnappliedAmountWhenReceiptIsSmall()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var batch = CreatePostedBatch(context, companyId, "BATCH-AC-002");
        var invoice = batch.AddInvoice(customerId, "INV-AC-003", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Item", 1, 1000, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var receipt = new CashReceipt(companyId, customerId, "CR-AC-002", 250, DateTimeOffset.UtcNow, "Check", "USD", null);
        context.CashReceipts.Add(receipt);
        await context.SaveChangesAsync();

        var service = new AutoCashApplicationService(context);
        var applications = await service.AutoApplyAsync(receipt.Id);

        applications.Should().HaveCount(1);
        applications[0].AppliedAmount.Should().Be(250);
        receipt.Status.Should().Be(CashReceiptStatus.FullyApplied);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.BalanceDue.Should().Be(750);
    }

    [Fact]
    public async Task FinanceChargeServiceCalculatesMonthlyRateOnOverdueBalance()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var annualRate = 12.0m;

        var customer = new Customer("C-FC-001", "FC Customer", null, null, 10000, 0, null, false, null, "USD");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var batch = CreatePostedBatch(context, companyId, "BATCH-FC-001");
        var overdueInvoice = batch.AddInvoice(customer.Id, "INV-FC-001", DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(-30), null, null, null, null);
        overdueInvoice.AddLine(Guid.NewGuid(), "Item", 1, 1000, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var asOfDate = DateTimeOffset.UtcNow;
        var service = new FinanceChargeService(context);
        var charges = await service.CalculateChargesAsync(companyId, annualRate, asOfDate);

        charges.Should().HaveCount(1);
        var monthlyRate = annualRate / 12 / 100;
        charges[0].ChargeAmount.Should().Be(Math.Round(1000m * monthlyRate, 2));
        charges[0].CustomerId.Should().Be(customer.Id);
        charges[0].Status.Should().Be(FinanceChargeStatus.Open);
    }

    [Fact]
    public async Task FinanceChargeServiceSkipsCustomerWithNoOverdueBalance()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();

        var customer = new Customer("C-FC-002", "FC Customer 2", null, null, 10000, 0, null, false, null, "USD");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var batch = CreatePostedBatch(context, companyId, "BATCH-FC-002");
        var notOverdue = batch.AddInvoice(customer.Id, "INV-FC-002", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        notOverdue.AddLine(Guid.NewGuid(), "Item", 1, 1000, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var service = new FinanceChargeService(context);
        var charges = await service.CalculateChargesAsync(companyId, 18.0m, DateTimeOffset.UtcNow);

        charges.Should().BeEmpty();
    }

    [Fact]
    public async Task StatementGenerationServiceCreatesStatementForCustomersWithOpenBalance()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();

        var customer = new Customer("C-ST-001", "Statement Customer", null, null, 5000, 0, null, false, null, "USD");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var batch = CreatePostedBatch(context, companyId, "BATCH-ST-001");
        var invoice = batch.AddInvoice(customer.Id, "INV-ST-001", DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(20), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Item", 1, 750, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var service = new StatementGenerationService(context);
        var statements = await service.GenerateStatementsAsync(companyId, DateTimeOffset.UtcNow);

        statements.Should().HaveCount(1);
        statements[0].CustomerId.Should().Be(customer.Id);
        statements[0].Status.Should().Be(StatementStatus.Generated);
        statements[0].StatementNumber.Should().StartWith("STMT-");
    }

    [Fact]
    public async Task StatementGenerationServiceSkipsCustomerWithPaidBalance()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();

        var customer = new Customer("C-ST-002", "Paid Customer", null, null, 5000, 0, null, false, null, "USD");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var batch = CreatePostedBatch(context, companyId, "BATCH-ST-002");
        var invoice = batch.AddInvoice(customer.Id, "INV-ST-002", DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(20), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Item", 1, 500, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var receipt = new CashReceipt(companyId, customer.Id, "CR-ST-001", 500, DateTimeOffset.UtcNow, "Check", "USD", null);
        context.CashReceipts.Add(receipt);
        await context.SaveChangesAsync();

        var savedInvoice = await context.Invoices.FirstAsync(i => i.Id == invoice.Id);
        receipt.ApplyToInvoice(savedInvoice, 500);
        await context.SaveChangesAsync();

        var service = new StatementGenerationService(context);
        var statements = await service.GenerateStatementsAsync(companyId, DateTimeOffset.UtcNow);

        statements.Should().BeEmpty();
    }
}
