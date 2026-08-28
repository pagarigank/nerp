// <copyright file="SavedQuery.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class SavedQuery : AuditableAggregateRoot
{
    protected SavedQuery() { }

    public SavedQuery(
        Guid companyId,
        string name,
        string module,
        string queryType,
        string? entityName,
        string? filterJson,
        string? sortJson,
        string? columnSelectionJson,
        string? createdByUser) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        QueryType = queryType ?? throw new ArgumentNullException(nameof(queryType));
        EntityName = entityName;
        FilterJson = filterJson;
        SortJson = sortJson;
        ColumnSelectionJson = columnSelectionJson;
        CreatedByUser = createdByUser ?? string.Empty;
        RunCount = 0;
        IsShared = false;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string QueryType { get; private set; } = string.Empty; // QuickQuery, Report, Custom
    public string? EntityName { get; private set; }
    public string? FilterJson { get; private set; }
    public string? SortJson { get; private set; }
    public string? ColumnSelectionJson { get; private set; }
    public string CreatedByUser { get; private set; } = string.Empty;
    public int RunCount { get; private set; }
    public DateTimeOffset? LastRunOn { get; private set; }
    public bool IsShared { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string module, string queryType, string? entityName,
        string? filterJson, string? sortJson, string? columnSelectionJson)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        QueryType = queryType ?? throw new ArgumentNullException(nameof(queryType));
        EntityName = entityName;
        FilterJson = filterJson;
        SortJson = sortJson;
        ColumnSelectionJson = columnSelectionJson;
    }

    public void RecordRun()
    {
        RunCount++;
        LastRunOn = DateTimeOffset.UtcNow;
    }

    public void SetShared(bool isShared) => IsShared = isShared;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
