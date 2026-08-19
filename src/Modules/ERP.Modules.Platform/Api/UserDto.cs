// <copyright file="UserDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn,
    IReadOnlyList<UserRoleAssignmentDto> Roles);

public record UserRoleAssignmentDto(
    Guid RoleId,
    string RoleName,
    Guid? CompanyId,
    string? CompanyName,
    bool IsGlobal);

public record CreateUserRequest(
    string Username,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    string Password);

public record UpdateUserRequest(
    string Email,
    string DisplayName,
    string? PhoneNumber,
    string? Password = null);

public record AssignRoleRequest(
    Guid RoleId,
    Guid? CompanyId = null);
