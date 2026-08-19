// <copyright file="NumberSequence.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class NumberSequence : AuditableAggregateRoot
{
    protected NumberSequence() { }

    public NumberSequence(
        Guid companyId,
        string name,
        string prefix,
        int nextValue,
        int increment,
        int minValue,
        int maxValue) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Prefix = prefix ?? string.Empty;
        NextValue = nextValue;
        Increment = increment;
        MinValue = minValue;
        MaxValue = maxValue;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Prefix { get; private set; } = string.Empty;

    public int NextValue { get; private set; }

    public int Increment { get; private set; }

    public int MinValue { get; private set; }

    public int MaxValue { get; private set; }

    public bool IsActive { get; private set; }

    public string GetNextNumber()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Number sequence '{Name}' is inactive.");

        if (NextValue > MaxValue)
            throw new InvalidOperationException($"Number sequence '{Name}' has exceeded its maximum value of {MaxValue}.");

        var current = NextValue;
        NextValue += Increment;
        return $"{Prefix}{current:D8}";
    }

    public void Update(string name, string prefix, int increment, int minValue, int maxValue)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Prefix = prefix ?? string.Empty;
        Increment = increment;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reset(int startingValue)
    {
        NextValue = startingValue;
    }
}
