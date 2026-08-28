// <copyright file="ReportDefinition.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class ReportDefinition : AuditableAggregateRoot
{
    protected ReportDefinition() { }

    public ReportDefinition(
        Guid companyId,
        string name,
        string module,
        string category,
        string description,
        string reportType,
        string? dataSource = null,
        string? sqlQuery = null,
        string? parametersJson = null,
        string? layoutJson = null) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Description = description ?? string.Empty;
        ReportType = reportType ?? throw new ArgumentNullException(nameof(reportType));
        DataSource = dataSource;
        SqlQuery = sqlQuery;
        ParametersJson = parametersJson;
        LayoutJson = layoutJson;
        IsShared = false;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ReportType { get; private set; } = string.Empty; // Standard, FinancialStatement, Custom, Dashboard
    public string? DataSource { get; private set; }
    public string? SqlQuery { get; private set; }
    public string? ParametersJson { get; private set; }
    public string? LayoutJson { get; private set; }
    public bool IsShared { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string module, string category, string description, string reportType,
        string? dataSource, string? sqlQuery, string? parametersJson, string? layoutJson)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Description = description ?? string.Empty;
        ReportType = reportType ?? throw new ArgumentNullException(nameof(reportType));
        DataSource = dataSource;
        SqlQuery = sqlQuery;
        ParametersJson = parametersJson;
        LayoutJson = layoutJson;
    }

    public void SetShared(bool isShared) => IsShared = isShared;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
