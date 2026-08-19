// <copyright file="Permission.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class Permission : Entity
{
    protected Permission() { }

    public Permission(string module, string action, string description) : base(Guid.NewGuid())
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public string Module { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public void Update(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
