// <copyright file="PricingRule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public enum PricingRuleScope
{
    Standard = 0,
    CustomerSpecific = 1,
    QuantityBreak = 2,
    Promotional = 3
}

/// <summary>
/// A single pricing/discount rule. Rules are evaluated by the pricing engine in
/// priority order (lower PrioritySequence first). The engine applies the first
/// matching rule's discount to the line unit price.
/// </summary>
public class PricingRule : AuditableEntity
{
    private PricingRule() { }

    public PricingRule(
        Guid companyId,
        string code,
        string description,
        PricingRuleScope scope,
        int prioritySequence,
        decimal discountPercent,
        decimal? unitPriceOverride,
        Guid? customerId,
        Guid? itemId,
        decimal? minimumQuantity,
        DateTime? effectiveFrom,
        DateTime? effectiveTo)
    {
        CompanyId = companyId;
        Code = code;
        Description = description;
        Scope = scope;
        PrioritySequence = prioritySequence;
        DiscountPercent = discountPercent;
        UnitPriceOverride = unitPriceOverride;
        CustomerId = customerId;
        ItemId = itemId;
        MinimumQuantity = minimumQuantity;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PricingRuleScope Scope { get; private set; }
    public int PrioritySequence { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal? UnitPriceOverride { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? ItemId { get; private set; }
    public decimal? MinimumQuantity { get; private set; }
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(
        string description,
        int prioritySequence,
        decimal discountPercent,
        decimal? unitPriceOverride,
        decimal? minimumQuantity,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        bool isActive)
    {
        Description = description;
        PrioritySequence = prioritySequence;
        DiscountPercent = discountPercent;
        UnitPriceOverride = unitPriceOverride;
        MinimumQuantity = minimumQuantity;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = isActive;
    }

    public bool IsEffectiveOn(DateTime asOf) =>
        (!EffectiveFrom.HasValue || EffectiveFrom.Value <= asOf) &&
        (!EffectiveTo.HasValue || EffectiveTo.Value >= asOf);

    public bool Matches(Guid? customerId, Guid? itemId, decimal quantity, DateTime asOf) =>
        IsActive &&
        IsEffectiveOn(asOf) &&
        (CustomerId == null || CustomerId == customerId) &&
        (ItemId == null || ItemId == itemId) &&
        (MinimumQuantity == null || quantity >= MinimumQuantity.Value);
}
