// <copyright file="PermissionDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Api;

public record PermissionDto(
    Guid Id,
    string Module,
    string Page,
    string Action,
    string Code,
    string Description);