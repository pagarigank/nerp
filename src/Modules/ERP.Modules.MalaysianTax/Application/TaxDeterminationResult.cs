// <copyright file="TaxDeterminationResult.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Result of tax determination.
/// </summary>
public class TaxDeterminationResult
{
    public Guid TaxCodeId { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public string LhdntTaxType { get; set; } = string.Empty;
    public decimal TaxAmount { get; set; }
    public decimal RatePercent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the net amount (excluding tax).</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Gets or sets the gross amount (including tax).</summary>
    public decimal GrossAmount { get; set; }
}

/// <summary>
/// Result of a tax calculation (net, tax, gross amounts).
/// </summary>
public class TaxCalculationResult
{
    /// <summary>Gets or sets the net amount (tax-exclusive).</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Gets or sets the tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the gross amount (tax-inclusive).</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>Gets or sets the effective tax rate used.</summary>
    public decimal RatePercent { get; set; }
}