// <copyright file="TaxEngineTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.OrderManagement.Tests;

public class TaxEngineTests
{
    private static TaxCode Code(string jurisdiction, decimal rate, bool taxable = true, DateTime? from = null, DateTime? to = null) =>
        new(Guid.NewGuid(), "TAX", "Tax", jurisdiction, rate, taxable, from, to);

    [Fact]
    public void TaxableItem_AndJurisdiction_ReturnsExpectedTax()
    {
        var codes = new List<TaxCode> { Code("CA", 8.5m) };

        var result = TaxEngine.CalculateTax(100m, "CA", itemTaxable: true, customerExempt: false, codes, DateTime.UtcNow);

        result.Exempt.Should().BeFalse();
        result.TaxAmount.Should().Be(8.5m);
    }

    [Fact]
    public void ExemptCustomer_PaysZeroTax()
    {
        var codes = new List<TaxCode> { Code("CA", 8.5m) };

        var result = TaxEngine.CalculateTax(100m, "CA", itemTaxable: true, customerExempt: true, codes, DateTime.UtcNow);

        result.Exempt.Should().BeTrue();
        result.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void NonTaxableItem_PaysZeroTax()
    {
        var codes = new List<TaxCode> { Code("CA", 8.5m) };

        var result = TaxEngine.CalculateTax(100m, "CA", itemTaxable: false, customerExempt: false, codes, DateTime.UtcNow);

        result.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void UnknownJurisdiction_PaysZeroTax()
    {
        var codes = new List<TaxCode> { Code("CA", 8.5m) };

        var result = TaxEngine.CalculateTax(100m, "NY", itemTaxable: true, customerExempt: false, codes, DateTime.UtcNow);

        result.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void EffectiveDatedRate_ExpiredRate_NotApplied()
    {
        var expired = Code("CA", 10m, from: new DateTime(2020, 1, 1), to: new DateTime(2020, 12, 31));

        var result = TaxEngine.CalculateTax(100m, "CA", itemTaxable: true, customerExempt: false, new List<TaxCode> { expired }, new DateTime(2026, 1, 1));

        result.TaxAmount.Should().Be(0m);
    }
}
