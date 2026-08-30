// <copyright file="RoleDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace ERP.Modules.Platform.Api;

public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn,
    IReadOnlyList<PermissionDto> Permissions);

public record CreateRoleRequest(
    string Name,
    string Description);

public record UpdateRoleRequest(
    string Name,
    string Description);

public record AssignPermissionRequest(
    Guid PermissionId);

// --- RBAC catalog/matrix DTOs for the page×action role editor ---
public record CatalogActionDto(string Action, string Label);

public record CatalogPageDto(string Page, string Label, IReadOnlyList<CatalogActionDto> Actions);

public record CatalogModuleDto(string Module, string Label, IReadOnlyList<CatalogPageDto> Pages);

/// <summary>
/// One page row in the role editor matrix: which actions the role currently has.
/// </summary>
public record RoleMatrixPageDto(
    string Page,
    string Label,
    bool View,
    bool Create,
    bool Edit,
    bool Delete);

public record RoleMatrixModuleDto(string Module, string Label, IReadOnlyList<RoleMatrixPageDto> Pages);

public record RoleMatrixDto(Guid RoleId, string RoleName, IReadOnlyList<RoleMatrixModuleDto> Modules);

public record SetRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);
