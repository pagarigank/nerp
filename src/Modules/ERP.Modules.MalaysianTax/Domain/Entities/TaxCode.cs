// <copyright file="TaxCode.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.MalaysianTax.Domain.Entities;

/// <summary>
/// Tax code master for Malaysian SST compliance.
/// Maps to LHDN e-Invoice tax type codes (01, 02, 03, 04, E, ZRL).
/// </summary>
[Table("tax_code", Schema = "tax")]
public class TaxCode
{
    public TaxCode()
    {
        Id = Guid.NewGuid();
        IsActive = true;
    }

    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(5)]
    public string LhdntTaxType { get; set; } = "01"; // 01, 02, 03, 04, E, ZRL

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}