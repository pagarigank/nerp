// <copyright file="ApiKey.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// Scoped machine identity for integrations (EDI, webhooks, external API clients).
/// The key secret is stored as a SHA-256 hash; the plaintext is only ever returned
/// once at creation time. Rotation issues a new secret and expires the previous one.
/// </summary>
public class ApiKey : AuditableAggregateRoot
{
    protected ApiKey() { }

    public ApiKey(
        Guid companyId,
        string name,
        string ownerUserId,
        IReadOnlyList<string> scopes,
        DateTimeOffset? expiresOn = null) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        OwnerUserId = ownerUserId ?? throw new ArgumentNullException(nameof(ownerUserId));
        Scopes = scopes?.ToList() ?? [];
        ExpiresOn = expiresOn;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string OwnerUserId { get; private set; } = string.Empty;

    /// <summary>SHA-256 hash of the secret (lowercase hex). Never the plaintext.</summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>Public prefix shown in UIs to help identify a key without revealing it.</summary>
    public string KeyPrefix { get; private set; } = string.Empty;

#pragma warning disable CA1002 // EF primitive collection requires a mutable List<string>

    /// <summary>Scoped permissions for this machine identity (e.g. "platform:read", "om:write").</summary>
    public List<string> Scopes { get; private set; } = [];
#pragma warning restore CA1002

    public bool IsActive { get; private set; }

    public DateTimeOffset? ExpiresOn { get; private set; }

    public DateTimeOffset? LastUsedOn { get; private set; }

    public void SetSecret(string keyHash, string keyPrefix)
    {
        KeyHash = keyHash ?? throw new ArgumentNullException(nameof(keyHash));
        KeyPrefix = keyPrefix ?? throw new ArgumentNullException(nameof(keyPrefix));
    }

    public void RecordUse() => LastUsedOn = DateTimeOffset.UtcNow;

    public void UpdateScopes(IReadOnlyList<string> scopes) => Scopes = scopes?.ToList() ?? [];

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool IsExpired() => ExpiresOn.HasValue && ExpiresOn.Value <= DateTimeOffset.UtcNow;
}
