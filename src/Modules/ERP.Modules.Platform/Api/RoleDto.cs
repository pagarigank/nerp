// <copyright file="RoleDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateRoleRequest(
    string Name,
    string Description);

public record UpdateRoleRequest(
    string Name,
    string Description);

public record AssignPermissionRequest(
    Guid PermissionId);
