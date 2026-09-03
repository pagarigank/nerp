// <copyright file="CompanySstRegistrationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// Company SST Registration CRUD API for Malaysian SST compliance.
/// Manages Sales Tax, Service Tax, and Tourism Tax registration numbers per company.
/// All endpoints enforce multi-company isolation via ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CompanySstRegistrationController : ControllerBase
{
    private readonly ICompanySstRegistrationRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CompanySstRegistrationController(ICompanySstRegistrationRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets all active SST registrations scoped to the caller's companies.
    /// Super admins see all; company-scoped users see only their companies.
    /// </summary>
    [HttpGet]
    [RequirePermission("tax", "sst-registration", "view")]
    [ProducesResponseType(typeof(IEnumerable<CompanySstRegistration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var all = await _repository.GetAllActiveAsync();
        var filtered = _currentUser.IsSuperAdmin
            ? all
            : all.Where(r => _currentUser.CompanyIds.Contains(r.CompanyId));
        return Ok(filtered);
    }

    /// <summary>
    /// Gets an SST registration by ID (must belong to caller's company).
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("tax", "sst-registration", "view")]
    [ProducesResponseType(typeof(CompanySstRegistration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var registration = await _repository.GetByIdAsync(id);
        if (registration == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(registration.CompanyId))
            return Forbid();
        return Ok(registration);
    }

    /// <summary>
    /// Gets SST registration by Company ID (must belong to caller's companies).
    /// </summary>
    [HttpGet("by-company/{companyId:guid}")]
    [RequirePermission("tax", "sst-registration", "view")]
    [ProducesResponseType(typeof(CompanySstRegistration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCompanyId(Guid companyId)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();
        var registration = await _repository.GetByCompanyIdAsync(companyId);
        if (registration == null)
            return NotFound();
        return Ok(registration);
    }

    /// <summary>
    /// Creates or updates an SST registration for a company (upsert).
    /// Caller must have access to the target company.
    /// </summary>
    [HttpPost]
    [RequirePermission("tax", "sst-registration", "create")]
    [ProducesResponseType(typeof(CompanySstRegistration), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CompanySstRegistration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CompanySstRegistrationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.CompanyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(request.CompanyId))
            return Forbid();

        var existing = await _repository.GetByCompanyIdAsync(request.CompanyId);

        if (existing != null)
        {
            existing.UpdateProfile(
                request.IsRegisteredForSalesTax,
                request.SalesTaxRegistrationNumber,
                request.SalesTaxRegistrationDate,
                request.IsRegisteredForServiceTax,
                request.ServiceTaxRegistrationNumber,
                request.ServiceTaxRegistrationDate,
                request.IsRegisteredForTourismTax,
                request.TourismTaxRegistrationNumber,
                request.RmcdPortalReference,
                request.Notes);
            await _repository.UpdateAsync(existing);
            return Ok(existing);
        }

        var entity = new CompanySstRegistration
        {
            CompanyId = request.CompanyId,
            IsRegisteredForSalesTax = request.IsRegisteredForSalesTax,
            SalesTaxRegistrationNumber = request.SalesTaxRegistrationNumber,
            SalesTaxRegistrationDate = request.SalesTaxRegistrationDate,
            IsRegisteredForServiceTax = request.IsRegisteredForServiceTax,
            ServiceTaxRegistrationNumber = request.ServiceTaxRegistrationNumber,
            ServiceTaxRegistrationDate = request.ServiceTaxRegistrationDate,
            IsRegisteredForTourismTax = request.IsRegisteredForTourismTax,
            TourismTaxRegistrationNumber = request.TourismTaxRegistrationNumber,
            RmcdPortalReference = request.RmcdPortalReference,
            Notes = request.Notes
        };

        await _repository.AddAsync(entity);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    /// <summary>
    /// Updates an SST registration (must belong to caller's company).
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("tax", "sst-registration", "edit")]
    [ProducesResponseType(typeof(CompanySstRegistration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompanySstRegistrationRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(existing.CompanyId))
            return Forbid();

        existing.UpdateProfile(
            request.IsRegisteredForSalesTax,
            request.SalesTaxRegistrationNumber,
            request.SalesTaxRegistrationDate,
            request.IsRegisteredForServiceTax,
            request.ServiceTaxRegistrationNumber,
            request.ServiceTaxRegistrationDate,
            request.IsRegisteredForTourismTax,
            request.TourismTaxRegistrationNumber,
            request.RmcdPortalReference,
            request.Notes);

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    /// <summary>
    /// Soft-deletes an SST registration (must belong to caller's company).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("tax", "sst-registration", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NoContent();
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(existing.CompanyId))
            return Forbid();

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// Request body for creating or updating an SST registration.
/// </summary>
public class CompanySstRegistrationRequest
{
    /// <summary>Gets or sets the company ID this registration belongs to.</summary>
    public Guid CompanyId { get; set; }

    // ---- Sales Tax ----
    public bool IsRegisteredForSalesTax { get; set; }
    public string? SalesTaxRegistrationNumber { get; set; }
    public DateTime? SalesTaxRegistrationDate { get; set; }

    // ---- Service Tax ----
    public bool IsRegisteredForServiceTax { get; set; }
    public string? ServiceTaxRegistrationNumber { get; set; }
    public DateTime? ServiceTaxRegistrationDate { get; set; }

    // ---- Tourism Tax ----
    public bool IsRegisteredForTourismTax { get; set; }
    public string? TourismTaxRegistrationNumber { get; set; }

    // ---- General ----
    public string? RmcdPortalReference { get; set; }
    public string? Notes { get; set; }
}
