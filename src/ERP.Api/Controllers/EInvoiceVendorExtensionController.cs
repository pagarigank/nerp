// <copyright file="EInvoiceVendorExtensionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// CRUD API for e-Invoice vendor extensions.
/// Manages TIN, ID type, SST registration numbers, and self-billing eligibility
/// for vendor master data required for Malaysian e-Invoice compliance (EINV-02, EINV-06).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/vendor-extensions")]
[ApiVersion("1.0")]
public class EInvoiceVendorExtensionController : ControllerBase
{
    private readonly IEInvoiceVendorExtensionRepository _repository;

    public EInvoiceVendorExtensionController(IEInvoiceVendorExtensionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all active vendor e-Invoice extensions.
    /// </summary>
    [HttpGet]
    [RequirePermission("einvoice", "vendor-extensions", "view")]
    [ProducesResponseType(typeof(IEnumerable<EInvoiceVendorExtension>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var extensions = await _repository.GetAllAsync();
        return Ok(extensions);
    }

    /// <summary>
    /// Gets a vendor e-Invoice extension by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("einvoice", "vendor-extensions", "view")]
    [ProducesResponseType(typeof(EInvoiceVendorExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var extension = await _repository.GetByIdAsync(id);
        if (extension == null)
            return NotFound();
        return Ok(extension);
    }

    /// <summary>
    /// Gets a vendor e-Invoice extension by vendor ID.
    /// </summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [RequirePermission("einvoice", "vendor-extensions", "view")]
    [ProducesResponseType(typeof(EInvoiceVendorExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByVendorId(Guid vendorId)
    {
        var extension = await _repository.GetByVendorIdAsync(vendorId);
        if (extension == null)
            return NotFound();
        return Ok(extension);
    }

    /// <summary>
    /// Creates or updates a vendor e-Invoice extension (upsert by VendorId).
    /// </summary>
    [HttpPost]
    [RequirePermission("einvoice", "vendor-extensions", "create")]
    [ProducesResponseType(typeof(EInvoiceVendorExtension), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(EInvoiceVendorExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] EInvoiceVendorExtensionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.VendorId == Guid.Empty)
            return BadRequest("VendorId is required.");

        var existing = await _repository.GetByVendorIdAsync(request.VendorId);

        var entity = new EInvoiceVendorExtension(
            request.VendorId,
            request.TaxIdentificationNumber,
            request.IdType,
            request.IdValue,
            request.SstRegistrationNumber,
            request.SstBillingAddress,
            request.SstBillingCity,
            request.SstBillingState,
            request.SstBillingPostalCode,
            request.SstBillingCountry,
            request.IsSubjectToEInvoice);

        entity.SetSelfBilledEligible(request.CanIssueSelfBilled);

        await _repository.AddAsync(entity);

        if (existing != null)
            return Ok(entity);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    /// <summary>
    /// Updates a vendor e-Invoice extension.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("einvoice", "vendor-extensions", "edit")]
    [ProducesResponseType(typeof(EInvoiceVendorExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] EInvoiceVendorExtensionRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.SetTaxIdentificationNumber(request.TaxIdentificationNumber);
        existing.SetIdType(request.IdType);
        existing.SetIdValue(request.IdValue);
        existing.SetSstRegistrationNumber(request.SstRegistrationNumber);
        existing.SetSstBillingAddress(
            request.SstBillingAddress,
            request.SstBillingCity,
            request.SstBillingState,
            request.SstBillingPostalCode,
            request.SstBillingCountry);
        existing.SetEInvoiceStatus(request.IsSubjectToEInvoice);
        existing.SetSelfBilledEligible(request.CanIssueSelfBilled);

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    /// <summary>
    /// Soft-deletes a vendor e-Invoice extension.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("einvoice", "vendor-extensions", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// Request body for creating or updating a vendor e-Invoice extension.
/// </summary>
public class EInvoiceVendorExtensionRequest
{
    /// <summary>Gets or sets the vendor ID this extension belongs to.</summary>
    [Required]
    public Guid VendorId { get; set; }

    /// <summary>Gets or sets the Tax Identification Number (TIN).</summary>
    [MaxLength(50)]
    public string? TaxIdentificationNumber { get; set; }

    /// <summary>Gets or sets the identity type (BRN, NRIC, Army, Passport, Old BRN).</summary>
    [MaxLength(20)]
    public string? IdType { get; set; }

    /// <summary>Gets or sets the identity document value.</summary>
    [MaxLength(50)]
    public string? IdValue { get; set; }

    /// <summary>Gets or sets the SST Registration Number.</summary>
    [MaxLength(50)]
    public string? SstRegistrationNumber { get; set; }

    /// <summary>Gets or sets the SST billing address.</summary>
    [MaxLength(200)]
    public string? SstBillingAddress { get; set; }

    /// <summary>Gets or sets the SST billing city.</summary>
    [MaxLength(100)]
    public string? SstBillingCity { get; set; }

    /// <summary>Gets or sets the SST billing state.</summary>
    [MaxLength(100)]
    public string? SstBillingState { get; set; }

    /// <summary>Gets or sets the SST billing postal code.</summary>
    [MaxLength(20)]
    public string? SstBillingPostalCode { get; set; }

    /// <summary>Gets or sets the SST billing country code (default MY).</summary>
    [MaxLength(3)]
    public string? SstBillingCountry { get; set; }

    /// <summary>Gets or sets a value indicating whether this vendor is subject to e-Invoice.</summary>
    public bool IsSubjectToEInvoice { get; set; }

    /// <summary>Gets or sets a value indicating whether this vendor can issue self-billed invoices.</summary>
    public bool CanIssueSelfBilled { get; set; }
}
