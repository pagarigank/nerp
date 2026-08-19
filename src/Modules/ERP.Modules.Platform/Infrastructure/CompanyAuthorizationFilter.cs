// <copyright file="CompanyAuthorizationFilter.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Platform.Infrastructure;

/// <summary>
/// Enforces company-scoped authorization. Every request that targets a specific
/// company (via the <c>companyId</c> query parameter or the <c>X-Company-Id</c>
/// header) is checked against the principal's allowed companies:
/// <list type="bullet">
///   <item><description>A <b>super admin</b> (any role assignment with no company
///   scope) may access every company.</description></item>
///   <item><description>A <b>company admin</b> may access only the companies in
///   their company-scoped role assignments; anything else returns 403.</description></item>
///   <item><description>Requests with no company context are passed through (the
///   individual controllers still apply their own data-scoping).</description></item>
/// </list>
/// The allowed companies are resolved from the database (using the <c>sub</c>
/// claim) rather than from token claims, because the .NET 8 JWT handler strips
/// custom claim types from <c>HttpContext.User</c>.
/// </summary>
public class CompanyAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var principal = context.HttpContext.User;
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
        {
            await next();
            return;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            // No usable identity -> let the standard [Authorize] challenge handle it.
            await next();
            return;
        }

        // No specific company requested -> controllers decide scope themselves.
        var requested = ResolveRequestedCompany(context);
        if (requested is null)
        {
            await next();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<PlatformDbContext>();
        var assignments = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.CompanyId)
            .ToListAsync(context.HttpContext.RequestAborted);

        var isSuperAdmin = assignments.Contains(null);
        if (isSuperAdmin)
        {
            await next();
            return;
        }

        var allowed = assignments
            .Where(c => c != null)
            .Select(c => c!.Value)
            .ToHashSet();

        if (allowed.Count == 0 || !allowed.Contains(requested.Value))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }

    private static Guid? ResolveRequestedCompany(ActionExecutingContext context)
    {
        // 1) X-Company-Id header.
        if (context.HttpContext.Request.Headers.TryGetValue("X-Company-Id", out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var fromHeader))
        {
            return fromHeader;
        }

        // 2) companyId query parameter (most modules pass it this way).
        if (context.HttpContext.Request.Query.TryGetValue("companyId", out var queryValue)
            && Guid.TryParse(queryValue.ToString(), out var fromQuery))
        {
            return fromQuery;
        }

        // 3) companyId action argument (route/body bound).
        if (context.ActionArguments.TryGetValue("companyId", out var arg)
            && arg is Guid guidArg)
        {
            return guidArg;
        }

        return null;
    }
}
