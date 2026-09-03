// <copyright file="TaxCodeController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// Tax Code CRUD API for Malaysian SST compliance.
/// Maps to LHDN e-Invoice tax type codes (01, 02, 03, 04, E, ZRL).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TaxCodeController : ControllerBase
{
    private readonly ITaxCodeRepository _repository;

    public TaxCodeController(ITaxCodeRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Get all tax codes.
    /// </summary>
    [HttpGet]
    [RequirePermission("tax", "tax-codes", "view")]
    [ProducesResponseType(typeof(IEnumerable<TaxCode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var codes = await _repository.GetAllActiveAsync();
        return Ok(codes);
    }

    /// <summary>
    /// Get a tax code by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaxCode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var code = await _repository.GetByIdAsync(id);
        if (code == null)
            return NotFound();
        return Ok(code);
    }

    /// <summary>
    /// Create a new tax code.
    /// </summary>
    [HttpPost]
    [RequirePermission("tax", "tax-codes", "create")]
    [ProducesResponseType(typeof(TaxCode), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TaxCode code)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check for duplicate code
        var exists = await _repository.GetByCodeAsync(code.Code);
        if (exists != null)
            return BadRequest("Tax code already exists");

        await _repository.AddAsync(code);

        return CreatedAtAction(nameof(Get), new { id = code.Id }, code);
    }

    /// <summary>
    /// Update a tax code.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("tax", "tax-codes", "edit")]
    [ProducesResponseType(typeof(TaxCode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaxCode code)
    {
        if (id != code.Id)
            return BadRequest("ID mismatch");

        await _repository.UpdateAsync(code);

        return Ok(code);
    }

    /// <summary>
    /// Soft delete a tax code.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("tax", "tax-codes", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeleteAsync(id);

        return NoContent();
    }
}