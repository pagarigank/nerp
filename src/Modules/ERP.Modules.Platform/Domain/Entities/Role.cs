// <copyright file="Role.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class Role : AuditableAggregateRoot
{
    private readonly List<RolePermission> _permissions = [];

    protected Role() { }

    public Role(string name, string description) : base(Guid.NewGuid())
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public IReadOnlyList<RolePermission> Permissions => _permissions.AsReadOnly();

    public void Update(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public void AddPermission(Guid permissionId)
    {
        if (_permissions.Any(p => p.PermissionId == permissionId))
            return;

        _permissions.Add(new RolePermission(Id, permissionId));
    }

    public void RemovePermission(Guid permissionId)
    {
        var permission = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (permission != null)
        {
            _permissions.Remove(permission);
        }
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
