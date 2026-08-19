// <copyright file="AuditLogDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    Guid EntityId,
    string PerformedBy,
    DateTimeOffset PerformedOn,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId,
    string? OldValues,
    string? NewValues);
