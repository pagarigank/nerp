// <copyright file="AuditLog.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class AuditLog : Entity
{
    protected AuditLog() { }

    public AuditLog(
        string action,
        string entityType,
        Guid entityId,
        string performedBy,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null) : base(Guid.NewGuid())
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        EntityId = entityId;
        PerformedBy = performedBy ?? throw new ArgumentNullException(nameof(performedBy));
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        PerformedOn = DateTimeOffset.UtcNow;
    }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string PerformedBy { get; private set; } = string.Empty;

    public DateTimeOffset PerformedOn { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public void SetOldValues(string? oldValues)
    {
        OldValues = oldValues;
    }

    public void SetNewValues(string? newValues)
    {
        NewValues = newValues;
    }
}
