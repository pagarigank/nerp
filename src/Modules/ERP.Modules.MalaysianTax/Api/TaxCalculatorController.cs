// <copyright file="TaxCalculatorController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.Platform.Api.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// API for previewing tax calculations with inclusive/exclusive pricing.
/// Used by the ERP UI to show tax breakdown before posting.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/tax/calculator")]
[ApiVersion("1.0")]
public class TaxCalculatorController : ControllerBase
{
    private readonly ITaxService _taxService;

    public TaxCalculatorController(ITaxService taxService)
    {
        _taxService = taxService;
    }

    /// <summary>
    /// Previews a tax calculation for a given amount, rate, and pricing mode.
    /// </summary>
    [HttpPost("preview")]
    [RequirePermission("tax", "tax-codes", "view")]
    [ProducesResponseType(typeof(TaxPreviewResult), StatusCodes.Status200OK)]
    public IActionResult Preview([FromBody] TaxPreviewRequest request)
    {
        var result = _taxService.CalculateTax(
            request.Amount,
            request.RatePercent,
            request.PricingMode == "inclusive" ? PricingMode.Inclusive : PricingMode.Exclusive);

        return Ok(new TaxPreviewResult
        {
            InputAmount = request.Amount,
            PricingMode = request.PricingMode,
            RatePercent = result.RatePercent,
            NetAmount = result.NetAmount,
            TaxAmount = result.TaxAmount,
            GrossAmount = result.GrossAmount,
        });
    }

    /// <summary>
    /// Determines tax for a line item using the configured rules.
    /// </summary>
    [HttpPost("determine")]
    [RequirePermission("tax", "tax-rules", "view")]
    [ProducesResponseType(typeof(TaxDeterminationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Determine([FromBody] TaxDetermineRequest request, CancellationToken ct)
    {
        var pricingMode = request.PricingMode == "inclusive"
            ? PricingMode.Inclusive
            : PricingMode.Exclusive;

        var result = await _taxService.DetermineTaxAsync(
            request.ItemId,
            request.CustomerId,
            request.Amount,
            request.CompanyId,
            pricingMode,
            ct);

        if (!result.Success)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(result);
    }
}

/// <summary>
/// Request for tax preview calculation.
/// </summary>
public class TaxPreviewRequest
{
    /// <summary>Gets or sets the amount to calculate tax on.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the tax rate percentage (e.g., 10 for 10%).</summary>
    public decimal RatePercent { get; set; }

    /// <summary>Gets or sets the pricing mode: 'inclusive' (amount includes tax) or 'exclusive' (amount excludes tax).</summary>
    public string PricingMode { get; set; } = "exclusive";
}

/// <summary>
/// Result of a tax preview calculation.
/// </summary>
public class TaxPreviewResult
{
    /// <summary>Gets or sets the input amount.</summary>
    public decimal InputAmount { get; set; }

    /// <summary>Gets or sets the pricing mode used.</summary>
    public string PricingMode { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax rate percentage.</summary>
    public decimal RatePercent { get; set; }

    /// <summary>Gets or sets the net amount (excluding tax).</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Gets or sets the tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the gross amount (including tax).</summary>
    public decimal GrossAmount { get; set; }
}

/// <summary>
/// Request for tax determination.
/// </summary>
public class TaxDetermineRequest
{
    /// <summary>Gets or sets the item ID.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the company ID.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the pricing mode: 'inclusive' or 'exclusive'.</summary>
    public string PricingMode { get; set; } = "exclusive";
}
