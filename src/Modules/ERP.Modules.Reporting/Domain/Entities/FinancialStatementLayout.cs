// <copyright file="FinancialStatementLayout.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class FinancialStatementLayout : AuditableAggregateRoot
{
    protected FinancialStatementLayout() { }

    public FinancialStatementLayout(
        Guid companyId,
        string name,
        string statementType,
        string? description,
        string rowDefinitionsJson,
        string columnDefinitionsJson,
        string? treeJson,
        bool suppressZero,
        bool roundToNearestDollar,
        int version) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        StatementType = statementType ?? throw new ArgumentNullException(nameof(statementType));
        Description = description ?? string.Empty;
        RowDefinitionsJson = rowDefinitionsJson ?? throw new ArgumentNullException(nameof(rowDefinitionsJson));
        ColumnDefinitionsJson = columnDefinitionsJson ?? throw new ArgumentNullException(nameof(columnDefinitionsJson));
        TreeJson = treeJson;
        SuppressZero = suppressZero;
        RoundToNearestDollar = roundToNearestDollar;
        Version = version;
        IsApproved = false;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string StatementType { get; private set; } = string.Empty; // BalanceSheet, IncomeStatement, CashFlow, TrialBalance, Custom
    public string Description { get; private set; } = string.Empty;
    public string RowDefinitionsJson { get; private set; } = string.Empty;
    public string ColumnDefinitionsJson { get; private set; } = string.Empty;
    public string? TreeJson { get; private set; }
    public bool SuppressZero { get; private set; }
    public bool RoundToNearestDollar { get; private set; }
    public int Version { get; private set; }
    public bool IsApproved { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string statementType, string description,
        string rowDefinitionsJson, string columnDefinitionsJson, string? treeJson,
        bool suppressZero, bool roundToNearestDollar)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        StatementType = statementType ?? throw new ArgumentNullException(nameof(statementType));
        Description = description ?? string.Empty;
        RowDefinitionsJson = rowDefinitionsJson ?? throw new ArgumentNullException(nameof(rowDefinitionsJson));
        ColumnDefinitionsJson = columnDefinitionsJson ?? throw new ArgumentNullException(nameof(columnDefinitionsJson));
        TreeJson = treeJson;
        SuppressZero = suppressZero;
        RoundToNearestDollar = roundToNearestDollar;
        Version++;
    }

    public void Approve() => IsApproved = true;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
