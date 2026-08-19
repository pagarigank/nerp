// <copyright file="BudgetLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class BudgetLine : Entity
{
    protected BudgetLine() { }

    internal BudgetLine(
        Guid budgetId,
        Guid accountId,
        int periodNumber,
        decimal amount,
        Guid? projectId) : base(Guid.NewGuid())
    {
        BudgetId = budgetId;
        AccountId = accountId;
        PeriodNumber = periodNumber;
        Amount = amount;
        ProjectId = projectId;
    }

    public Guid BudgetId { get; private set; }

    public Guid AccountId { get; private set; }

    public int PeriodNumber { get; private set; }

    public decimal Amount { get; private set; }

    public Guid? ProjectId { get; private set; }

    public void AdjustAmount(decimal newAmount)
    {
        if (newAmount < 0)
            throw new ArgumentException("Budget amount cannot be negative.", nameof(newAmount));

        Amount = newAmount;
    }
}
