// <copyright file="SoDConflict.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class SoDConflict : Entity
{
    protected SoDConflict() { }

    public SoDConflict(
        Guid ruleId,
        string userId,
        string module,
        string documentType,
        Guid documentId,
        string conflictType) : base(Guid.NewGuid())
    {
        RuleId = ruleId;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        DocumentType = documentType ?? throw new ArgumentNullException(nameof(documentType));
        DocumentId = documentId;
        ConflictType = conflictType ?? throw new ArgumentNullException(nameof(conflictType));
        DetectedOn = DateTimeOffset.UtcNow;
        Resolved = false;
    }

    public Guid RuleId { get; private set; }

    public string UserId { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public string DocumentType { get; private set; } = string.Empty;

    public Guid DocumentId { get; private set; }

    public string ConflictType { get; private set; } = string.Empty;

    public DateTimeOffset DetectedOn { get; private set; }

    public bool Resolved { get; private set; }

    public string? Resolution { get; private set; }

    public string? ResolvedBy { get; private set; }

    public DateTimeOffset? ResolvedOn { get; private set; }

    public void Resolve(string resolution, string resolvedBy)
    {
        Resolved = true;
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        ResolvedBy = resolvedBy ?? throw new ArgumentNullException(nameof(resolvedBy));
        ResolvedOn = DateTimeOffset.UtcNow;
    }
}
