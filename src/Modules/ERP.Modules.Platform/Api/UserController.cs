// <copyright file="UserController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/users")]
[Authorize(Policy = "CompanyAdminOrSuper")]
public class UserController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUser;

    public UserController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    private static UserDto MapToDto(
        User user,
        Dictionary<Guid, string> roleNameMap,
        Dictionary<Guid, string> companyNameMap)
    {
        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.PhoneNumber,
            user.IsActive,
            user.LastLoginAt,
            user.CreatedOn,
            user.ModifiedOn,
            user.Roles.Select(r => new UserRoleAssignmentDto(
                r.RoleId,
                roleNameMap.TryGetValue(r.RoleId, out var rn) ? rn : r.RoleId.ToString(),
                r.CompanyId,
                r.CompanyId.HasValue && companyNameMap.TryGetValue(r.CompanyId.Value, out var cn) ? cn : null,
                !r.CompanyId.HasValue)).ToList());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken, u => u.Roles);

        // Company scoping: a company admin only sees users who hold a role in
        // one of their companies; a super admin sees every user.
        if (!_currentUser.IsSuperAdmin)
        {
            var allowed = _currentUser.CompanyIds;
            users = users.Where(u => u.Roles.Any(r => r.CompanyId.HasValue && allowed.Contains(r.CompanyId.Value))).ToList();
        }

        var roleNames = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var companyNames = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        var roleNameMap = roleNames.ToDictionary(r => r.Id, r => r.Name);
        var companyNameMap = companyNames.ToDictionary(c => c.Id, c => c.Name);
        return Ok(users.Select(u => MapToDto(u, roleNameMap, companyNameMap)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken, u => u.Roles);
        if (user == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && !UserBelongsToCallerCompany(user))
            return Forbid();

        var roleNames = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var companyNames = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        var roleNameMap = roleNames.ToDictionary(r => r.Id, r => r.Name);
        var companyNameMap = companyNames.ToDictionary(c => c.Id, c => c.Name);
        return Ok(MapToDto(user, roleNameMap, companyNameMap));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        // A company admin may only create users inside their own company.
        // When no company is supplied we bind the new user to the admin's single
        // company; super admins may specify any company (or none).
        Guid? companyId = request.CompanyId;
        if (!_currentUser.IsSuperAdmin)
        {
            if (companyId.HasValue && !_currentUser.CompanyIds.Contains(companyId.Value))
            {
                return Forbid();
            }

            companyId ??= _currentUser.CompanyIds.Count == 1 ? _currentUser.CompanyIds[0] : companyId;
            if (!companyId.HasValue)
                return BadRequest(ApiResponse<UserDto>.Failure(["A company-scoped administrator must operate within a single company."]));
        }

        var user = new User(
            request.Username,
            request.Email,
            request.DisplayName,
            request.PhoneNumber);

        user.SetPassword(JwtTokenService.HashPassword(request.Password));

        await _unitOfWork.Users.AddAsync(user, cancellationToken);

        if (companyId.HasValue && request.RoleId.HasValue)
        {
            user.AddRole(request.RoleId.Value, companyId.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(User),
            user.Id,
            _currentUser.UserId ?? "system",
            newValues: new { request.Username, request.Email, CompanyId = companyId },
            cancellationToken: cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            await ToDtoAsync(user, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && !UserBelongsToCallerCompany(user))
            return Forbid();

        user.Update(request.Email, request.DisplayName, request.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.SetPassword(JwtTokenService.HashPassword(request.Password));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await ToDtoAsync(user, cancellationToken));
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken, u => u.Roles);
        if (user == null)
            return NotFound();

        // The assigned company must be within the caller's scope. A super admin
        // may assign to any company; a company admin only to their own.
        Guid? companyId = request.CompanyId;
        if (!_currentUser.IsSuperAdmin && (!companyId.HasValue || !_currentUser.CompanyIds.Contains(companyId.Value)))
        {
            return Forbid();
        }

        user.AddRole(request.RoleId, companyId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken, [FromQuery] Guid? companyId = null)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken, u => u.Roles);
        if (user == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && (!companyId.HasValue || !_currentUser.CompanyIds.Contains(companyId.Value)))
        {
            return Forbid();
        }

        user.RemoveRole(roleId, companyId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && !UserBelongsToCallerCompany(user))
            return Forbid();

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Deactivated",
            nameof(User),
            user.Id,
            _currentUser.UserId ?? "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && !UserBelongsToCallerCompany(user))
            return Forbid();

        user.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && !UserBelongsToCallerCompany(user))
            return Forbid();

        user.MarkDeleted(_currentUser.UserId ?? "system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool UserBelongsToCallerCompany(User user)
    {
        var allowed = _currentUser.CompanyIds;
        return user.Roles.Any(r => r.CompanyId.HasValue && allowed.Contains(r.CompanyId.Value));
    }

    private async Task<UserDto> ToDtoAsync(User user, CancellationToken cancellationToken)
    {
        var roleNames = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var companyNames = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        var roleNameMap = roleNames.ToDictionary(r => r.Id, r => r.Name);
        var companyNameMap = companyNames.ToDictionary(c => c.Id, c => c.Name);
        return MapToDto(user, roleNameMap, companyNameMap);
    }
}
