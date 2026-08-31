// <copyright file="AccessRequestController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Security.Claims;
using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Api;

/// <summary>
/// Self-service access requests. Anyone may <c>POST /request</c> (public
/// registration). Reviewing (list/approve/reject) requires a company
/// administrator or the super admin — enforced by the <c>CompanyAdminOrSuper</c>
/// policy. Approvers are additionally scoped to their own company: a company
/// admin can only review/approve requests for their company, the super admin
/// can review any.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/access-requests")]
public class AccessRequestController : ControllerBase
{
    private readonly PlatformDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AccessRequestController(PlatformDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Public registration: submit a request for access.</summary>
    [HttpPost("request")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AccessRequestDto>>> Submit(
        [FromBody] SubmitAccessRequest body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.FullName) ||
            string.IsNullOrWhiteSpace(body.Email) ||
            string.IsNullOrWhiteSpace(body.Username) ||
            string.IsNullOrWhiteSpace(body.Password) ||
            body.CompanyId == Guid.Empty)
        {
            return BadRequest(ApiResponse<AccessRequestDto>.Failure(
                ["Full name, email, username, password and company are required."]));
        }

        if (body.Password.Length < 8)
            return BadRequest(ApiResponse<AccessRequestDto>.Failure(["Password must be at least 8 characters."]));

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == body.CompanyId, cancellationToken);
        if (company is null)
            return BadRequest(ApiResponse<AccessRequestDto>.Failure(["Selected company does not exist."]));

        // Reject duplicates (pending request, or an existing active user).
        // Compared in-memory using ordinal ignore-case to stay analyzer-clean
        // (CultureInfo-aware methods like ToUpper/Equals are not SQL-translatable
        // on this EF8 + SQL Server 180 build). The Users table is small in this
        // deployment; email uniqueness is also enforced by a DB unique constraint.
#pragma warning disable CA1311 // intentional ordinal comparison for dup pre-check
        var emailUpper = body.Email.Trim().ToUpperInvariant();
        var existingPending = await _db.UserAccessRequests
            .Where(r => r.Status == AccessRequestStatus.Pending && r.DeletedOn == null)
            .ToListAsync(cancellationToken);
        var dupPending = existingPending.Any(r => r.Email != null && r.Email.Equals(emailUpper, StringComparison.OrdinalIgnoreCase));
        if (dupPending)
            return Conflict(ApiResponse<AccessRequestDto>.Failure(["A pending request for this email already exists."]));

        var existingUsers = await _db.Users.ToListAsync(cancellationToken);
        var dupUser = existingUsers.Any(u =>
            (u.Email != null && u.Email.Equals(emailUpper, StringComparison.OrdinalIgnoreCase)) ||
            u.Username == body.Username);
#pragma warning restore CA1311
        if (dupUser)
            return Conflict(ApiResponse<AccessRequestDto>.Failure(["A user with this email or username already exists."]));

        var entity = new UserAccessRequest(
            body.FullName.Trim(),
            body.Email.Trim(),
            body.Username.Trim(),
            JwtTokenService.HashPassword(body.Password),
            body.CompanyId,
            body.RequestedRole?.Trim() ?? "Staff",
            body.PhoneNumber?.Trim(),
            body.Reason?.Trim());
        entity.CreatedBy = body.Email;

        _db.UserAccessRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AccessRequestDto>.Success(ToDto(entity)));
    }

    /// <summary>List requests. Company admins see only their company; super admin sees all.</summary>
    [HttpGet]
    [Authorize(Policy = "CompanyAdminOrSuper")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccessRequestDto>>>> List(
        CancellationToken cancellationToken)
    {
        var query = _db.UserAccessRequests.Where(r => r.DeletedOn == null);
        if (!_currentUser.IsSuperAdmin)
            query = query.Where(r => _currentUser.CompanyIds.Contains(r.CompanyId));

        var items = await query.OrderByDescending(r => r.CreatedOn).ToListAsync(cancellationToken);
        var companyNames = await _db.Companies.ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccessRequestDto>>.Success(
            items.Select(r => ToDto(r, companyNames)).ToList()));
    }

    /// <summary>Pending-request count for the caller's scope (for dashboard cards).</summary>
    [HttpGet("pending-count")]
    [Authorize(Policy = "CompanyAdminOrSuper")]
    public async Task<ActionResult<ApiResponse<int>>> PendingCount(CancellationToken cancellationToken)
    {
        var query = _db.UserAccessRequests.Where(r => r.DeletedOn == null && r.Status == AccessRequestStatus.Pending);
        if (!_currentUser.IsSuperAdmin)
            query = query.Where(r => _currentUser.CompanyIds.Contains(r.CompanyId));
        return Ok(ApiResponse<int>.Success(await query.CountAsync(cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CompanyAdminOrSuper")]
    public async Task<ActionResult<ApiResponse<AccessRequestDto>>> Get(
        Guid id, CancellationToken cancellationToken)
    {
        var req = await _db.UserAccessRequests.FirstOrDefaultAsync(r => r.Id == id && r.DeletedOn == null, cancellationToken);
        if (req is null)
            return NotFound(ApiResponse<AccessRequestDto>.Failure(["Request not found."]));

        if (!CanReview(req.CompanyId))
            return Forbid();

        var companyName = (await _db.Companies.FirstOrDefaultAsync(c => c.Id == req.CompanyId, cancellationToken))?.Name;
        return Ok(ApiResponse<AccessRequestDto>.Success(ToDto(req, companyName)));
    }

    /// <summary>Approve a request and provision the user account.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "CompanyAdminOrSuper")]
    public async Task<ActionResult<ApiResponse<AccessRequestDto>>> Approve(
        Guid id, [FromBody] ReviewAccessRequest review, CancellationToken cancellationToken)
    {
        var req = await _db.UserAccessRequests.FirstOrDefaultAsync(r => r.Id == id && r.DeletedOn == null, cancellationToken);
        if (req is null)
            return NotFound(ApiResponse<AccessRequestDto>.Failure(["Request not found."]));

        if (!CanReview(req.CompanyId))
            return Forbid();

        if (req.Status != AccessRequestStatus.Pending)
            return BadRequest(ApiResponse<AccessRequestDto>.Failure(["This request has already been reviewed."]));

        // Resolve the role to assign. Prefer the approver's override, else the
        // requester's requested role. Never allow assigning an admin role.
        Guid? roleId = await ResolveRoleAsync(review.RoleId, req.RequestedRole, cancellationToken);
        if (review.RoleId.HasValue && roleId != review.RoleId)
            return BadRequest(ApiResponse<AccessRequestDto>.Failure(["The selected role is not assignable for this company."]));

        var user = new User(req.Username, req.Email, req.FullName, req.PhoneNumber);
        user.SetPassword(req.PasswordHash);
        user.Activate();
        user.CreatedBy = req.Email;
        _db.Users.Add(user);

        if (roleId.HasValue)
            user.AddRole(roleId.Value, req.CompanyId);

        var reviewerId = _currentUser.UserId;
        var reviewerGuid = Guid.TryParse(reviewerId, out var g) ? g : Guid.Empty;
        req.Approve(reviewerGuid, review.Notes);

        await _db.SaveChangesAsync(cancellationToken);

        var companyName = (await _db.Companies.FirstOrDefaultAsync(c => c.Id == req.CompanyId, cancellationToken))?.Name;
        return Ok(ApiResponse<AccessRequestDto>.Success(ToDto(req, companyName)));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "CompanyAdminOrSuper")]
    public async Task<ActionResult<ApiResponse<AccessRequestDto>>> Reject(
        Guid id, [FromBody] ReviewAccessRequest review, CancellationToken cancellationToken)
    {
        var req = await _db.UserAccessRequests.FirstOrDefaultAsync(r => r.Id == id && r.DeletedOn == null, cancellationToken);
        if (req is null)
            return NotFound(ApiResponse<AccessRequestDto>.Failure(["Request not found."]));

        if (!CanReview(req.CompanyId))
            return Forbid();

        if (req.Status != AccessRequestStatus.Pending)
            return BadRequest(ApiResponse<AccessRequestDto>.Failure(["This request has already been reviewed."]));

        var rejecterGuid = Guid.TryParse(_currentUser.UserId, out var rg) ? rg : Guid.Empty;
        req.Reject(rejecterGuid, review.Notes);
        await _db.SaveChangesAsync(cancellationToken);

        var companyName = (await _db.Companies.FirstOrDefaultAsync(c => c.Id == req.CompanyId, cancellationToken))?.Name;
        return Ok(ApiResponse<AccessRequestDto>.Success(ToDto(req, companyName)));
    }

    private static AccessRequestDto ToDto(UserAccessRequest r, string? companyName = null)
        => new AccessRequestDto(
            r.Id,
            r.FullName,
            r.Email,
            r.Username,
            r.CompanyId,
            companyName,
            r.RequestedRole,
            r.PhoneNumber,
            r.Reason,
            r.Status.ToString(),
            r.ReviewedOn,
            r.ReviewNotes,
            r.CreatedOn);

    private static AccessRequestDto ToDto(UserAccessRequest r, Dictionary<Guid, string> companyNames)
        => ToDto(r, companyNames.TryGetValue(r.CompanyId, out var n) ? n : null);

    private bool CanReview(Guid companyId)
    {
        return _currentUser.IsSuperAdmin || _currentUser.CompanyIds.Contains(companyId);
    }

    /// <summary>
    /// Maps a desired role id/name to a real Role id within the target company.
    /// Returns null when no role is appropriate (the admin can assign later).
    /// Admin/Administrator roles are never auto-assigned.
    /// </summary>
    private async Task<Guid?> ResolveRoleAsync(Guid? requestedRoleId, string requestedRoleName, CancellationToken cancellationToken)
    {
        var adminNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin", "Administrator", "Super Admin" };

        if (requestedRoleId.HasValue)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == requestedRoleId.Value && r.IsActive, cancellationToken);
            if (role is null || adminNames.Contains(role.Name) || adminNames.Contains(role.Description))
                return null;
            return role.Id;
        }

        if (!string.IsNullOrWhiteSpace(requestedRoleName))
        {
            var role = await _db.Roles.FirstOrDefaultAsync(
                r => r.IsActive && (r.Name == requestedRoleName || r.Description == requestedRoleName), cancellationToken);
            if (role is not null && !adminNames.Contains(role.Name) && !adminNames.Contains(role.Description))
                return role.Id;
        }

        // Default: first non-admin active role (best-effort).
        var any = await _db.Roles.FirstOrDefaultAsync(r => r.IsActive, cancellationToken);
        return any is not null && !adminNames.Contains(any.Name) && !adminNames.Contains(any.Description) ? any.Id : null;
    }
}

public record SubmitAccessRequest(
    string FullName,
    string Email,
    string Username,
    string Password,
    Guid CompanyId,
    string? RequestedRole = null,
    string? PhoneNumber = null,
    string? Reason = null);

public record ReviewAccessRequest(Guid? RoleId = null, string? Notes = null);

public record AccessRequestDto(
    Guid Id,
    string FullName,
    string Email,
    string Username,
    Guid CompanyId,
    string? CompanyName,
    string RequestedRole,
    string? PhoneNumber,
    string? Reason,
    string Status,
    DateTimeOffset? ReviewedOn,
    string? ReviewNotes,
    DateTimeOffset CreatedOn);
