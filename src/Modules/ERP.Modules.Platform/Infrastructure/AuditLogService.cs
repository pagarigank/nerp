// <copyright file="AuditLogService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Infrastructure;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, Guid entityId, string performedBy, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(string performedBy, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default);
}

public class AuditLogService : IAuditLogService
{
    private readonly IRepository<AuditLog> _auditLogRepository;

    public AuditLogService(IRepository<AuditLog> auditLogRepository)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid entityId,
        string performedBy,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(action, entityType, entityId, performedBy, ipAddress, userAgent, correlationId);

        if (oldValues != null)
        {
            auditLog.SetOldValues(JsonSerializer.Serialize(oldValues));
        }

        if (newValues != null)
        {
            auditLog.SetNewValues(JsonSerializer.Serialize(newValues));
        }

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.FindAsync(
            x => x.EntityType == entityType && x.EntityId == entityId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(string performedBy, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.FindAsync(
            x => x.PerformedBy == performedBy && x.PerformedOn >= from && x.PerformedOn <= to,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        var all = await _auditLogRepository.FindAsync(_ => true, cancellationToken);
        return all.OrderByDescending(x => x.PerformedOn).Take(take).ToList();
    }
}
