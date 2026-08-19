// <copyright file="User.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class User : AuditableAggregateRoot
{
    private readonly List<UserRole> _roles = [];

    protected User() { }

    public User(
        string username,
        string email,
        string displayName,
        string? phoneNumber = null) : base(Guid.NewGuid())
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        PhoneNumber = phoneNumber;
        IsActive = true;
        LastLoginAt = null;
    }

    public string Username { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// PBKDF2 password hash (format: pbkdf2:iterations:salt:hash). Null for
    /// users that only authenticate via external IdP (Azure AD / Google).
    /// </summary>
    public string? PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();

    public void Update(string email, string displayName, string? phoneNumber)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        PhoneNumber = phoneNumber;
    }

    public void AddRole(Guid roleId, Guid? companyId = null)
    {
        if (_roles.Any(r => r.RoleId == roleId && r.CompanyId == companyId))
            return;

        _roles.Add(new UserRole(Id, roleId, companyId));
    }

    public void RemoveRole(Guid roleId, Guid? companyId = null)
    {
        var role = _roles.FirstOrDefault(r => r.RoleId == roleId && r.CompanyId == companyId);
        if (role != null)
        {
            _roles.Remove(role);
        }
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
