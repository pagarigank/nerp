// <copyright file="PermissionRequirement.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authorization;

namespace ERP.Modules.Platform.Api.Authorization;

/// <summary>
/// Authorization requirement that a principal holds a specific page-scoped
/// permission, e.g. <c>gl.journal-batches.view</c>. The accompanying handler
/// also honors legacy (<c>module.action</c> =&gt; <c>module.*.action</c>) and
/// wildcard (<c>*.*</c>, <c>module.*.action</c>, <c>module.page.*</c>) grants
/// so the backend matches the frontend <c>hasPermission</c> semantics exactly.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string code)
    {
        // Normalize once: lowercase, always 3 segments (module.page.action).
        Code = ToCode(code);
    }

    public string Code { get; }

    /// <summary>Normalize any permission code to its canonical 3-segment form.</summary>
    public static string ToCode(string code)
    {
        var (module, page, action) = Split(code);
        return $"{module}.{page}.{action}";
    }

    /// <summary>Split a permission code into its 3 segments (lowercase).</summary>
    internal static (string Module, string Page, string Action) Split(string code)
    {
#pragma warning disable CA1308 // Permission codes are intentionally lower-case to match the DB/frontend vocabulary.
        var parts = code.ToLowerInvariant().Split('.');
#pragma warning restore CA1308
        if (parts.Length == 3)
            return (parts[0], parts[1], parts[2]);
        if (parts.Length == 2)
            return (parts[0], "*", parts[1]); // legacy module.action
        return (parts.Length == 1 ? parts[0] : "*", "*", "*");
    }
}
