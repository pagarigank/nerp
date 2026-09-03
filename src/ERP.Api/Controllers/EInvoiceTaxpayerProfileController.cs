// <copyright file="EInvoiceTaxpayerProfileController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Infrastructure;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// API for company taxpayer profiles (TIN, BRN, SST numbers, MSIC, address).
/// Used as the Supplier party on every e-Invoice document.
/// All endpoints enforce multi-company isolation via ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/taxpayer-profiles")]
[ApiVersion("1.0")]
#pragma warning disable S6960 // Controller has multiple responsibilities — batch validation and cache status are TIN-related
public class EInvoiceTaxpayerProfileController : ControllerBase
#pragma warning restore S6960
{
    private readonly ITaxpayerProfileService _service;
    private readonly ITinValidationCache _tinCache;
    private readonly ICurrentUserService _currentUser;

    public EInvoiceTaxpayerProfileController(
        ITaxpayerProfileService service,
        ITinValidationCache tinCache,
        ICurrentUserService currentUser)
    {
        _service = service;
        _tinCache = tinCache;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets the taxpayer profile for a company.
    /// </summary>
    [HttpGet("by-company/{companyId:guid}")]
    [RequirePermission("einvoice", "taxpayer-profiles", "view")]
    [ProducesResponseType(typeof(TaxpayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByCompanyId(Guid companyId)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
        {
            return Forbid();
        }

        var profile = await _service.GetByCompanyIdAsync(companyId);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    /// <summary>
    /// Creates or updates the taxpayer profile for a company (upsert).
    /// </summary>
    [HttpPost]
    [RequirePermission("einvoice", "taxpayer-profiles", "create")]
    [ProducesResponseType(typeof(TaxpayerProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TaxpayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] TaxpayerProfileUpdateRequest request)
    {
        if (request.CompanyId == Guid.Empty)
        {
            return BadRequest("CompanyId is required.");
        }

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(request.CompanyId))
        {
            return Forbid();
        }

        var existing = await _service.GetByCompanyIdAsync(request.CompanyId);
        var result = await _service.UpdateAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        if (existing == null)
        {
            return CreatedAtAction(nameof(GetByCompanyId), new { companyId = request.CompanyId }, result.Value);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Validates a TIN against the MyInvois API (with cache).
    /// </summary>
    [HttpGet("tin-validate/{tin}")]
    [RequirePermission("einvoice", "taxpayer-profiles", "view")]
    [ProducesResponseType(typeof(TinValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTin(string tin)
    {
        var result = await _service.ValidateTinAsync(tin);
        return Ok(result);
    }

    /// <summary>
    /// Batch-validates multiple TINs against the MyInvois API.
    /// Returns validation results for each TIN.
    /// </summary>
    [HttpPost("tin-validate-batch")]
    [RequirePermission("einvoice", "taxpayer-profiles", "view")]
    [ProducesResponseType(typeof(IReadOnlyList<TinValidationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTinBatch(
        [FromBody] TinBatchValidationRequest request,
        CancellationToken ct)
    {
        if (request.Tins == null || request.Tins.Count == 0)
        {
            return BadRequest(new { Error = "At least one TIN is required." });
        }

        if (request.Tins.Count > 50)
        {
            return BadRequest(new { Error = "Maximum 50 TINs per batch validation." });
        }

        var results = new List<TinValidationResult>();
        foreach (var tin in request.Tins)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            var result = await _service.ValidateTinAsync(tin);
            results.Add(result);
        }

        return Ok(results);
    }

    /// <summary>
    /// Gets the TIN validation cache status (count of cached TINs, cached TIN list).
    /// </summary>
    [HttpGet("tin-cache-status")]
    [RequirePermission("einvoice", "taxpayer-profiles", "view")]
    [ProducesResponseType(typeof(TinCacheStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetTinCacheStatus()
    {
        var cachedTins = _tinCache.GetAllCachedTins();

        return Ok(new TinCacheStatusDto
        {
            CachedCount = cachedTins.Count,
            Tins = cachedTins.Take(100).ToList(),
        });
    }

    /// <summary>
    /// Clears the TIN validation cache.
    /// </summary>
    [HttpPost("tin-cache-clear")]
    [RequirePermission("einvoice", "taxpayer-profiles", "edit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult ClearTinCache()
    {
        _tinCache.Clear();
        return Ok(new { message = "TIN validation cache cleared." });
    }
}

/// <summary>
/// Request body for batch TIN validation.
/// </summary>
public class TinBatchValidationRequest
{
    /// <summary>Gets or sets the list of TINs to validate.</summary>
    #pragma warning disable CA1002, CA2227
    public List<string> Tins { get; set; } = new();
    #pragma warning restore CA1002, CA2227
}

/// <summary>
/// DTO for TIN validation cache status.
/// </summary>
public class TinCacheStatusDto
{
    /// <summary>Gets or sets the number of cached TINs.</summary>
    public int CachedCount { get; set; }

    /// <summary>Gets or sets the list of cached TINs (up to 100).</summary>
    #pragma warning disable CA1002, CA2227
    public List<string> Tins { get; set; } = new();
    #pragma warning restore CA1002, CA2227
}
