// <copyright file="RolePermissionDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Api;

public record RolePermissionDto(
    Guid RoleId,
    string RoleName,
    string Description,
    bool IsActive,
    IReadOnlyList<PermissionDto> Permissions);