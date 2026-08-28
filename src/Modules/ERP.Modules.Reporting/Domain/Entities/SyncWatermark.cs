// <copyright file="SyncWatermark.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

/// <summary>
/// Tracks the high-water mark for incremental CDC/ETL extraction from each
/// source table. The LastSyncOn timestamp is used as the WHERE clause filter
/// on the source table's ModifiedOn column to extract only new/changed rows.
/// </summary>
public class SyncWatermark : Entity
{
    protected SyncWatermark() { }

    public SyncWatermark(string sourceTable, string stagingTable)
    {
        Id = Guid.NewGuid();
        SourceTable = sourceTable ?? throw new ArgumentNullException(nameof(sourceTable));
        StagingTable = stagingTable ?? throw new ArgumentNullException(nameof(stagingTable));
        TotalRowsSynced = 0;
    }

    public string SourceTable { get; private set; } = string.Empty;
    public string StagingTable { get; private set; } = string.Empty;
    public DateTimeOffset? LastSyncOn { get; private set; }
    public long TotalRowsSynced { get; private set; }

    public void RecordSync(long rowCount)
    {
        LastSyncOn = DateTimeOffset.UtcNow;
        TotalRowsSynced += rowCount;
    }

    public void Reset()
    {
        LastSyncOn = null;
        TotalRowsSynced = 0;
    }
}

/// <summary>
/// Logs each CDC/ETL sync run with timing, row counts, and error details.
/// Used for monitoring sync health and diagnosing failures.
/// </summary>
public class SyncRunLog : Entity
{
    protected SyncRunLog() { }

    public SyncRunLog(Guid runId, string sourceTable, string stagingTable)
    {
        Id = runId;
        SourceTable = sourceTable ?? throw new ArgumentNullException(nameof(sourceTable));
        StagingTable = stagingTable ?? throw new ArgumentNullException(nameof(stagingTable));
        Status = "Running";
        StartedOn = DateTimeOffset.UtcNow;
    }

    public string SourceTable { get; private set; } = string.Empty;
    public string StagingTable { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset StartedOn { get; private set; }
    public DateTimeOffset? CompletedOn { get; private set; }
    public long? RowsSynced { get; private set; }
    public long? DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void Complete(long rowCount, long durationMs)
    {
        Status = "Success";
        CompletedOn = DateTimeOffset.UtcNow;
        RowsSynced = rowCount;
        DurationMs = durationMs;
    }

    public void Fail(string error, long durationMs)
    {
        Status = "Failed";
        CompletedOn = DateTimeOffset.UtcNow;
        ErrorMessage = error;
        DurationMs = durationMs;
    }
}
