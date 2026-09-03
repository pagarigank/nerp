// <copyright file="TaxRate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.MalaysianTax.Domain.Entities;

/// <summary>
/// Tax rate master with effective-dating.
/// Supports historical rate changes without corrupting past documents.
/// </summary>
[Table("tax_rate", Schema = "tax")]
public class TaxRate
{
    public TaxRate()
    {
        Id = Guid.NewGuid();
        IsActive = true;
    }

    [Key]
    public Guid Id { get; set; }

    [Required, ForeignKey("TaxCode")]
    public Guid TaxCodeId { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal RatePercent { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual TaxCode TaxCode { get; set; } = null!;
}