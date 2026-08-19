// <copyright file="PricingEngineTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.OrderManagement.Tests;

public class PricingEngineTests
{
    private static PricingRule Rule(string code, PricingRuleScope scope, int priority, decimal discount, Guid? customer = null, Guid? item = null, decimal? minQty = null) =>
        new(Guid.NewGuid(), code, code, scope, priority, discount, null, customer, item, minQty, null, null);

    [Fact]
    public void QuantityBreak_Wins_OverStandard_ByPriority()
    {
        var basePrice = 100m;
        var rules = new List<PricingRule>
        {
            Rule("STD", PricingRuleScope.Standard, 100, 0m),
            Rule("QTY", PricingRuleScope.QuantityBreak, 10, 10m, minQty: 5),
        };

        var result = PricingEngine.CalculatePrice(basePrice, null, null, 10, rules, DateTime.UtcNow);

        result.DiscountPercent.Should().Be(10m);
        result.UnitPrice.Should().Be(90m);
    }

    [Fact]
    public void CustomerSpecific_Wins_OverQuantityBreak()
    {
        var customerId = Guid.NewGuid();
        var basePrice = 100m;
        var rules = new List<PricingRule>
        {
            Rule("QTY", PricingRuleScope.QuantityBreak, 10, 10m, minQty: 5),
            Rule("CUST", PricingRuleScope.CustomerSpecific, 5, 25m, customer: customerId),
        };

        var result = PricingEngine.CalculatePrice(basePrice, customerId, null, 10, rules, DateTime.UtcNow);

        result.DiscountPercent.Should().Be(25m);
        result.UnitPrice.Should().Be(75m);
    }

    [Fact]
    public void UnitPriceOverride_Produces_ComputedDiscount()
    {
        var basePrice = 100m;
        var rules = new List<PricingRule>
        {
            Rule("PROMO", PricingRuleScope.Promotional, 1, 0m).SetOverride(80m),
        };

        var result = PricingEngine.CalculatePrice(basePrice, null, null, 1, rules, DateTime.UtcNow);

        result.UnitPrice.Should().Be(80m);
        result.DiscountPercent.Should().Be(20m);
    }

    [Fact]
    public void NoMatchingRule_ReturnsBasePrice()
    {
        var rules = new List<PricingRule>
        {
            Rule("CUST", PricingRuleScope.CustomerSpecific, 1, 25m, customer: Guid.NewGuid()),
        };

        var result = PricingEngine.CalculatePrice(100m, Guid.NewGuid(), null, 1, rules, DateTime.UtcNow);

        result.UnitPrice.Should().Be(100m);
        result.DiscountPercent.Should().Be(0m);
        result.AppliedRuleId.Should().BeNull();
    }
}

// Helper to set the optional UnitPriceOverride without bloating the constructor call above.
internal static class PricingRuleExtensions
{
    public static PricingRule SetOverride(this PricingRule rule, decimal overridePrice) =>
        new(rule.CompanyId, rule.Code, rule.Description, rule.Scope, rule.PrioritySequence, rule.DiscountPercent, overridePrice, rule.CustomerId, rule.ItemId, rule.MinimumQuantity, rule.EffectiveFrom, rule.EffectiveTo);
}
