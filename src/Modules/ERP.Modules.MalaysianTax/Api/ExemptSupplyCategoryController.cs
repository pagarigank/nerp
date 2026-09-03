// <copyright file="ExemptSupplyCategoryController.cs" company="ERP Project">
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
/// Exempt/Zero-Rated Supply Category CRUD API for Malaysian SST compliance.
/// Maps product/service categories to exempt or zero-rated tax codes.
/// All mutations are audit-logged.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ExemptSupplyCategoryController : ControllerBase
{
    private readonly IExemptSupplyCategoryRepository _repository;
    private readonly IAuditLogService _auditLog;
    private readonly ICurrentUserService _currentUser;

    public ExemptSupplyCategoryController(
        IExemptSupplyCategoryRepository repository,
        IAuditLogService auditLog,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditLog = auditLog;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all exempt supply categories for a company.
    /// </summary>
    [HttpGet]
    [RequirePermission("tax", "exempt-supplies", "view")]
    [ProducesResponseType(typeof(IEnumerable<ExemptSupplyCategory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required");

        var categories = await _repository.GetAllByCompanyIdAsync(companyId, ct);
        return Ok(categories);
    }

    /// <summary>
    /// Get an exempt supply category by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("tax", "exempt-supplies", "view")]
    [ProducesResponseType(typeof(ExemptSupplyCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null)
            return NotFound();
        return Ok(category);
    }

    /// <summary>
    /// Get effective exempt supply categories for a given date.
    /// </summary>
    [HttpGet("effective")]
    [RequirePermission("tax", "exempt-supplies", "view")]
    [ProducesResponseType(typeof(IEnumerable<ExemptSupplyCategory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffective(
        [FromQuery] Guid companyId,
        [FromQuery] DateTime? asOfDate,
        CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required");

        var date = asOfDate ?? DateTime.UtcNow;
        var categories = await _repository.GetEffectiveOnDateAsync(companyId, date, ct);
        return Ok(categories);
    }

    /// <summary>
    /// Create a new exempt supply category.
    /// </summary>
    [HttpPost]
    [RequirePermission("tax", "exempt-supplies", "create")]
    [ProducesResponseType(typeof(ExemptSupplyCategory), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ExemptSupplyCategory category, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (category.CompanyId == Guid.Empty)
            return BadRequest("CompanyId is required");

        // Check for duplicate category code within the company
        var exists = await _repository.GetByCategoryCodeAsync(category.CompanyId, category.CategoryCode, ct);
        if (exists != null)
            return BadRequest($"Category code '{category.CategoryCode}' already exists for this company");

        await _repository.AddAsync(category, ct);

        await _auditLog.LogAsync(
            "Create",
            nameof(ExemptSupplyCategory),
            category.Id,
            _currentUser.UserId ?? "system",
            newValues: new { category.CompanyId, category.CategoryCode, category.SupplyType, category.TaxCodeId },
            correlationId: _currentUser.CorrelationId,
            cancellationToken: ct);

        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    /// <summary>
    /// Update an exempt supply category.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("tax", "exempt-supplies", "edit")]
    [ProducesResponseType(typeof(ExemptSupplyCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExemptSupplyCategory category, CancellationToken ct)
    {
        if (id != category.Id)
            return BadRequest("ID mismatch");

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing == null)
            return NotFound();

        var oldValues = new { existing.CategoryCode, existing.SupplyType, existing.TaxCodeId, existing.IsActive, existing.EffectiveFrom, existing.EffectiveTo };

        existing.Update(
            category.CompanyId,
            category.CategoryCode,
            category.Description,
            category.SupplyType,
            category.TaxCodeId,
            category.LhdnReference,
            category.IsActive,
            category.EffectiveFrom,
            category.EffectiveTo,
            category.Notes);

        await _repository.UpdateAsync(existing, ct);

        await _auditLog.LogAsync(
            "Update",
            nameof(ExemptSupplyCategory),
            id,
            _currentUser.UserId ?? "system",
            oldValues: oldValues,
            newValues: new { category.CategoryCode, category.SupplyType, category.TaxCodeId, category.IsActive, category.EffectiveFrom, category.EffectiveTo },
            correlationId: _currentUser.CorrelationId,
            cancellationToken: ct);

        return Ok(existing);
    }

    /// <summary>
    /// Soft delete an exempt supply category.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("tax", "exempt-supplies", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);

        await _repository.DeleteAsync(id, ct);

        await _auditLog.LogAsync(
            "Delete",
            nameof(ExemptSupplyCategory),
            id,
            _currentUser.UserId ?? "system",
            oldValues: existing != null ? new { existing.CategoryCode, existing.SupplyType, existing.CompanyId } : null,
            correlationId: _currentUser.CorrelationId,
            cancellationToken: ct);

        return NoContent();
    }
}
