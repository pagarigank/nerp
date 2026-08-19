// <copyright file="SodService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Infrastructure;

public interface ISodService
{
    Task<bool> CheckConflictAsync(string module, string documentType, string userId, string action, decimal amount = 0, CancellationToken cancellationToken = default);
    Task LogConflictAsync(Guid ruleId, string userId, string module, string documentType, Guid documentId, string conflictType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SoDConflict>> GetConflictsAsync(string? userId = null, bool? resolved = null, CancellationToken cancellationToken = default);
    Task ResolveConflictAsync(Guid conflictId, string resolution, string resolvedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SoDRule>> GetActiveRulesAsync(string? module = null, CancellationToken cancellationToken = default);
    Task<bool> HasConflictingActionAsync(Guid userId, string module, string documentType, string documentId, string action, CancellationToken cancellationToken = default);
}

public class SodService : ISodService
{
    private readonly PlatformDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public SodService(PlatformDbContext context, IAuditLogService auditLogService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<bool> CheckConflictAsync(
        string module,
        string documentType,
        string userId,
        string action,
        decimal amount = 0,
        CancellationToken cancellationToken = default)
    {
        var matchingRules = await _context.SoDRules
            .Where(r => r.Module == module && r.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var rule in matchingRules)
        {
            if (rule.ThresholdAmount.HasValue && amount < rule.ThresholdAmount.Value)
                continue;

            if (!string.IsNullOrEmpty(rule.DocumentType) && rule.DocumentType != documentType)
                continue;

            if (rule.ActionA == action || rule.ActionB == action)
            {
                var conflictingAction = rule.ActionA == action ? rule.ActionB : rule.ActionA;

                // The audit interceptor writes to PendingAuditLogs (platform
                // schema); AuditLogs is only populated by explicit service calls.
                // The real activity trail therefore lives in PendingAuditLogs, so
                // the conflict check must read from there to actually detect a
                // prior conflicting action by the same user.
                var hasConflict = await _context.PendingAuditLogs
                    .AnyAsync(a =>
                        a.EntityType == documentType &&
                        a.Action == conflictingAction &&
                        a.PerformedBy == userId,
                        cancellationToken);

                if (hasConflict)
                    return true;
            }
        }

        return false;
    }

    public async Task LogConflictAsync(
        Guid ruleId,
        string userId,
        string module,
        string documentType,
        Guid documentId,
        string conflictType,
        CancellationToken cancellationToken = default)
    {
        var conflict = new SoDConflict(ruleId, userId, module, documentType, documentId, conflictType);
        _context.SoDConflicts.Add(conflict);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "SoDConflictDetected",
            nameof(SoDConflict),
            conflict.Id,
            "system",
            newValues: new { ruleId, userId, module, documentType, conflictType },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SoDConflict>> GetConflictsAsync(
        string? userId = null,
        bool? resolved = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SoDConflicts.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(c => c.UserId == userId);
        }

        if (resolved.HasValue)
        {
            query = query.Where(c => c.Resolved == resolved.Value);
        }

        return await query.OrderByDescending(c => c.DetectedOn).ToListAsync(cancellationToken);
    }

    public async Task ResolveConflictAsync(Guid conflictId, string resolution, string resolvedBy, CancellationToken cancellationToken = default)
    {
        var conflict = await _context.SoDConflicts.FindAsync(new object[] { conflictId }, cancellationToken)
            ?? throw new InvalidOperationException($"SoD conflict {conflictId} not found.");

        conflict.Resolve(resolution, resolvedBy);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "SoDConflictResolved",
            nameof(SoDConflict),
            conflictId,
            resolvedBy,
            newValues: new { resolution },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SoDRule>> GetActiveRulesAsync(string? module = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SoDRules.Where(r => r.IsActive);

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(r => r.Module == module);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> HasConflictingActionAsync(Guid userId, string module, string documentType, string documentId, string action, CancellationToken cancellationToken = default)
    {
        return await _context.SoDRules
            .AnyAsync(r => r.Module == module && r.IsActive &&
                (r.ActionA == action || r.ActionB == action) &&
                (r.DocumentType == null || r.DocumentType == documentType),
                cancellationToken);
    }
}
