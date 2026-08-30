// <copyright file="RoleController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain;
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
        var roles = await _unitOfWork.Roles.GetAllAsync(cancellationToken, r => r.Permissions);
        var dtos = new List<RoleDto>();
        foreach (var role in roles)
            dtos.Add(await MapToDto(role, cancellationToken));
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken, r => r.Permissions);
        if (role == null)
            return NotFound();

        return Ok(await MapToDto(role, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
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

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, await MapToDto(role, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.Update(request.Name, request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await MapToDto(role, cancellationToken));
    }

    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermission(Guid id, [FromBody] AssignPermissionRequest request, CancellationToken cancellationToken)
    {
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
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.RemovePermission(permissionId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Bulk-replace a role's permissions. Replaces the entire permission set with
    /// the supplied ids — used by the role editor's page×action matrix.
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken, r => r.Permissions);
        if (role == null)
            return NotFound();

        var wanted = (request.PermissionIds ?? Array.Empty<Guid>()).Distinct().ToHashSet();
        var current = role.Permissions.Select(p => p.PermissionId).ToHashSet();

        foreach (var pid in wanted.Except(current))
            role.AddPermission(pid);
        foreach (var pid in current.Except(wanted))
            role.RemovePermission(pid);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "PermissionsUpdated",
            nameof(Role),
            role.Id,
            _currentUser.UserId ?? "system",
            newValues: new { count = wanted.Count },
            cancellationToken: cancellationToken);

        return NoContent();
    }

    /// <summary>Full page×action catalog used to build the role editor UI.</summary>
    [HttpGet("catalog")]
    public ActionResult<IReadOnlyList<CatalogModuleDto>> GetCatalog()
    {
        var modules = PermissionCatalog.Modules.Select(mod => new CatalogModuleDto(
            mod.Module,
            mod.Label,
            mod.Pages.Select(p => new CatalogPageDto(
                p.Page,
                p.Label,
                PermissionCatalog.Actions.Select(a => new CatalogActionDto(a, char.ToUpperInvariant(a[0]) + a[1..])).ToList())).ToList())).ToList();

        return Ok(modules);
    }

    /// <summary>
    /// The role editor payload: the full catalog with a granted flag per action
    /// for the requested role.
    /// </summary>
    [HttpGet("{id:guid}/matrix")]
    public async Task<ActionResult<RoleMatrixDto>> GetMatrix(Guid id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken, r => r.Permissions);
        if (role == null)
            return NotFound();

        var permIds = role.Permissions.Select(rp => rp.PermissionId).ToHashSet();
        var granted = (await _unitOfWork.Permissions.GetAllAsync(cancellationToken))
            .Where(p => permIds.Contains(p.Id))
            .Select(p => (p.Page, p.Action))
            .ToHashSet();

        var modules = PermissionCatalog.Modules.Select(mod => new RoleMatrixModuleDto(
            mod.Module,
            mod.Label,
            mod.Pages.Select(p => new RoleMatrixPageDto(
                p.Page,
                p.Label,
                granted.Contains((p.Page, PermissionCatalog.View)),
                granted.Contains((p.Page, PermissionCatalog.Create)),
                granted.Contains((p.Page, PermissionCatalog.Edit)),
                granted.Contains((p.Page, PermissionCatalog.Delete)))).ToList())).ToList();

        return Ok(new RoleMatrixDto(role.Id, role.Name, modules));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (role == null)
            return NotFound();

        role.MarkDeleted(_currentUser.UserId ?? "system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Clone a role: copies its name (optionally overridden) and full permission
    /// set into a new role. Useful for "tweak an existing role" admin flows.
    /// </summary>
    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<RoleDto>> Clone(Guid id, [FromBody] CloneRoleRequest request, CancellationToken cancellationToken)
    {
        var source = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken, r => r.Permissions);
        if (source == null)
            return NotFound();

        var name = string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} (Copy)" : request.Name;
        var clone = new Role(name, source.Description);
        foreach (var rp in source.Permissions)
            clone.AddPermission(rp.PermissionId);

        await _unitOfWork.Roles.AddAsync(clone, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Cloned",
            nameof(Role),
            clone.Id,
            _currentUser.UserId ?? "system",
            newValues: new { From = source.Name, name },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = clone.Id }, await MapToDto(clone, cancellationToken));
    }

    /// <summary>
    /// Bulk-grant every page-scoped permission to a role in a single call (the
    /// "grant all" editor helper). Existing grants are left untouched.
    /// </summary>
    [HttpPost("{id:guid}/permissions/all")]
    public async Task<IActionResult> GrantAllPermissions(Guid id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken, r => r.Permissions);
        if (role == null)
            return NotFound();

        var current = role.Permissions.Select(p => p.PermissionId).ToHashSet();
        var all = (await _unitOfWork.Permissions.GetAllAsync(cancellationToken)).ToList();
        var toAdd = all.Where(p => !current.Contains(p.Id)).Select(p => p.Id).ToList();
        foreach (var pid in toAdd)
            role.AddPermission(pid);

        var added = toAdd.Count;
        if (added > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "PermissionsGrantedAll",
            nameof(Role),
            role.Id,
            _currentUser.UserId ?? "system",
            newValues: new { added },
            cancellationToken: cancellationToken);

        return NoContent();
    }

    private async Task<RoleDto> MapToDto(Role role, CancellationToken cancellationToken)
    {
        var permissionIds = role.Permissions.Select(p => p.PermissionId).ToHashSet();
        var perms = permissionIds.Count == 0
            ? new Dictionary<Guid, Permission>()
            : (await _unitOfWork.Permissions.GetAllAsync(cancellationToken))
                .Where(p => permissionIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p);

        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsActive,
            role.CreatedOn,
            role.ModifiedOn,
            role.Permissions
                .Select(rp => perms.TryGetValue(rp.PermissionId, out var perm)
                    ? new PermissionDto(rp.PermissionId, perm.Module, perm.Page, perm.Action, perm.Code, perm.Description)
                    : new PermissionDto(rp.PermissionId, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty))
                .ToList());
    }
}
