// <copyright file="ICurrentUserService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Infrastructure;

public interface ICurrentUserService
{
    string? UserId { get; }

    string? CorrelationId { get; }

    /// <summary>True when the principal may access every company (super admin).</summary>
    bool IsSuperAdmin { get; }

    /// <summary>Companies the principal is scoped to (empty when not a super admin with no assignments).</summary>
    IReadOnlyList<Guid> CompanyIds { get; }
}
