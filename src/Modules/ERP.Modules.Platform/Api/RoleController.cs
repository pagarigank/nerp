// <copyright file="RoleController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/roles")]
[Authorize(Policy = "CompanyAdminOrSuper")]
public class RoleController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUser;

    public RoleController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        return Ok(roles.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        return Ok(MapToDto(role));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            return Forbid();

        var role = new Role(request.Name, request.Description);

        await _unitOfWork.Roles.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(Role),
            role.Id,
            _currentUser.UserId ?? "system",
            newValues: new { request.Name },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, MapToDto(role));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            return Forbid();

        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.Update(request.Name, request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(role));
    }

    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermission(Guid id, [FromBody] AssignPermissionRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            return Forbid();

        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.AddPermission(request.PermissionId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(Guid id, Guid permissionId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            return Forbid();

        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.RemovePermission(permissionId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            return Forbid();

        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.MarkDeleted(_currentUser.UserId ?? "system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static RoleDto MapToDto(Role role)
    {
        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsActive,
            role.CreatedOn,
            role.ModifiedOn);
    }
}
