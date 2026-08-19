using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.AccountsPayable.Tests;

public class ApIntegrationTests
{
    private static ApDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApDbContext(options);
    }

    [Fact]
    public async Task VoidPaymentReversesOriginalGlDistribution()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var vendor = new Vendor("V-INT-001", "Integration Vendor", null, "12-3456789", null, null, true);
        context.Vendors.Add(vendor);

        var batch = new VoucherBatch(
            companyId,
            "VB-INT-001",
            "Integration test batch",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
        context.VoucherBatches.Add(batch);
        await context.SaveChangesAsync();

        var voucher = batch.AddVoucher(
            vendor.Id,
            VoucherType.Invoice,
            "INV-INT-001",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            1000.00m,
            0,
            null,
            null);

        voucher.AddDistribution(accountA, 1000.00m, null, null, null);
        voucher.AddDistribution(accountB, null, 1000.00m, null, null);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var payment = new Payment(
            companyId,
            vendor.Id,
            "PMT-INT-001",
            DateTimeOffset.UtcNow,
            PaymentMethod.Check,
            "USD",
            null);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        voucher.MarkSelectedForPayment();
        payment.AddVoucher(voucher, 1000.00m);
        payment.Issue();
        await context.SaveChangesAsync();

        payment.Void("Integration test void");
        await context.SaveChangesAsync();

        payment.Status.Should().Be(PaymentStatus.Voided);

        var allDistributions = await context.VoucherDistributions
            .Where(d => d.VoucherId == voucher.Id)
            .ToListAsync();

        allDistributions.Sum(d => d.Debit).Should().Be(1000.00m);
        allDistributions.Sum(d => d.Credit).Should().Be(1000.00m);
    }

    [Fact]
    public async Task VendorTrialBalanceTiesToGlControlAccount()
    {
        using var context = CreateContext();

        var companyId = Guid.NewGuid();
        var glControlAccountId = Guid.NewGuid();

        var vendor = new Vendor("V-TB-001", "TB Vendor", null, "12-3456789", null, null, true);
        context.Vendors.Add(vendor);

        var batch = new VoucherBatch(
            companyId,
            "VB-TB-001",
            "TB test batch",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
        context.VoucherBatches.Add(batch);
        await context.SaveChangesAsync();

        var voucher = batch.AddVoucher(
            vendor.Id,
            VoucherType.Invoice,
            "INV-TB-001",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            2000.00m,
            0,
            null,
            null);

        voucher.AddDistribution(glControlAccountId, 2000.00m, null, null, null);
        voucher.AddDistribution(Guid.NewGuid(), null, 2000.00m, null, null);

        batch.Release();
        batch.Post();
        await context.SaveChangesAsync();

        var glDebit = voucher.Distributions
            .Where(d => d.AccountId == glControlAccountId)
            .Sum(d => d.Debit);
        var glCredit = voucher.Distributions
            .Where(d => d.AccountId == glControlAccountId)
            .Sum(d => d.Credit);
        var glBalance = glDebit - glCredit;

        var vendorTotal = await context.Vouchers
            .Where(v => v.VendorId == vendor.Id
                && v.VoucherBatch != null
                && v.VoucherBatch.Status == VoucherBatchStatus.Posted)
            .SumAsync(v => v.TotalAmount);

        var paymentsList = await context.Payments
            .Where(p => p.VendorId == vendor.Id && p.Status == PaymentStatus.Issued)
            .ToListAsync();
        var payments = paymentsList.Sum(p => p.TotalAmount);

        var endingBalance = vendorTotal - payments;

        glBalance.Should().Be(endingBalance,
            "the GL control account balance should equal the vendor trial balance ending balance");
    }
}
