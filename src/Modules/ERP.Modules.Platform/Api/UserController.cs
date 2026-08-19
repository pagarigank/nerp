// <copyright file="UserController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/users")]
public class UserController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public UserController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken, u => u.Roles);
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

        var roleNames = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var companyNames = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        var roleNameMap = roleNames.ToDictionary(r => r.Id, r => r.Name);
        var companyNameMap = companyNames.ToDictionary(c => c.Id, c => c.Name);
        return Ok(MapToDto(user, roleNameMap, companyNameMap));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User(
            request.Username,
            request.Email,
            request.DisplayName,
            request.PhoneNumber);

        user.SetPassword(JwtTokenService.HashPassword(request.Password));

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(User),
            user.Id,
            "system",
            newValues: new { request.Username, request.Email },
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

        user.AddRole(request.RoleId, request.CompanyId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken, [FromQuery] Guid? companyId = null)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken, u => u.Roles);
        if (user == null)
            return NotFound();

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

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Deactivated",
            nameof(User),
            user.Id,
            "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

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

        user.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
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

    private async Task<UserDto> ToDtoAsync(User user, CancellationToken cancellationToken)
    {
        var roleNames = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var companyNames = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        var roleNameMap = roleNames.ToDictionary(r => r.Id, r => r.Name);
        var companyNameMap = companyNames.ToDictionary(c => c.Id, c => c.Name);
        return MapToDto(user, roleNameMap, companyNameMap);
    }
}
