using ERP.Modules.AccountsPayable.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.AccountsPayable.Tests;

public class ThreeWayMatchServiceTests
{
    private readonly ThreeWayMatchService _service = new ThreeWayMatchService();

    [Fact]
    public async Task ValidMatchReturnsIsValidTrue()
    {
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
            new List<ThreeWayMatchLine> { line },
            500.00m);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task InvoiceExceedsReceivedQuantityOverToleranceReturnsError()
    {
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
            "INV-002",
            new List<ThreeWayMatchLine> { line },
            500.00m);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeFalse();
        result.HasQuantityVariance.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Invoice quantity") && e.Contains("exceeds received quantity"));
    }

    [Fact]
    public async Task InvoiceExceedsOrderedQuantityOverToleranceReturnsError()
    {
        var line = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-001",
            "Test Item",
            10,
            15,
            15,
            50.00m,
            750.00m,
            0.05m);

        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-003",
            new List<ThreeWayMatchLine> { line },
            750.00m);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeFalse();
        result.HasQuantityVariance.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Invoice quantity") && e.Contains("exceeds ordered quantity"));
    }

    [Fact]
    public async Task PriceVarianceOverToleranceReturnsError()
    {
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
            "INV-004",
            new List<ThreeWayMatchLine> { line },
            600.00m);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeFalse();
        result.HasPriceVariance.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Price variance"));
    }

    [Fact]
    public async Task MultipleLinesWithOneFailingReturnsIsValidFalse()
    {
        var goodLine = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-001",
            "Good Item",
            5,
            5,
            5,
            100.00m,
            500.00m,
            0.05m);

        var badLine = new ThreeWayMatchLine(
            Guid.NewGuid(),
            "ITEM-002",
            "Bad Item",
            10,
            10,
            12,
            25.00m,
            300.00m,
            0.05m);

        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-005",
            new List<ThreeWayMatchLine> { goodLine, badLine },
            800.00m);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task PriceVarianceWithinToleranceReturnsWarning()
    {
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
            "INV-006",
            new List<ThreeWayMatchLine> { line },
            502.00m);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(e => e.Contains("Price variance"));
    }

    [Fact]
    public async Task EmptyLineListReturnsIsValidTrue()
    {
        var request = new ThreeWayMatchRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-007",
            new List<ThreeWayMatchLine>(),
            0);

        var result = await _service.ValidateMatchAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullRequestThrowsArgumentNullException()
    {
        var act = async () => await _service.ValidateMatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
