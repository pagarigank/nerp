// <copyright file="EInvoiceCertificateController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// CRUD API for digital certificates used for XAdES signing of e-Invoice documents.
/// Manages certificate upload, metadata, activation, and revocation.
/// All endpoints enforce multi-company isolation via ICurrentUserService.
/// Private keys are never exposed after upload.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/certificates")]
[ApiVersion("1.0")]
public class EInvoiceCertificateController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly ICurrentUserService _currentUser;

    public EInvoiceCertificateController(ICertificateService certificateService, ICurrentUserService currentUser)
    {
        _certificateService = certificateService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets all certificates for a company (metadata only, no private key).
    /// </summary>
    [HttpGet]
    [RequirePermission("einvoice", "certificates", "view")]
    [ProducesResponseType(typeof(List<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var certs = await _certificateService.GetAllByCompanyIdAsync(companyId);
        return Ok(certs);
    }

    /// <summary>
    /// Gets a certificate by ID (must belong to caller's company).
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("einvoice", "certificates", "view")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(Guid id)
    {
        var cert = await _certificateService.GetByIdAsync(id);
        if (cert == null)
            return NotFound();
        return Ok(cert);
    }

    /// <summary>
    /// Uploads a new digital certificate (PFX format).
    /// Extracts metadata (issuer, serial, thumbprint, expiry) from the file.
    /// The private key is stored in the vault; only metadata is kept in the DB.
    /// </summary>
    [HttpPost("upload")]
    [RequirePermission("einvoice", "certificates", "create")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        [FromQuery] Guid companyId,
        [FromForm] IFormFile file,
        [FromQuery] string name,
        [FromQuery] bool isActive = true)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest("Certificate file is required.");

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Certificate name is required.");

        // Validate file extension
        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".pfx", StringComparison.OrdinalIgnoreCase) && !string.Equals(ext, ".p12", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PFX/P12 certificate files are supported.");

        byte[] fileContent;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            fileContent = memoryStream.ToArray();
        }

        // Max 100KB for a certificate file
        if (fileContent.Length > 100 * 1024)
            return BadRequest("Certificate file too large (max 100KB).");

        try
        {
            var result = await _certificateService.UploadAsync(companyId, fileContent, file.FileName, name, isActive);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Activates a certificate and deactivates all others for the same company.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [RequirePermission("einvoice", "certificates", "edit")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            var result = await _certificateService.ActivateAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Revokes (deactivates) a certificate with a reason.
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [RequirePermission("einvoice", "certificates", "edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] CertificateRevokeRequest request)
    {
        var success = await _certificateService.RevokeAsync(id, request.Reason ?? "No reason provided");
        if (!success)
            return NotFound();
        return NoContent();
    }
}

/// <summary>
/// Request body for revoking a certificate.
/// </summary>
public class CertificateRevokeRequest
{
    /// <summary>Gets or sets the reason for revocation.</summary>
    public string? Reason { get; set; }
}
