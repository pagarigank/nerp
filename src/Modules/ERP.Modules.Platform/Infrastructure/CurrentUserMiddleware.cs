// <copyright file="CurrentUserMiddleware.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ERP.Modules.Platform.Infrastructure;

public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
    {
        if (currentUserService is CurrentUserService concrete)
        {
            concrete.UserId = context.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User?.FindFirst("sub")?.Value
                ?? "system";
            concrete.CorrelationId = context.Items["CorrelationId"]?.ToString();

            // Company scoping: a "super_admin" claim (or a "*" company_scope) means
            // the user may operate across every company. Otherwise the allowed
            // companies are taken from the "company_scope" claims.
            concrete.IsSuperAdmin = context.User?.HasClaim("super_admin", "true") == true
                || context.User?.HasClaim(c => c.Type == "company_scope" && c.Value == "*") == true;

            var scoped = context.User?
                .FindAll("company_scope")
                .Where(c => c.Value != "*")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var ids = new List<Guid>();
            foreach (var value in scoped)
            {
                if (Guid.TryParse(value, out var id))
                {
                    ids.Add(id);
                }
            }

            concrete.CompanyIds = ids;
        }

        await _next(context);
    }
}

public class CurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; }

    public string? CorrelationId { get; set; }

    /// <summary>
    /// True when the principal may access every company (super admin). When
    /// false, <see cref="CompanyIds"/> holds the only companies the principal
    /// may operate within.
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>
    /// Companies the principal is scoped to. Empty (and <see cref="IsSuperAdmin"/>
    /// false) means no company access.
    /// </summary>
    public IReadOnlyList<Guid> CompanyIds { get; set; } = Array.Empty<Guid>();
}
