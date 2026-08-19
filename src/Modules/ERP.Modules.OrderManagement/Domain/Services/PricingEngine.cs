// <copyright file="PricingEngine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Linq;
using ERP.Modules.OrderManagement.Domain.Entities;

namespace ERP.Modules.OrderManagement.Domain.Services;

/// <summary>
/// Applies pricing/discount rules in priority order:
/// customer-specific -> quantity-break -> promotional -> standard.
/// The first (lowest PrioritySequence) active, effective rule that matches the
/// (customer, item, quantity, date) context wins.
/// </summary>
public static class PricingEngine
{
    public static PricingResult CalculatePrice(
        decimal baseUnitPrice,
        Guid? customerId,
        Guid? itemId,
        decimal quantity,
        IReadOnlyList<PricingRule> rules,
        DateTime asOf)
    {
        var winning = rules
            .Where(r => r.Matches(customerId, itemId, quantity, asOf))
            .OrderBy(r => r.PrioritySequence)
            .ThenBy(r => r.Scope == PricingRuleScope.CustomerSpecific ? 0 : 1)
            .FirstOrDefault();

        if (winning is null)
        {
            return new PricingResult(baseUnitPrice, 0m, baseUnitPrice, null);
        }

        decimal discountPercent;
        decimal unitPrice;
        if (winning.UnitPriceOverride.HasValue)
        {
            unitPrice = winning.UnitPriceOverride.Value;
            discountPercent = baseUnitPrice == 0m
                ? 0m
                : ((1m - (winning.UnitPriceOverride.Value / baseUnitPrice)) * 100m);
        }
        else
        {
            discountPercent = winning.DiscountPercent;
            unitPrice = baseUnitPrice * (1m - (winning.DiscountPercent / 100m));
        }

        return new PricingResult(unitPrice, discountPercent, unitPrice, winning.Id);
    }
}

public record PricingResult(
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal NetUnitPrice,
    Guid? AppliedRuleId);
