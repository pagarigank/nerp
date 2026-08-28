// <copyright file="ReportParameterSet.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class ReportParameterSet : AuditableAggregateRoot
{
    protected ReportParameterSet() { }

    public ReportParameterSet(
        Guid companyId,
        Guid reportDefinitionId,
        string name,
        string parametersJson,
        bool isDefault,
        string? description) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ReportDefinitionId = reportDefinitionId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParametersJson = parametersJson ?? throw new ArgumentNullException(nameof(parametersJson));
        IsDefault = isDefault;
        Description = description;
        RunCount = 0;
    }

    public Guid CompanyId { get; private set; }
    public Guid ReportDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ParametersJson { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public string? Description { get; private set; }
    public int RunCount { get; private set; }

    public void Update(string name, string parametersJson, bool isDefault, string? description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParametersJson = parametersJson ?? throw new ArgumentNullException(nameof(parametersJson));
        IsDefault = isDefault;
        Description = description;
    }

    public void IncrementRunCount() => RunCount++;
    public void SetDefault(bool isDefault) => IsDefault = isDefault;
}
