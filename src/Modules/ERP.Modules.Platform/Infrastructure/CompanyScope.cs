// <copyright file="CompanyScope.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Platform.Infrastructure;

/// <summary>
/// Applies the principal's company scope to a queryable. The scope is derived
/// from <see cref="ICurrentUserService"/> (populated from the JWT's
/// <c>company_scope</c> claims by <see cref="CurrentUserMiddleware"/>):
/// <list type="bullet">
///   <item><description><b>Super admin</b> (<see cref="ICurrentUserService.IsSuperAdmin"/>):
///   no restriction; when an explicit <paramref name="explicitCompanyId"/> is supplied
///   it is still honoured (the <see cref="CompanyAuthorizationFilter"/> has already
///   verified the principal may access it).</description></item>
///   <item><description><b>Company-scoped user</b>: rows are limited to
///   <see cref="ICurrentUserService.CompanyIds"/>. An explicit id, if present, is also
///   limited to that id (already authorized by the filter).</description></item>
///   <item><description><b>No company assignments</b>: returns nothing.</description></item>
/// </list>
/// Call this from every list/read endpoint so data segregation is enforced from the
/// token rather than being opt-in on the caller. The <see cref="HttpContext"/> overload
/// resolves the current user from the request scope, so no controller DI change is needed.
/// </summary>
public static class CompanyScope
{
    /// <summary>Applies company scoping using the current user resolved from <paramref name="httpContext"/>.</summary>
    public static IQueryable<T> ApplyCompanyScope<T>(
        this IQueryable<T> query,
        HttpContext httpContext,
        Expression<Func<T, Guid>> companyId,
        Guid? explicitCompanyId = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var user = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        return query.ApplyCompanyScope(user, companyId, explicitCompanyId);
    }

    /// <summary>Applies company scoping using an explicitly-provided <see cref="ICurrentUserService"/>.</summary>
    public static IQueryable<T> ApplyCompanyScope<T>(
        this IQueryable<T> query,
        ICurrentUserService user,
        Expression<Func<T, Guid>> companyId,
        Guid? explicitCompanyId = null)
    {
        if (user is null)
            ArgumentNullException.ThrowIfNull(user);
        if (companyId is null)
            ArgumentNullException.ThrowIfNull(companyId);

        // Super admin: unrestricted, but still honour an explicit (already-authorized) id.
        if (user.IsSuperAdmin)
        {
            return explicitCompanyId.HasValue
                ? query.Where(ComposeEquals(companyId, explicitCompanyId.Value))
                : query;
        }

        // Explicit id requested: filter to it (CompanyAuthorizationFilter already ensured access).
        if (explicitCompanyId.HasValue)
            return query.Where(ComposeEquals(companyId, explicitCompanyId.Value));

        var ids = user.CompanyIds;
        if (ids is null || ids.Count == 0)
            return query.Where(AlwaysFalse<T>());

        return query.Where(ComposeContains(companyId, ids));
    }

    private static Expression<Func<T, bool>> ComposeEquals<T>(Expression<Func<T, Guid>> selector, Guid value)
    {
        var parameter = selector.Parameters[0];
        var equal = Expression.Equal(selector.Body, Expression.Constant(value, typeof(Guid)));
        return Expression.Lambda<Func<T, bool>>(equal, parameter);
    }

    private static Expression<Func<T, bool>> ComposeContains<T>(Expression<Func<T, Guid>> selector, IReadOnlyList<Guid> ids)
    {
        var parameter = selector.Parameters[0];
        var body = selector.Body;

        // Enumerable.Contains(IReadOnlyList<Guid>, Guid) -> ids.Contains(x.CompanyId)
        var contains = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        var idsConstant = Expression.Constant(ids, typeof(IReadOnlyList<Guid>));
        var call = Expression.Call(contains, idsConstant, body);
        return Expression.Lambda<Func<T, bool>>(call, parameter);
    }

    private static Expression<Func<T, bool>> AlwaysFalse<T>() => _ => false;
}
