// <copyright file="ExemptSupplyCategory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.MalaysianTax.Domain.Entities;

/// <summary>
/// Maps product/service categories to exempt or zero-rated tax codes.
/// Per Malaysian SST law, certain supplies are exempt (E) or zero-rated (ZRL) from tax.
/// This entity provides a reference table so the tax determination engine can automatically
/// assign the correct tax code based on item category, customer type, or place of supply.
/// </summary>
[Table("exempt_supply_category", Schema = "tax")]
public class ExemptSupplyCategory
{
    public ExemptSupplyCategory()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the company this configuration applies to. Use Guid.Empty for system-wide defaults.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Gets or sets the product/service category code (e.g., "FOOD", "MEDICAL", "EDUCATION").
    /// This matches against the ItemCategory field in TaxDeterminationRule.
    /// </summary>
    [Required, MaxLength(50)]
    public string CategoryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of the category.
    /// </summary>
    [MaxLength(250)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the supply type: Exempt, ZeroRated, or DeemedSupply.
    /// </summary>
    [Required, MaxLength(20)]
    public string SupplyType { get; set; } = "Exempt"; // Exempt, ZeroRated, DeemedSupply

    /// <summary>
    /// Gets or sets the tax code ID this category maps to (e.g., SST-EXEMPT, SST-ZRL).
    /// Must reference a valid TaxCode in the tax.tax_code table.
    /// </summary>
    public Guid TaxCodeId { get; set; }

    /// <summary>
    /// Gets or sets the LHDN reference section or schedule for the exemption.
    /// </summary>
    [MaxLength(100)]
    public string? LhdnReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this category is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date from which this exemption is effective.
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the date until which this exemption is effective.
    /// Null means no end date (indefinitely effective).
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Gets or sets the created-at timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated-at timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets notes or additional context for the exemption.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // ---- Business Methods ----

    /// <summary>
    /// Determines whether this exemption is effective on the given date.
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
    /// Updates all fields of the category.
    /// </summary>
    public void Update(
        Guid companyId,
        string categoryCode,
        string? description,
        string supplyType,
        Guid taxCodeId,
        string? lhdnReference,
        bool isActive,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        string? notes)
    {
        CompanyId = companyId;
        CategoryCode = categoryCode;
        Description = description;
        SupplyType = supplyType;
        TaxCodeId = taxCodeId;
        LhdnReference = lhdnReference;
        IsActive = isActive;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
