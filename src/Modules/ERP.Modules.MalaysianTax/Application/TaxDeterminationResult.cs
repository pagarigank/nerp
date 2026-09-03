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
}