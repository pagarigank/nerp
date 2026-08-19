// <copyright file="SoDRule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class SoDRule : AuditableAggregateRoot
{
    protected SoDRule() { }

    public SoDRule(
        string module,
        string actionA,
        string actionB,
        string description,
        string? documentType = null,
        decimal? thresholdAmount = null) : base(Guid.NewGuid())
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        ActionA = actionA ?? throw new ArgumentNullException(nameof(actionA));
        ActionB = actionB ?? throw new ArgumentNullException(nameof(actionB));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        DocumentType = documentType;
        ThresholdAmount = thresholdAmount;
        IsActive = true;
    }

    public string Module { get; private set; } = string.Empty;

    public string ActionA { get; private set; } = string.Empty;

    public string ActionB { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string? DocumentType { get; private set; }

    public bool IsActive { get; private set; }

    public decimal? ThresholdAmount { get; private set; }

    public void Update(string module, string actionA, string actionB, string description, string? documentType, decimal? thresholdAmount)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        ActionA = actionA ?? throw new ArgumentNullException(nameof(actionA));
        ActionB = actionB ?? throw new ArgumentNullException(nameof(actionB));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        DocumentType = documentType;
        ThresholdAmount = thresholdAmount;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
