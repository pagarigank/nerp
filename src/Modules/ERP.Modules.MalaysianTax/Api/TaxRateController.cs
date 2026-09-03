// <copyright file="TaxRateController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.MalaysianTax.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// Tax Rate CRUD API for Malaysian SST compliance.
/// Supports effective dating for rate changes.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TaxRateController : ControllerBase
{
    private readonly ITaxRateRepository _repository;

    public TaxRateController(ITaxRateRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Get all tax rates.
    /// </summary>
    [HttpGet]
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
    [ProducesResponseType(typeof(TaxRate), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TaxRate rate)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _repository.AddAsync(rate);

        return CreatedAtAction(nameof(Get), new { id = rate.Id }, rate);
    }

    /// <summary>
    /// Update a tax rate.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaxRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaxRate rate)
    {
        if (id != rate.Id)
            return BadRequest("ID mismatch");

        await _repository.UpdateAsync(rate);

        return Ok(rate);
    }

    /// <summary>
    /// Soft delete a tax rate.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeleteAsync(id);

        return NoContent();
    }

    /// <summary>
    /// Get current tax rate for an effective date.
    /// </summary>
    [HttpGet("current/{taxCodeId:guid}")]
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