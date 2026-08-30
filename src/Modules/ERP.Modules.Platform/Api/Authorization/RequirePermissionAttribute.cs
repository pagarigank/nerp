// <copyright file="RequirePermissionAttribute.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authorization;

namespace ERP.Modules.Platform.Api.Authorization;

/// <summary>
/// Declarative attribute that protects an endpoint with a page-scoped RBAC
/// permission. Usage:
/// <code>
///   [RequirePermission("gl.journal-batches.view")]   // single code
///   [RequirePermission("gl", "journal-batches", "view")] // module/page/action
/// </code>
/// Mirrors the frontend <c>hasPermission("{module}.{page}.{action}")</c> check.
/// The requirement honors legacy and wildcard grants (see <see cref="PermissionRequirement"/>).
/// The <c>perm:&lt;code&gt;</c> policy name is resolved by <see cref="PermissionPolicyProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public RequirePermissionAttribute(string code)
    {
        Code = PermissionRequirement.ToCode(code);
        Policy = PolicyPrefix + Code;
        var (module, page, action) = PermissionRequirement.Split(code);
        Module = module;
        Page = page;
        Action = action;
    }

    public RequirePermissionAttribute(string module, string page, string action)
        : this($"{module}.{page}.{action}")
    {
        Module = module;
        Page = page;
        Action = action;
    }

    /// <summary>The canonical permission code this attribute enforces (e.g. gl.journal-batches.view).</summary>
    public string Code { get; }

    /// <summary>Module segment of the enforced permission (e.g. <c>gl</c>).</summary>
    public string Module { get; private set; }

    /// <summary>Page segment of the enforced permission (e.g. <c>journal-batches</c>).</summary>
    public string Page { get; private set; }

    /// <summary>Action segment of the enforced permission (e.g. <c>view</c>).</summary>
    public string Action { get; private set; }
}
