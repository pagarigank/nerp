// <copyright file="UserRole.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// Links a user to a role. When CompanyId is null the assignment applies across
/// every company (super-admin scope). When set, the assignment is valid only for
/// that company (company-admin scope). The combination (UserId, RoleId, CompanyId)
/// is unique so a user cannot hold the same role twice for the same company.
/// </summary>
public class UserRole : Entity
{
    public Guid UserId { get; private set; }

    public Guid? CompanyId { get; private set; }

    public bool IsGlobal => !CompanyId.HasValue;

    protected UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId, Guid? companyId = null) : base(Guid.NewGuid())
    {
        UserId = userId;
        RoleId = roleId;
        CompanyId = companyId;
    }

    public Guid RoleId { get; private set; }
}
