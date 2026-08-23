// <copyright file="AuditSaveChangesInterceptor.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Core.Domain.Common;
using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Platform.Infrastructure;

/// <summary>
/// Captures every create/update/delete on any module context and persists an
/// audit entry to the shared <c>platform.PendingAuditLogs</c> table. The audit
/// rows are written through a dedicated <see cref="PlatformDbContext"/> (resolved
/// from DI) rather than the triggering context, so the audit trail lives solely in
/// the platform schema and no sub-ledger module has to model the table (which would
/// cause duplicate-table migration collisions on the shared database). The Separation-
/// of-Duties engine reads <c>PendingAuditLogs</c> to detect create/approve/post
/// conflicts.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;
    private readonly AsyncLocal<List<PendingAuditLog>?> _collected = new();

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser, IServiceProvider serviceProvider)
    {
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        _collected.Value = CollectAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _collected.Value = CollectAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var entries = _collected.Value;
        _collected.Value = null;
        if (entries is { Count: > 0 })
        {
            await PersistAsync(entries, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        // Synchronous SaveChanges path: persist within a fresh scope.
        var entries = _collected.Value;
        _collected.Value = null;
        if (entries is { Count: > 0 })
        {
            PersistAsync(entries, CancellationToken.None).GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    private static string? SerializeProperties(IEnumerable<PropertyEntry> properties)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in properties)
        {
            dict[prop.Metadata.Name] = AuditSensitiveValueRedactor.Redact(prop.Metadata.Name, prop.CurrentValue);
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict) : null;
    }

    private static Guid GetPrimaryKeyValue(EntityEntry entry)
    {
        var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        if (pkProp?.CurrentValue is Guid id)
        {
            return id;
        }

        return Guid.Empty;
    }

    private async Task PersistAsync(IReadOnlyList<PendingAuditLog> entries, CancellationToken cancellationToken)
    {
        // Resolve a PlatformDbContext from an isolated scope so the audit write is
        // independent of the triggering context's transaction.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        platform.PendingAuditLogs.AddRange(entries);
        await platform.SaveChangesAsync(cancellationToken);
    }

    private List<PendingAuditLog> CollectAuditEntries(DbContext? context)
    {
        var auditEntries = new List<PendingAuditLog>();

        if (context == null)
        {
            return auditEntries;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.Entity is PendingAuditLog)
            {
                continue;
            }

            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var entityType = entry.Entity.GetType().Name;
            var entityId = GetPrimaryKeyValue(entry);

            var auditEntry = new PendingAuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                PerformedBy = _currentUser.UserId ?? "system",
                CorrelationId = _currentUser.CorrelationId,
                PerformedOn = DateTimeOffset.UtcNow,
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntry.Action = "Created";
                    auditEntry.NewValues = SerializeProperties(entry.Properties);
                    break;

                case EntityState.Modified:
                    auditEntry.Action = "Updated";
                    var original = new Dictionary<string, object?>();
                    var current = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified || prop.Metadata.IsPrimaryKey())
                        {
                            var name = prop.Metadata.Name;
                            original[name] = AuditSensitiveValueRedactor.Redact(name, prop.OriginalValue);
                            current[name] = AuditSensitiveValueRedactor.Redact(name, prop.CurrentValue);
                        }
                    }

                    auditEntry.OldValues = JsonSerializer.Serialize(original);
                    auditEntry.NewValues = JsonSerializer.Serialize(current);
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is AuditableAggregateRoot auditableRoot && !auditableRoot.IsDeleted)
                    {
                        auditEntry.Action = "SoftDeleted";
                    }
                    else
                    {
                        auditEntry.Action = "Deleted";
                    }

                    auditEntry.OldValues = SerializeProperties(entry.Properties);
                    break;
            }

            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }
}

public class PendingAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string PerformedBy { get; set; } = string.Empty;

    public DateTimeOffset PerformedOn { get; set; }

    public string? CorrelationId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }
}
