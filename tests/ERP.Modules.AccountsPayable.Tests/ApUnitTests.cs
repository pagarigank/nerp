using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.AccountsPayable.Tests;

public class ApUnitTests
{
    [Fact]
    public async Task ThreeWayMatchWithExactQuantitiesAndPriceReturnsValid()
    {
        var service = new ThreeWayMatchService();
        var line = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-001",
            "Test Item",
            10,
            10,
            10,
            50.00m,
            500.00m,
            0.05m);

        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-001",
            [line],
            500.00m);

        var result = await service.ValidateMatchAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.HasQuantityVariance.Should().BeFalse();
        result.HasPriceVariance.Should().BeFalse();
    }

    [Fact]
    public async Task ThreeWayMatchWithPriceVarianceExceedingToleranceReturnsInvalid()
    {
        var service = new ThreeWayMatchService();
        var line = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-001",
            "Test Item",
            10,
            10,
            10,
            50.00m,
            600.00m,
            0.05m);

        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-002",
            [line],
            600.00m);

        var result = await service.ValidateMatchAsync(request);

        result.IsValid.Should().BeFalse();
        result.HasPriceVariance.Should().BeTrue();
    }

    [Fact]
    public async Task ThreeWayMatchWithQuantityVarianceExceedingToleranceReturnsInvalid()
    {
        var service = new ThreeWayMatchService();
        var line = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-001",
            "Test Item",
            10,
            5,
            10,
            50.00m,
            500.00m,
            0.05m);

        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-003",
            [line],
            500.00m);

        var result = await service.ValidateMatchAsync(request);

        result.IsValid.Should().BeFalse();
        result.HasQuantityVariance.Should().BeTrue();
    }

    [Fact]
    public async Task ThreeWayMatchPriceVarianceWithinToleranceReturnsWarning()
    {
        var service = new ThreeWayMatchService();
        var line = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-001",
            "Test Item",
            10,
            10,
            10,
            50.00m,
            502.00m,
            0.05m);

        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-004",
            [line],
            502.00m);

        var result = await service.ValidateMatchAsync(request);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Price variance"));
    }

    [Fact]
    public void DiscountCalculationRespectsPaymentTerms()
    {
        var term = new PaymentTerm("2%10-Net30", 30, 10, 0.02m);

        term.DueDays.Should().Be(30);
        term.DiscountDays.Should().Be(10);
        term.DiscountPercent.Should().Be(0.02m);

        var invoiceAmount = 1000.00m;
        var discount = invoiceAmount * term.DiscountPercent;
        discount.Should().Be(20.00m);
    }

    [Fact]
    public void VoucherBatchBalancedValidationWorks()
    {
        var batch = new VoucherBatch(
            Guid.NewGuid(),
            "VB-001",
            "Test batch",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var voucher = batch.AddVoucher(
            Guid.NewGuid(),
            VoucherType.Invoice,
            "INV-001",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            500.00m,
            0,
            null,
            null);

        voucher.AddDistribution(Guid.NewGuid(), 500.00m, null, null, null);
        voucher.AddDistribution(Guid.NewGuid(), null, 500.00m, null, null);

        batch.Release();
        batch.Status.Should().Be(VoucherBatchStatus.Batched);
    }

    [Fact]
    public void VoucherBatchUnbalancedThrowsOnRelease()
    {
        var batch = new VoucherBatch(
            Guid.NewGuid(),
            "VB-002",
            "Unbalanced batch",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var voucher = batch.AddVoucher(
            Guid.NewGuid(),
            VoucherType.Invoice,
            "INV-002",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            500.00m,
            0,
            null,
            null);

        voucher.AddDistribution(Guid.NewGuid(), 500.00m, null, null, null);

        var act = () => batch.Release();

        act.Should().Throw<InvalidOperationException>().WithMessage("*balanced*");
    }

    [Fact]
    public void VoucherStatusTransitionsCorrectly()
    {
        var batch = new VoucherBatch(
            Guid.NewGuid(),
            "VB-003",
            "Status test",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
        var vendorId = Guid.NewGuid();

        var voucher = batch.AddVoucher(
            vendorId,
            VoucherType.Invoice,
            "INV-003",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            250.00m,
            5.00m,
            null,
            null);

        voucher.AddDistribution(Guid.NewGuid(), 250.00m, null, null, null);
        voucher.AddDistribution(Guid.NewGuid(), null, 250.00m, null, null);

        batch.Release();
        batch.Status.Should().Be(VoucherBatchStatus.Batched);

        batch.Post();
        batch.Status.Should().Be(VoucherBatchStatus.Posted);

        var reversal = batch.Reverse("Test void");
        reversal.Should().NotBeNull();
        reversal.Status.Should().Be(VoucherBatchStatus.Draft);
        batch.Status.Should().Be(VoucherBatchStatus.Reversed);
    }

    [Fact]
    public void PaymentStatusTransitionsCorrectly()
    {
        var batch = new VoucherBatch(
            Guid.NewGuid(),
            "VB-PMT",
            "Payment status test",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var voucher = batch.AddVoucher(
            Guid.NewGuid(),
            VoucherType.Invoice,
            "INV-999",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            100.00m,
            0,
            null,
            null);

        voucher.AddDistribution(Guid.NewGuid(), 100.00m, null, null, null);
        voucher.AddDistribution(Guid.NewGuid(), null, 100.00m, null, null);

        var payment = new Payment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PMT-001",
            DateTimeOffset.UtcNow,
            PaymentMethod.ACH,
            "USD",
            null);

        payment.Status.Should().Be(PaymentStatus.Selected);

        var actPreIssue = () => payment.Clear();
        actPreIssue.Should().Throw<InvalidOperationException>().WithMessage("*Issued payment*");

        voucher.MarkSelectedForPayment();
        payment.AddVoucher(voucher, 100.00m);

        payment.Issue();
        payment.Status.Should().Be(PaymentStatus.Issued);

        payment.Clear();
        payment.Status.Should().Be(PaymentStatus.Cleared);
    }
}
