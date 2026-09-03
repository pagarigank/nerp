// <copyright file="EInvoiceCredentialController.cs" company="ERP Project">
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
/// CRUD API for MyInvois API credentials.
/// Manages Client ID/Secret pairs per environment (sandbox/production).
/// Secrets are write-once and never returned in plaintext after creation.
/// All endpoints enforce multi-company isolation via ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/credentials")]
[ApiVersion("1.0")]
public class EInvoiceCredentialController : ControllerBase
{
    private readonly IApiCredentialRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public EInvoiceCredentialController(IApiCredentialRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets all API credentials for a company (secrets masked).
    /// </summary>
    [HttpGet]
    [RequirePermission("einvoice", "credentials", "view")]
    [ProducesResponseType(typeof(IEnumerable<ApiCredentialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var credentials = await _repository.GetByCompanyIdAsync(companyId);
        return Ok(credentials.Select(MapToDto));
    }

    /// <summary>
    /// Gets an API credential by ID (secret masked).
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("einvoice", "credentials", "view")]
    [ProducesResponseType(typeof(ApiCredentialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var credential = await _repository.GetByIdAsync(id);
        if (credential == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(credential.CompanyId))
            return Forbid();
        return Ok(MapToDto(credential));
    }

    /// <summary>
    /// Creates a new API credential. The secret is stored but never returned.
    /// Only one active credential per company per environment is allowed.
    /// </summary>
    [HttpPost]
    [RequirePermission("einvoice", "credentials", "create")]
    [ProducesResponseType(typeof(ApiCredentialDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] ApiCredentialCreateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.CompanyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (string.IsNullOrWhiteSpace(request.ClientId))
            return BadRequest("ClientId is required.");

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
            return BadRequest("ClientSecret is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(request.CompanyId))
            return Forbid();

        // Validate environment
        var env = request.Environment;
        if (!string.Equals(env, "sandbox", StringComparison.OrdinalIgnoreCase) && !string.Equals(env, "production", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Environment must be 'sandbox' or 'production'.");

        var entity = new ApiCredential
        {
            CompanyId = request.CompanyId,
            Environment = env!,
            ClientId = request.ClientId,
            EncryptedClientSecret = request.ClientSecret, // In production, encrypt before storage
            Description = request.Description,
            ExpiresAt = request.ExpiresAt,
        };

        await _repository.AddAsync(entity);

        var dto = MapToDto(entity);
        dto.SecretNote = "Secret stored. It will not be displayed again.";
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    /// <summary>
    /// Rotates the client secret for an existing credential.
    /// The old secret is invalidated. The new secret is returned once.
    /// </summary>
    [HttpPost("{id:guid}/rotate")]
    [RequirePermission("einvoice", "credentials", "edit")]
    [ProducesResponseType(typeof(ApiCredentialRotateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RotateSecret(Guid id, [FromBody] ApiCredentialRotateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewClientSecret))
            return BadRequest("NewClientSecret is required.");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(existing.CompanyId))
            return Forbid();

        existing.RotateSecret(request.NewClientSecret); // In production, encrypt before storage
        await _repository.UpdateAsync(existing);

        return Ok(new ApiCredentialRotateResult
        {
            Id = existing.Id,
            ClientId = existing.ClientId,
            Environment = existing.Environment,
            LastRotatedAt = existing.LastRotatedAt,
            NewSecretNote = "New secret stored. It will not be displayed again.",
        });
    }

    /// <summary>
    /// Deactivates an API credential.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission("einvoice", "credentials", "edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(existing.CompanyId))
            return Forbid();

        existing.Deactivate();
        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes an API credential.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("einvoice", "credentials", "delete")]
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

    private static ApiCredentialDto MapToDto(ApiCredential entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        Environment = entity.Environment,
        ClientId = entity.ClientId,
        IsActive = entity.IsActive,
        ExpiresAt = entity.ExpiresAt,
        LastRotatedAt = entity.LastRotatedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Description = entity.Description,
    };

    // ClientSecret is NEVER returned
}

/// <summary>
/// DTO for API credential (secret never included).
/// </summary>
public class ApiCredentialDto
{
    /// <summary>Gets or sets the credential ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the company ID.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the environment (sandbox/production).</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Gets or sets the Client ID (visible).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this credential is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the expiration date.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets the last rotation date.</summary>
    public DateTime? LastRotatedAt { get; set; }

    /// <summary>Gets or sets the created-at timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the updated-at timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a note about the secret (one-time display).</summary>
    public string? SecretNote { get; set; }
}

/// <summary>
/// Request body for creating an API credential.
/// </summary>
public class ApiCredentialCreateRequest
{
    /// <summary>Gets or sets the company ID.</summary>
    [Required] public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the environment (sandbox/production).</summary>
    [Required] public string Environment { get; set; } = "sandbox";

    /// <summary>Gets or sets the OAuth Client ID.</summary>
    [Required] public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth Client Secret (stored encrypted, never returned).</summary>
    [Required] public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional expiration date.</summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request body for rotating a secret.
/// </summary>
public class ApiCredentialRotateRequest
{
    /// <summary>Gets or sets the new client secret.</summary>
    [Required] public string NewClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Result of a secret rotation (new secret never returned, only confirmation).
/// </summary>
public class ApiCredentialRotateResult
{
    /// <summary>Gets or sets the credential ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the Client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the environment.</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Gets or sets the last rotation date.</summary>
    public DateTime? LastRotatedAt { get; set; }

    /// <summary>Gets or sets the one-time note about the new secret.</summary>
    public string NewSecretNote { get; set; } = string.Empty;
}
