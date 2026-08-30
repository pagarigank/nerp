// <copyright file="PermissionAuthorizationHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ERP.Modules.Platform.Api.Authorization;

/// <summary>
/// Evaluates <see cref="PermissionRequirement"/> against the permission claims
/// carried in the JWT (<c>permission</c> claim type). The matching semantics
/// mirror the frontend <c>hasPermission</c> so server and client agree:
///   - exact <c>module.page.action</c> match,
///   - legacy <c>module.action</c> (page = "*") grants any page of that module/action,
///   - <c>module.*.action</c> grants that action on any page of the module,
///   - <c>module.page.*</c> grants every action on that page,
///   - <c>*.*</c> / <c>*.*.*</c> grants everything.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string ClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (HoldsPermission(context.User, requirement.Code))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static bool HoldsPermission(ClaimsPrincipal user, string requiredCode)
    {
        var req = PermissionRequirement.Split(requiredCode);

        return user.FindAll(ClaimType)
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .Select(c => PermissionRequirement.Split(c.Value))
            .Any(held => held.Module == "*" || held.Action == "*"
                || (held.Module == req.Module
                    && (held.Action == "*" || held.Action == req.Action)
                    && (held.Page == "*" || held.Page == req.Page)));
    }
}
