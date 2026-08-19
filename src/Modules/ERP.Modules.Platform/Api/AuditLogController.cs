// <copyright file="AuditLogController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/audit-logs")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetByEntity(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetByEntityAsync(entityType, entityId, cancellationToken);
        return Ok(logs.Select(MapToDto).ToList());
    }

    [HttpGet("user/{performedBy}")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetByUser(
        string performedBy,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetByUserAsync(performedBy, from, to, cancellationToken);
        return Ok(logs.Select(MapToDto).ToList());
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto(
            log.Id,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.PerformedBy,
            log.PerformedOn,
            log.IpAddress,
            log.UserAgent,
            log.CorrelationId,
            log.OldValues,
            log.NewValues);
    }
}
