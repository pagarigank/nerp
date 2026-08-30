// <copyright file="PermissionPolicyProvider.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ERP.Modules.Platform.Api.Authorization;

/// <summary>
/// Resolves the dynamic <c>perm:&lt;code&gt;</c> policies created by
/// <see cref="RequirePermissionAttribute"/> to a policy containing a single
/// <see cref="PermissionRequirement"/>. All other policy names are delegated to
/// the default provider so existing policies (e.g. <c>CompanyAdminOrSuper</c>)
/// keep working.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var code = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            var requirement = new PermissionRequirement(code);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(requirement)
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
