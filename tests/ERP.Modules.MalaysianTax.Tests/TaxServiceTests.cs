// <copyright file="TaxServiceTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.MalaysianTax.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.Modules.MalaysianTax.Tests;

public class TaxServiceTests
{
    private readonly Mock<ITaxRateRepository> _taxRateRepoMock;
    private readonly Mock<ITaxCodeRepository> _taxCodeRepoMock;
    private readonly Mock<ITaxDeterminationRuleRepository> _ruleRepoMock;
    private readonly Mock<ILogger<TaxService>> _loggerMock;
    private readonly TaxService _sut;

    public TaxServiceTests()
    {
        _taxRateRepoMock = new Mock<ITaxRateRepository>();
        _taxCodeRepoMock = new Mock<ITaxCodeRepository>();
        _ruleRepoMock = new Mock<ITaxDeterminationRuleRepository>();
        _loggerMock = new Mock<ILogger<TaxService>>();
        _sut = new TaxService(
            _taxRateRepoMock.Object,
            _taxCodeRepoMock.Object,
            _ruleRepoMock.Object,
            _loggerMock.Object);
    }

    // ===== CalculateTax Tests =====

    [Fact]
    public void CalculateTax_ExclusiveMode_10Percent_CalculatesCorrectly()
    {
        var result = _sut.CalculateTax(100m, 10m, PricingMode.Exclusive);

        result.NetAmount.Should().Be(100m);
        result.TaxAmount.Should().Be(10m);
        result.GrossAmount.Should().Be(110m);
        result.RatePercent.Should().Be(10m);
    }

    [Fact]
    public void CalculateTax_InclusiveMode_10Percent_CalculatesCorrectly()
    {
        var result = _sut.CalculateTax(110m, 10m, PricingMode.Inclusive);

        result.NetAmount.Should().Be(100m);
        result.TaxAmount.Should().Be(10m);
        result.GrossAmount.Should().Be(110m);
        result.RatePercent.Should().Be(10m);
    }

    [Fact]
    public void CalculateTax_ZeroRate_ReturnsSameAmount()
    {
        var result = _sut.CalculateTax(250m, 0m, PricingMode.Exclusive);

        result.NetAmount.Should().Be(250m);
        result.TaxAmount.Should().Be(0m);
        result.GrossAmount.Should().Be(250m);
        result.RatePercent.Should().Be(0m);
    }

    [Fact]
    public void CalculateTax_ExclusiveMode_8Percent_RoundsHalfUp()
    {
        var result = _sut.CalculateTax(100m, 8m, PricingMode.Exclusive);

        result.NetAmount.Should().Be(100m);
        result.TaxAmount.Should().Be(8m);
        result.GrossAmount.Should().Be(108m);
    }

    [Fact]
    public void CalculateTax_InclusiveMode_6Percent_RoundsCorrectly()
    {
        var result = _sut.CalculateTax(106m, 6m, PricingMode.Inclusive);

        result.NetAmount.Should().Be(100m);
        result.TaxAmount.Should().Be(6m);
        result.GrossAmount.Should().Be(106m);
    }

    [Fact]
    public void CalculateTax_ExclusiveMode_HandlesRoundingCorrectly()
    {
        // 33.33 * 0.07 = 2.3331 -> rounds to 2.33
        var result = _sut.CalculateTax(33.33m, 7m, PricingMode.Exclusive);

        result.NetAmount.Should().Be(33.33m);
        result.TaxAmount.Should().Be(2.33m);
        result.GrossAmount.Should().Be(35.66m);
    }

    [Fact]
    public void CalculateTax_InclusiveMode_HandlesRoundingCorrectly()
    {
        // 107.50 / 1.075 = 100.00, tax = 7.50
        var result = _sut.CalculateTax(107.50m, 7.5m, PricingMode.Inclusive);

        result.NetAmount.Should().Be(100m);
        result.TaxAmount.Should().Be(7.50m);
        result.GrossAmount.Should().Be(107.50m);
    }

    // ===== DetermineTaxAsync Tests =====

    private void SetupRuleRepo(
        Guid companyId,
        IEnumerable<TaxDeterminationRule> rules)
    {
        _ruleRepoMock
            .Setup(r => r.FindMatchingRulesAsync(
                companyId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);
    }

    [Fact]
    public async Task DetermineTaxAsync_WithMatchingRule_UsesRuleTaxCode()
    {
        var companyId = Guid.NewGuid();
        var taxCodeId = Guid.NewGuid();

        var rule = new TaxDeterminationRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ItemCategory = "*",
            CustomerType = "*",
            PlaceOfSupply = "DOMESTIC",
            TaxCodeId = taxCodeId,
            Priority = 100,
            IsActive = true,
        };

        var taxCode = new TaxCode
        {
            Id = taxCodeId,
            Code = "SST-SALES-10",
            Description = "Sales Tax 10%",
            LhdntTaxType = "01",
            IsActive = true,
        };

        var taxRate = new TaxRate
        {
            Id = Guid.NewGuid(),
            TaxCodeId = taxCodeId,
            RatePercent = 10m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
        };

        SetupRuleRepo(companyId, new[] { rule });

        _taxCodeRepoMock
            .Setup(r => r.GetByIdAsync(taxCodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxCode);

        _taxRateRepoMock
            .Setup(r => r.GetActiveRateByCodeAsync(
                taxCodeId,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRate);

        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, companyId);

        result.Success.Should().BeTrue();
        result.TaxCode.Should().Be("SST-SALES-10");
        result.RatePercent.Should().Be(10m);
        result.TaxAmount.Should().Be(10m);
        result.NetAmount.Should().Be(100m);
        result.GrossAmount.Should().Be(110m);
    }

    [Fact]
    public async Task DetermineTaxAsync_NoRules_FallsBackToDefault()
    {
        var companyId = Guid.NewGuid();
        var fallbackCodeId = Guid.NewGuid();

        SetupRuleRepo(companyId, new List<TaxDeterminationRule>());

        var fallbackCode = new TaxCode
        {
            Id = fallbackCodeId,
            Code = "SST-SALES-10",
            Description = "Sales Tax 10%",
            LhdntTaxType = "01",
            IsActive = true,
        };

        _taxCodeRepoMock
            .Setup(r => r.GetByCodeAsync("SST-SALES-10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackCode);

        var fallbackRate = new TaxRate
        {
            Id = Guid.NewGuid(),
            TaxCodeId = fallbackCodeId,
            RatePercent = 10m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
        };

        _taxRateRepoMock
            .Setup(r => r.GetActiveRateByCodeAsync(
                fallbackCodeId,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackRate);

        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 200m, companyId);

        result.Success.Should().BeTrue();
        result.TaxCode.Should().Be("SST-SALES-10");
        result.TaxAmount.Should().Be(20m);
        result.GrossAmount.Should().Be(220m);
    }

    [Fact]
    public async Task DetermineTaxAsync_NoRulesAndNoFallback_ReturnsError()
    {
        var companyId = Guid.NewGuid();

        SetupRuleRepo(companyId, new List<TaxDeterminationRule>());

        _taxCodeRepoMock
            .Setup(r => r.GetByCodeAsync("SST-SALES-10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxCode?)null);

        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, companyId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No tax determination rules configured");
    }

    [Fact]
    public async Task DetermineTaxAsync_RuleWithMissingTaxCode_ReturnsError()
    {
        var companyId = Guid.NewGuid();
        var missingCodeId = Guid.NewGuid();

        var rule = new TaxDeterminationRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            TaxCodeId = missingCodeId,
            Priority = 100,
        };

        SetupRuleRepo(companyId, new[] { rule });

        _taxCodeRepoMock
            .Setup(r => r.GetByIdAsync(missingCodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxCode?)null);

        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, companyId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DetermineTaxAsync_RuleWithNoActiveRate_ReturnsError()
    {
        var companyId = Guid.NewGuid();
        var taxCodeId = Guid.NewGuid();

        var rule = new TaxDeterminationRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            TaxCodeId = taxCodeId,
            Priority = 100,
        };

        var taxCode = new TaxCode
        {
            Id = taxCodeId,
            Code = "SST-SALES-10",
            LhdntTaxType = "01",
            IsActive = true,
        };

        SetupRuleRepo(companyId, new[] { rule });

        _taxCodeRepoMock
            .Setup(r => r.GetByIdAsync(taxCodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxCode);

        _taxRateRepoMock
            .Setup(r => r.GetActiveRateByCodeAsync(
                taxCodeId,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRate?)null);

        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, companyId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No active tax rate");
    }

    [Fact]
    public async Task DetermineTaxAsync_InclusivePricing_CalculatesCorrectly()
    {
        var companyId = Guid.NewGuid();
        var taxCodeId = Guid.NewGuid();

        var rule = new TaxDeterminationRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            TaxCodeId = taxCodeId,
            Priority = 100,
        };

        var taxCode = new TaxCode
        {
            Id = taxCodeId,
            Code = "SST-SERV-8",
            LhdntTaxType = "02",
            IsActive = true,
        };

        var taxRate = new TaxRate
        {
            Id = Guid.NewGuid(),
            TaxCodeId = taxCodeId,
            RatePercent = 8m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
        };

        SetupRuleRepo(companyId, new[] { rule });

        _taxCodeRepoMock
            .Setup(r => r.GetByIdAsync(taxCodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxCode);

        _taxRateRepoMock
            .Setup(r => r.GetActiveRateByCodeAsync(
                taxCodeId,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRate);

        // 108 is tax-inclusive, 8% rate -> net = 100, tax = 8
        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 108m, companyId, PricingMode.Inclusive);

        result.Success.Should().BeTrue();
        result.NetAmount.Should().Be(100m);
        result.TaxAmount.Should().Be(8m);
        result.GrossAmount.Should().Be(108m);
    }

    [Fact]
    public async Task DetermineTaxAsync_Exception_ReturnsErrorResult()
    {
        var companyId = Guid.NewGuid();

        _ruleRepoMock
            .Setup(r => r.FindMatchingRulesAsync(
                companyId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection lost"));

        var result = await _sut.DetermineTaxAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, companyId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Database connection lost");
    }
}
