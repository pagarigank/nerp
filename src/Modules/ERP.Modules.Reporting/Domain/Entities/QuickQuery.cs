// <copyright file="QuickQuery.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class QuickQuery : AuditableAggregateRoot
{
    protected QuickQuery() { }

    public QuickQuery(
        Guid companyId,
        string name,
        string entityName,
        string? filterJson,
        string? sortJson,
        string? columnSelectionJson,
        bool includeArchived,
        string? createdByUser) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        FilterJson = filterJson;
        SortJson = sortJson;
        ColumnSelectionJson = columnSelectionJson;
        IncludeArchived = includeArchived;
        CreatedByUser = createdByUser ?? string.Empty;
        RunCount = 0;
        IsShared = false;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty; // JournalBatch, SalesOrder, PurchaseOrder, etc.
    public string? FilterJson { get; private set; }
    public string? SortJson { get; private set; }
    public string? ColumnSelectionJson { get; private set; }
    public bool IncludeArchived { get; private set; }
    public string CreatedByUser { get; private set; } = string.Empty;
    public int RunCount { get; private set; }
    public DateTimeOffset? LastRunOn { get; private set; }
    public bool IsShared { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string entityName, string? filterJson,
        string? sortJson, string? columnSelectionJson, bool includeArchived)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        FilterJson = filterJson;
        SortJson = sortJson;
        ColumnSelectionJson = columnSelectionJson;
        IncludeArchived = includeArchived;
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
