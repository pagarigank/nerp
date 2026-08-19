// <copyright file="AllocationRuleLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class AllocationRuleLine : Entity
{
    protected AllocationRuleLine() { }

    internal AllocationRuleLine(
        Guid allocationRuleId,
        Guid targetAccountId,
        decimal percentage,
        decimal? fixedAmount,
        string? reference) : base(Guid.NewGuid())
    {
        AllocationRuleId = allocationRuleId;
        TargetAccountId = targetAccountId;
        Percentage = percentage;
        FixedAmount = fixedAmount;
        Reference = reference;
    }

    public Guid AllocationRuleId { get; private set; }

    public Guid TargetAccountId { get; private set; }

    public decimal Percentage { get; private set; }

    public decimal? FixedAmount { get; private set; }

    public string? Reference { get; private set; }
}
