// <copyright file="ApiKeyController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/api-keys")]
public class ApiKeyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUser;

    public ApiKeyController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> GetAll([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var keys = await _unitOfWork.ApiKeys.FindAsync(k => k.CompanyId == companyId, cancellationToken);
        return Ok(keys.OrderBy(k => k.Name).Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiKeyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var key = await _unitOfWork.ApiKeys.GetByIdAsync(id, cancellationToken);
        if (key == null)
            return NotFound();
        return Ok(MapToDto(key));
    }

    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedDto>> Create([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var owner = _currentUser.UserId ?? "system";
        var key = new ApiKey(request.CompanyId, request.Name, owner, request.Scopes, request.ExpiresOn);

        // Generate a cryptographically random secret: "erp_<prefix>_<random>".
        var secret = GenerateSecret(out var prefix);
        key.SetSecret(HashSecret(secret), prefix);

        await _unitOfWork.ApiKeys.AddAsync(key, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(ApiKey),
            key.Id,
            owner,
            newValues: new { request.Name, Scopes = request.Scopes, request.ExpiresOn },
            cancellationToken: cancellationToken);

        // The plaintext secret is returned exactly once and never stored.
        return CreatedAtAction(
            nameof(GetById),
            new { id = key.Id },
            new ApiKeyCreatedDto(key.Id, key.Name, key.KeyPrefix, secret, key.ExpiresOn));
    }

    [HttpPut("{id:guid}/scopes")]
    public async Task<ActionResult<ApiKeyDto>> UpdateScopes(Guid id, [FromBody] UpdateApiKeyScopesRequest request, CancellationToken cancellationToken)
    {
        var key = await _unitOfWork.ApiKeys.GetByIdAsync(id, cancellationToken);
        if (key == null)
            return NotFound();
        key.UpdateScopes(request.Scopes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(key));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var key = await _unitOfWork.ApiKeys.GetByIdAsync(id, cancellationToken);
        if (key == null)
            return NotFound();
        key.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var key = await _unitOfWork.ApiKeys.GetByIdAsync(id, cancellationToken);
        if (key == null)
            return NotFound();
        key.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var key = await _unitOfWork.ApiKeys.GetByIdAsync(id, cancellationToken);
        if (key == null)
            return NotFound();
        key.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string GenerateSecret(out string prefix)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var body = Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
        prefix = body[..6];
        return $"erp_{prefix}_{body}";
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static ApiKeyDto MapToDto(ApiKey key) => new(
        key.Id, key.CompanyId, key.Name, key.KeyPrefix, key.Scopes, key.IsActive,
        key.ExpiresOn, key.LastUsedOn, key.CreatedOn, key.ModifiedOn);
}

public record ApiKeyDto(
    Guid Id, Guid CompanyId, string Name, string KeyPrefix, IReadOnlyList<string> Scopes,
    bool IsActive, DateTimeOffset? ExpiresOn, DateTimeOffset? LastUsedOn, DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record ApiKeyCreatedDto(Guid Id, string Name, string KeyPrefix, string Secret, DateTimeOffset? ExpiresOn);

public record CreateApiKeyRequest(Guid CompanyId, string Name, IReadOnlyList<string> Scopes, DateTimeOffset? ExpiresOn);

public record UpdateApiKeyScopesRequest(IReadOnlyList<string> Scopes);
