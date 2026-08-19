using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.AccountsPayable.Tests;

public class BackupWithholdingServiceTests
{
    private static ApDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApDbContext(options);
    }

    [Fact]
    public async Task VendorWithoutWithholdingFlagReturnsNoWithholding()
    {
        using var context = CreateContext();
        var vendor = new Vendor("V-001", "Test Vendor", null, "12-3456789", null, null, true);
        context.Vendors.Add(vendor);
        await context.SaveChangesAsync();

        var service = new BackupWithholdingService(context);
        var result = await service.CalculateWithholdingAsync(vendor.Id, 1000.00m);

        result.IsSubjectToWithholding.Should().BeFalse();
        result.WithholdingAmount.Should().Be(0m);
        result.NetPaymentAmount.Should().Be(1000.00m);
    }

    [Fact]
    public async Task VendorWithWithholdingFlagCalculatesCorrectAmount()
    {
        using var context = CreateContext();
        var vendor = new Vendor("V-002", "Withholding Vendor", null, "98-7654321", Vendor1099Category.IndependentContractor, null, true, backupWithholdingFlag: true, backupWithholdingRate: 0.24m);
        context.Vendors.Add(vendor);
        await context.SaveChangesAsync();

        var service = new BackupWithholdingService(context);
        var result = await service.CalculateWithholdingAsync(vendor.Id, 1000.00m);

        result.IsSubjectToWithholding.Should().BeTrue();
        result.WithholdingRate.Should().Be(0.24m);
        result.WithholdingAmount.Should().Be(240.00m);
        result.NetPaymentAmount.Should().Be(760.00m);
    }

    [Fact]
    public async Task CustomWithholdingRateIsUsed()
    {
        using var context = CreateContext();
        var vendor = new Vendor("V-003", "Custom Rate Vendor", null, "12-3456789", Vendor1099Category.Rent, null, true, backupWithholdingFlag: true, backupWithholdingRate: 0.10m);
        context.Vendors.Add(vendor);
        await context.SaveChangesAsync();

        var service = new BackupWithholdingService(context);
        var result = await service.CalculateWithholdingAsync(vendor.Id, 500.00m);

        result.WithholdingRate.Should().Be(0.10m);
        result.WithholdingAmount.Should().Be(50.00m);
        result.NetPaymentAmount.Should().Be(450.00m);
    }

    [Fact]
    public async Task ZeroPaymentAmountReturnsZeroWithholding()
    {
        using var context = CreateContext();
        var vendor = new Vendor("V-004", "Zero Payment Vendor", null, "12-3456789", Vendor1099Category.NonEmployeeCompensation, null, true, backupWithholdingFlag: true, backupWithholdingRate: 0.24m);
        context.Vendors.Add(vendor);
        await context.SaveChangesAsync();

        var service = new BackupWithholdingService(context);
        var result = await service.CalculateWithholdingAsync(vendor.Id, 0m);

        result.WithholdingAmount.Should().Be(0m);
        result.NetPaymentAmount.Should().Be(0m);
    }

    [Fact]
    public async Task NonExistentVendorThrowsException()
    {
        using var context = CreateContext();
        var service = new BackupWithholdingService(context);

        var act = async () => await service.CalculateWithholdingAsync(Guid.NewGuid(), 1000m);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }
}
