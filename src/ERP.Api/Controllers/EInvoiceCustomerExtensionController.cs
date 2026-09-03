// <copyright file="EInvoiceCustomerExtensionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// CRUD API for e-Invoice customer extensions.
/// Manages TIN, ID type, SST registration numbers for customer master data
/// required for Malaysian e-Invoice compliance (EINV-02).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/customer-extensions")]
[ApiVersion("1.0")]
public class EInvoiceCustomerExtensionController : ControllerBase
{
    private readonly IEInvoiceCustomerExtensionRepository _repository;

    public EInvoiceCustomerExtensionController(IEInvoiceCustomerExtensionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all active customer e-Invoice extensions.
    /// </summary>
    [HttpGet]
    [RequirePermission("einvoice", "customer-extensions", "view")]
    [ProducesResponseType(typeof(IEnumerable<EInvoiceCustomerExtension>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var companyId = GetCompanyId();
        if (companyId == Guid.Empty)
        {
            return BadRequest("Company context is required.");
        }

        var extensions = await _repository.GetAllByCompanyIdAsync(companyId);
        return Ok(extensions);
    }

    /// <summary>
    /// Gets a customer e-Invoice extension by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("einvoice", "customer-extensions", "view")]
    [ProducesResponseType(typeof(EInvoiceCustomerExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var extension = await _repository.GetByIdAsync(id);
        if (extension == null)
            return NotFound();
        return Ok(extension);
    }

    /// <summary>
    /// Gets a customer e-Invoice extension by customer ID.
    /// </summary>
    [HttpGet("by-customer/{customerId:guid}")]
    [RequirePermission("einvoice", "customer-extensions", "view")]
    [ProducesResponseType(typeof(EInvoiceCustomerExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCustomerId(Guid customerId)
    {
        var extension = await _repository.GetByCustomerIdAsync(customerId);
        if (extension == null)
            return NotFound();
        return Ok(extension);
    }

    /// <summary>
    /// Creates or updates a customer e-Invoice extension (upsert by CustomerId).
    /// </summary>
    [HttpPost]
    [RequirePermission("einvoice", "customer-extensions", "create")]
    [ProducesResponseType(typeof(EInvoiceCustomerExtension), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(EInvoiceCustomerExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] EInvoiceCustomerExtensionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.CustomerId == Guid.Empty)
            return BadRequest("CustomerId is required.");

        var existing = await _repository.GetByCustomerIdAsync(request.CustomerId);

        var entity = new EInvoiceCustomerExtension(
            request.CustomerId,
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

        await _repository.AddAsync(entity);

        if (existing != null)
            return Ok(entity);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    /// <summary>
    /// Updates a customer e-Invoice extension.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("einvoice", "customer-extensions", "edit")]
    [ProducesResponseType(typeof(EInvoiceCustomerExtension), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] EInvoiceCustomerExtensionRequest request)
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

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    /// <summary>
    /// Soft-deletes a customer e-Invoice extension.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("einvoice", "customer-extensions", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }

    private Guid GetCompanyId()
    {
        var claim = User?.FindFirst("CompanyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : Guid.Empty;
    }
}

/// <summary>
/// Request body for creating or updating a customer e-Invoice extension.
/// </summary>
public class EInvoiceCustomerExtensionRequest
{
    /// <summary>Gets or sets the customer ID this extension belongs to.</summary>
    [Required]
    public Guid CustomerId { get; set; }

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

    /// <summary>Gets or sets a value indicating whether this customer is subject to e-Invoice.</summary>
    public bool IsSubjectToEInvoice { get; set; }
}
