// <copyright file="ITaxService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Pricing mode for tax calculation.
/// </summary>
public enum PricingMode
{
    /// <summary>Tax is added on top of the amount (e.g., RM100 + 10% SST = RM110).</summary>
    Exclusive,

    /// <summary>Tax is included in the amount (e.g., RM110 includes 10% SST, tax = RM10).</summary>
    Inclusive,
}

/// <summary>
/// Service interface for tax determination and operations.
/// </summary>
public interface ITaxService
{
    /// <summary>
    /// Determines tax for a line item using configurable rules.
    /// </summary>
    Task<TaxDeterminationResult> DetermineTaxAsync(
        Guid itemId,
        Guid customerId,
        decimal amount,
        Guid companyId,
        PricingMode pricingMode = PricingMode.Exclusive,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the tax amount and net/gross amounts given a rate and pricing mode.
    /// Applies RMCD rounding rules (round to 2 decimal places, half-up).
    /// </summary>
    TaxCalculationResult CalculateTax(
        decimal amount,
        decimal ratePercent,
        PricingMode pricingMode);
}