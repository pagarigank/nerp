// <copyright file="UserRoleDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record UserRoleDto(
    Guid UserId,
    string UserName,
    string Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles);