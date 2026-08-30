// <copyright file="Permission.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class Permission : Entity
{
    protected Permission() { }

    public Permission(string module, string page, string action, string code, string description) : base(Guid.NewGuid())
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>Module prefix, e.g. "gl", "om", "ar".</summary>
    public string Module { get; private set; } = string.Empty;

    /// <summary>Page/resource key derived from navigation, e.g. "journal-batches", "quotes".</summary>
    public string Page { get; private set; } = string.Empty;

    /// <summary>Verb: view | create | edit | delete | post | approve (etc.).</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Stable unique key "{module}.{page}.{action}", e.g. "gl.journal-batches.view".</summary>
    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public void Update(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
