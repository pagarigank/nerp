// <copyright file="EInvoiceTaxpayerProfileController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
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
public class EInvoiceTaxpayerProfileController : ControllerBase
{
    private readonly ITaxpayerProfileService _service;
    private readonly ICurrentUserService _currentUser;

    public EInvoiceTaxpayerProfileController(ITaxpayerProfileService service, ICurrentUserService currentUser)
    {
        _service = service;
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
            return Forbid();

        var profile = await _service.GetByCompanyIdAsync(companyId);
        if (profile == null)
            return NotFound();
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
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(request.CompanyId))
            return Forbid();

        var existing = await _service.GetByCompanyIdAsync(request.CompanyId);
        var result = await _service.UpdateAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        if (existing == null)
            return CreatedAtAction(nameof(GetByCompanyId), new { companyId = request.CompanyId }, result.Value);

        return Ok(result.Value);
    }

    /// <summary>
    /// Validates a TIN against the MyInvois API.
    /// </summary>
    [HttpGet("tin-validate/{tin}")]
    [RequirePermission("einvoice", "taxpayer-profiles", "view")]
    [ProducesResponseType(typeof(TinValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTin(string tin)
    {
        var result = await _service.ValidateTinAsync(tin);
        return Ok(result);
    }
}
