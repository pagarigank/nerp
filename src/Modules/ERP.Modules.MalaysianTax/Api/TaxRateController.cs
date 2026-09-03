// <copyright file="TaxRateController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.MalaysianTax.Infrastructure;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// Tax Rate CRUD API for Malaysian SST compliance.
/// Supports effective dating for rate changes.
/// All mutations are audit-logged.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TaxRateController : ControllerBase
{
    private readonly ITaxRateRepository _repository;
    private readonly IAuditLogService _auditLog;
    private readonly ICurrentUserService _currentUser;

    public TaxRateController(
        ITaxRateRepository repository,
        IAuditLogService auditLog,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditLog = auditLog;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all tax rates.
    /// </summary>
    [HttpGet]
    [RequirePermission("tax", "tax-rates", "view")]
    [ProducesResponseType(typeof(IEnumerable<TaxRate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var rates = await _repository.GetRatesByCodeAsync(Guid.Empty);
        return Ok(rates);
    }

    /// <summary>
    /// Get tax rate by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("tax", "tax-rates", "view")]
    [ProducesResponseType(typeof(TaxRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var rate = await _repository.GetByIdAsync(id);
        if (rate == null)
            return NotFound();
        return Ok(rate);
    }

    /// <summary>
    /// Get tax rates by tax code.
    /// </summary>
    [HttpGet("by-tax-code/{taxCodeId:guid}")]
    [RequirePermission("tax", "tax-rates", "view")]
    [ProducesResponseType(typeof(IEnumerable<TaxRate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTaxCode(Guid taxCodeId)
    {
        var rates = await _repository.GetRatesByCodeAsync(taxCodeId);
        return Ok(rates);
    }

    /// <summary>
    /// Create a new tax rate.
    /// </summary>
    [HttpPost]
    [RequirePermission("tax", "tax-rates", "create")]
    [ProducesResponseType(typeof(TaxRate), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TaxRate rate)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _repository.AddAsync(rate);

        await _auditLog.LogAsync(
            "Create",
            nameof(TaxRate),
            rate.Id,
            _currentUser.UserId ?? "system",
            newValues: new { rate.TaxCodeId, rate.RatePercent, rate.EffectiveFrom, rate.EffectiveTo },
            correlationId: _currentUser.CorrelationId);

        return CreatedAtAction(nameof(Get), new { id = rate.Id }, rate);
    }

    /// <summary>
    /// Update a tax rate.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("tax", "tax-rates", "edit")]
    [ProducesResponseType(typeof(TaxRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaxRate rate)
    {
        if (id != rate.Id)
            return BadRequest("ID mismatch");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var oldValues = new { existing.TaxCodeId, existing.RatePercent, existing.EffectiveFrom, existing.EffectiveTo, existing.IsActive };

        await _repository.UpdateAsync(rate);

        await _auditLog.LogAsync(
            "Update",
            nameof(TaxRate),
            rate.Id,
            _currentUser.UserId ?? "system",
            oldValues: oldValues,
            newValues: new { rate.TaxCodeId, rate.RatePercent, rate.EffectiveFrom, rate.EffectiveTo, rate.IsActive },
            correlationId: _currentUser.CorrelationId);

        return Ok(rate);
    }

    /// <summary>
    /// Soft delete a tax rate.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("tax", "tax-rates", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);

        await _repository.DeleteAsync(id);

        await _auditLog.LogAsync(
            "Delete",
            nameof(TaxRate),
            id,
            _currentUser.UserId ?? "system",
            oldValues: existing != null ? new { existing.TaxCodeId, existing.RatePercent, existing.EffectiveFrom } : null,
            correlationId: _currentUser.CorrelationId);

        return NoContent();
    }

    /// <summary>
    /// Get current tax rate for an effective date.
    /// </summary>
    [HttpGet("current/{taxCodeId:guid}")]
    [RequirePermission("tax", "tax-rates", "view")]
    [ProducesResponseType(typeof(TaxRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(Guid taxCodeId, [FromQuery] DateTime? effectiveDate = null)
    {
        var date = effectiveDate ?? DateTime.UtcNow;
        var rate = await _repository.GetActiveRateByCodeAsync(taxCodeId, date);
        if (rate == null)
            return NotFound();
        return Ok(rate);
    }
}
