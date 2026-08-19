// <copyright file="Currency.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class Currency : AuditableAggregateRoot
{
    protected Currency() { }

    public Currency(
        string code,
        string name,
        string symbol,
        int decimalPlaces) : base(Guid.NewGuid())
    {
        Code = code?.ToUpperInvariant().Trim() ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        DecimalPlaces = decimalPlaces;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Symbol { get; private set; } = string.Empty;

    public int DecimalPlaces { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string name, string symbol, int decimalPlaces)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        DecimalPlaces = decimalPlaces;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
