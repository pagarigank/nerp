// <copyright file="Budget.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class Budget : AuditableAggregateRoot
{
    private readonly List<BudgetLine> _lines = [];

    protected Budget() { }

    public Budget(
        Guid companyId,
        Guid fiscalYearId,
        string name,
        string description,
        BudgetType budgetType) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Budget name is required.", nameof(name));

        CompanyId = companyId;
        FiscalYearId = fiscalYearId;
        Name = name;
        Description = description ?? string.Empty;
        BudgetType = budgetType;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public Guid FiscalYearId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public BudgetType BudgetType { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<BudgetLine> Lines => _lines.AsReadOnly();

    public void AddLine(Guid accountId, int periodNumber, decimal amount, Guid? projectId = null)
    {
        if (amount < 0)
            throw new ArgumentException("Budget amount cannot be negative.", nameof(amount));

        if (periodNumber < 1 || periodNumber > 13)
            throw new ArgumentException("Period number must be between 1 and 13.", nameof(periodNumber));

        var line = new BudgetLine(Id, accountId, periodNumber, amount, projectId);
        _lines.Add(line);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

public enum BudgetType
{
    Original = 0,
    Revised = 1,
    Encumbrance = 2,
}
