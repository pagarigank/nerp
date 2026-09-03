// <copyright file="EInvoiceItemExtensionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// API for item/service e-Invoice extensions (classification codes, UOM, tariff codes).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/item-extensions")]
[ApiVersion("1.0")]
public class EInvoiceItemExtensionController : ControllerBase
{
    private readonly IEInvoiceItemExtensionRepository _repository;

    public EInvoiceItemExtensionController(IEInvoiceItemExtensionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all item extensions.
    /// </summary>
    [HttpGet]
    [RequirePermission("einvoice", "item-extensions", "view")]
    [ProducesResponseType(typeof(List<EInvoiceItemExtension>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repository.GetAllAsync();
        return Ok(items);
    }

    /// <summary>
    /// Gets an item extension by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("einvoice", "item-extensions", "view")]
    [ProducesResponseType(typeof(EInvoiceItemExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Gets an item extension by Item ID.
    /// </summary>
    [HttpGet("by-item/{itemId:guid}")]
    [RequirePermission("einvoice", "item-extensions", "view")]
    [ProducesResponseType(typeof(EInvoiceItemExtension), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByItemId(Guid itemId)
    {
        var item = await _repository.GetByItemIdAsync(itemId);
        if (item == null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Creates or updates an item extension (upsert by ItemId).
    /// </summary>
    [HttpPost]
    [RequirePermission("einvoice", "item-extensions", "create")]
    [ProducesResponseType(typeof(EInvoiceItemExtension), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] ItemExtensionRequest request)
    {
        var existing = await _repository.GetByItemIdAsync(request.ItemId);

        if (existing != null)
        {
            existing.SetClassificationCode(request.ClassificationCode);
            existing.SetUnitOfMeasureCode(request.UnitOfMeasureCode);
            existing.SetProductTariffCode(request.ProductTariffCode);
            existing.SetCountryOfOrigin(request.CountryOfOrigin);
            existing.SetEInvoiceStatus(request.IsSubjectToEInvoice);
            await _repository.UpdateAsync(existing);
            return Ok(existing);
        }

        var entity = new EInvoiceItemExtension(
            request.ItemId,
            request.ClassificationCode,
            request.UnitOfMeasureCode,
            request.ProductTariffCode,
            request.CountryOfOrigin,
            request.IsSubjectToEInvoice);

        await _repository.AddAsync(entity);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    /// <summary>
    /// Deletes an item extension.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("einvoice", "item-extensions", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// Request body for item extension create/update.
/// </summary>
public class ItemExtensionRequest
{
    /// <summary>Gets or sets the ERP Item ID.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Gets or sets the LHDN classification code.</summary>
    public string? ClassificationCode { get; set; }

    /// <summary>Gets or sets the UN/ECE unit of measure code.</summary>
    public string? UnitOfMeasureCode { get; set; }

    /// <summary>Gets or sets the product tariff code (HS code).</summary>
    public string? ProductTariffCode { get; set; }

    /// <summary>Gets or sets the country of origin (ISO 3166-1 alpha-3).</summary>
    public string? CountryOfOrigin { get; set; }

    /// <summary>Gets or sets whether this item is subject to e-Invoice.</summary>
    public bool IsSubjectToEInvoice { get; set; } = true;
}
