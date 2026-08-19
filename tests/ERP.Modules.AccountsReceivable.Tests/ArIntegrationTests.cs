using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.AccountsReceivable.Tests;

public class ArIntegrationTests
{
    private static ArDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ArDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ArDbContext(options);
    }

    [Fact]
    public async Task EndToEndInvoiceToCashApplicationFlow()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var batch = new InvoiceBatch(companyId, "BATCH-E2E", "E2E test batch", DateTimeOffset.UtcNow, Guid.NewGuid());
        context.InvoiceBatches.Add(batch);

        var invoice = batch.AddInvoice(customerId, "INV-E2E-001", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), "E2E invoice", null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Consulting", 10, 150.00m, 0, 0);
        invoice.AddLine(Guid.NewGuid(), "Materials", 5, 200.00m, 25.00m, 0);

        invoice.TotalAmount.Should().Be((10 * 150.00m) + (5 * 200.00m) + 25.00m);
        var expectedTotal = (10 * 150.00m) + (5 * 200.00m) + 25.00m;
        invoice.BalanceDue.Should().Be(expectedTotal);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var receipt = new CashReceipt(companyId, customerId, "CR-E2E-001", expectedTotal, DateTimeOffset.UtcNow, "Wire", "USD", null);
        context.CashReceipts.Add(receipt);
        await context.SaveChangesAsync();

        var savedInvoice = await context.Invoices
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoice.Id);

        receipt.ApplyToInvoice(savedInvoice, expectedTotal);
        await context.SaveChangesAsync();

        receipt.Status.Should().Be(CashReceiptStatus.FullyApplied);
        savedInvoice.Status.Should().Be(InvoiceStatus.Paid);
        savedInvoice.BalanceDue.Should().Be(0);

        var applications = await context.CashReceiptApplications
            .Where(a => a.CashReceiptId == receipt.Id)
            .ToListAsync();

        applications.Should().HaveCount(1);
        applications[0].AppliedAmount.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task CustomerTrialBalanceTiesToInvoices()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var batch = new InvoiceBatch(companyId, "BATCH-TB", "TB test", DateTimeOffset.UtcNow, Guid.NewGuid());
        context.InvoiceBatches.Add(batch);

        var inv1 = batch.AddInvoice(customerId, "INV-TB-001", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        inv1.AddLine(Guid.NewGuid(), "Service A", 1, 1000, 0, 0);

        var inv2 = batch.AddInvoice(customerId, "INV-TB-002", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        inv2.AddLine(Guid.NewGuid(), "Service B", 1, 2500, 0, 0);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var receipt = new CashReceipt(companyId, customerId, "CR-TB-001", 500, DateTimeOffset.UtcNow, "Check", "USD", null);
        context.CashReceipts.Add(receipt);

        var savedInv1 = await context.Invoices.FirstAsync(i => i.Id == inv1.Id);
        receipt.ApplyToInvoice(savedInv1, 500);
        await context.SaveChangesAsync();

        var customerInvoices = await context.Invoices
            .Where(i => i.CustomerId == customerId)
            .ToListAsync();

        var totalInvoiced = customerInvoices.Sum(i => i.TotalAmount);
        totalInvoiced.Should().Be(3500);

        var totalPaid = customerInvoices.Sum(i => i.TotalAmount - i.BalanceDue);
        totalPaid.Should().Be(500);

        var unpaidBalance = customerInvoices.Sum(i => i.BalanceDue);
        unpaidBalance.Should().Be(3000);
    }
}
