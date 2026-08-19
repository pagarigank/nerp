// <copyright file="RolePermission.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class RolePermission : Entity
{
    protected RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId) : base(Guid.NewGuid())
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }
}
