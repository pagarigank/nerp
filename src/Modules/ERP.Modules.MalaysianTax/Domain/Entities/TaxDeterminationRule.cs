// <copyright file="TaxDeterminationRule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.MalaysianTax.Domain.Entities;

/// <summary>
/// Tax determination rule for Malaysian SST compliance.
/// Maps an (item_category, customer_type, place_of_supply) tuple to a specific TaxCode.
/// Rules are evaluated most-specific-first; '*' acts as a wildcard matching any value.
/// </summary>
[Table("tax_determination_rule", Schema = "tax")]
public class TaxDeterminationRule
{
    public TaxDeterminationRule()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        Priority = 100;
        CreatedAt = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the company this rule applies to. Use Guid.Empty for system-wide defaults.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Gets or sets the item category this rule matches (e.g., "FOOD", "ELECTRONICS", "SERVICES").
    /// Use "*" to match any item category.
    /// </summary>
    [Required, MaxLength(50)]
    public string ItemCategory { get; set; } = "*";

    /// <summary>
    /// Gets or sets the customer type this rule matches (e.g., "BUSINESS", "INDIVIDUAL", "GOVERNMENT").
    /// Use "*" to match any customer type.
    /// </summary>
    [Required, MaxLength(50)]
    public string CustomerType { get; set; } = "*";

    /// <summary>
    /// Gets or sets the place of supply this rule matches (e.g., "DOMESTIC", "EXPORT", "LABUAN").
    /// Use "*" to match any place of supply.
    /// </summary>
    [Required, MaxLength(50)]
    public string PlaceOfSupply { get; set; } = "*";

    /// <summary>
    /// Gets or sets the tax code ID this rule resolves to.
    /// Must reference a valid TaxCode in the tax.tax_code table.
    /// </summary>
    public Guid TaxCodeId { get; set; }

    /// <summary>
    /// Gets or sets the priority for rule evaluation.
    /// Lower numbers = higher priority (evaluated first).
    /// Specific rules (no wildcards) should have lower priority numbers than wildcard rules.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is active.
    /// Inactive rules are excluded from determination lookups.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date from which this rule is effective.
    /// Allows scheduling tax rule changes in advance.
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the date until which this rule is effective.
    /// Null means no end date (indefinitely effective).
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of when this rule applies.
    /// </summary>
    [MaxLength(250)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the created-at timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated-at timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // ---- Business Methods ----

    /// <summary>
    /// Determines whether this rule matches the given context.
    /// '*' matches any value; otherwise exact match is required.
    /// </summary>
    public bool Matches(string itemCategory, string customerType, string placeOfSupply)
    {
        return MatchesField(ItemCategory, itemCategory)
            && MatchesField(CustomerType, customerType)
            && MatchesField(PlaceOfSupply, placeOfSupply);
    }

    /// <summary>
    /// Determines whether this rule is effective on the given date.
    /// </summary>
    public bool IsEffectiveOn(DateTime date)
    {
        if (EffectiveFrom.HasValue && date < EffectiveFrom.Value)
            return false;
        if (EffectiveTo.HasValue && date > EffectiveTo.Value)
            return false;
        return true;
    }

    /// <summary>
    /// Calculates the specificity score for ranking.
    /// Higher score = more specific = preferred over lower scores.
    /// Each wildcard reduces the score.
    /// </summary>
    public int SpecificityScore()
    {
        var score = 0;
        if (ItemCategory != "*") score += 100;
        if (CustomerType != "*") score += 50;
        if (PlaceOfSupply != "*") score += 25;
        return score;
    }

    /// <summary>
    /// Updates all fields of the rule.
    /// </summary>
    public void Update(
        Guid companyId,
        string itemCategory,
        string customerType,
        string placeOfSupply,
        Guid taxCodeId,
        int priority,
        bool isActive,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        string? description)
    {
        CompanyId = companyId;
        ItemCategory = itemCategory;
        CustomerType = customerType;
        PlaceOfSupply = placeOfSupply;
        TaxCodeId = taxCodeId;
        Priority = priority;
        IsActive = isActive;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool MatchesField(string ruleValue, string actualValue)
    {
        return ruleValue == "*" || string.Equals(ruleValue, actualValue, StringComparison.OrdinalIgnoreCase);
    }
}
