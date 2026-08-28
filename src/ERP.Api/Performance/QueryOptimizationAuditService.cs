// <copyright file="QueryOptimizationAuditService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413, SA1208, SA1512, SA1117, SA1134
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, CA1849, CA1000, S1118, S6966, SA1501, SA1107, SA1513, CA2100, CA1307, S2486, S108, S3358

using System.Data;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Serilog;

namespace ERP.Api.Performance;

public interface IQueryOptimizationAuditService
{
    Task<QueryAuditReport> RunAuditAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SlowQueryEntry>> GetSlowQueriesAsync(int top = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissingIndexEntry>> GetMissingIndexesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TableScanEntry>> GetTableScansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NPlusOneCandidate>> DetectNPlusOneCandidatesAsync(CancellationToken cancellationToken = default);
}

public sealed class QueryOptimizationAuditService : IQueryOptimizationAuditService
{
    private readonly string connectionString;

    public QueryOptimizationAuditService(IConfiguration configuration)
    {
        this.connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<QueryAuditReport> RunAuditAsync(CancellationToken cancellationToken = default)
    {
        var slowQueries = await GetSlowQueriesAsync(20, cancellationToken);
        var missingIndexes = await GetMissingIndexesAsync(cancellationToken);
        var tableScans = await GetTableScansAsync(cancellationToken);
        var nPlusOne = await DetectNPlusOneCandidatesAsync(cancellationToken);
        var totalQueriesAnalyzed = slowQueries.Count + missingIndexes.Count + tableScans.Count;
        return new QueryAuditReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalQueriesAnalyzed = totalQueriesAnalyzed,
            SlowQueries = slowQueries,
            MissingIndexes = missingIndexes,
            TableScans = tableScans,
            NPlusOneCandidates = nPlusOne,
            Summary = new AuditSummary
            {
                SlowQueryCount = slowQueries.Count,
                MissingIndexCount = missingIndexes.Count,
                TableScanCount = tableScans.Count,
                NPlusOneCandidateCount = nPlusOne.Count,
                EstimatedImprovementPercent = CalculateImprovement(slowQueries, missingIndexes, tableScans),
            },
        };
    }

    public async Task<IReadOnlyList<SlowQueryEntry>> GetSlowQueriesAsync(int top = 50, CancellationToken cancellationToken = default)
    {
        var results = new List<SlowQueryEntry>();
        const string sql = @"SELECT TOP (@Top) qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time_us, qs.execution_count, qs.total_logical_reads / qs.execution_count AS avg_logical_reads, qs.total_worker_time / qs.execution_count AS avg_cpu_time_us, qs.last_elapsed_time / 1000 AS last_elapsed_ms, qs.total_elapsed_time / 1000000.0 AS total_elapsed_sec, SUBSTRING(qt.text, (qs.statement_start_offset / 2) + 1, ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(qt.text) ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2) + 1) AS query_text, OBJECT_NAME(qt.objectid) AS object_name FROM sys.dm_exec_query_stats qs CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp WHERE qs.execution_count > 10 ORDER BY avg_elapsed_time_us DESC";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@Top", top);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new SlowQueryEntry
                {
                    AvgElapsedTimeMs = reader.GetInt64(0) / 1000.0,
                    ExecutionCount = reader.GetInt64(1),
                    AvgLogicalReads = reader.GetInt64(2),
                    AvgCpuTimeMs = reader.GetInt64(3) / 1000.0,
                    LastElapsedTimeMs = reader.GetDouble(4),
                    TotalElapsedSec = reader.GetDouble(5),
                    QueryText = await reader.IsDBNullAsync(6, cancellationToken) ? string.Empty : reader.GetString(6),
                    ObjectName = await reader.IsDBNullAsync(7, cancellationToken) ? null : reader.GetString(7),
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to query slow queries from DMVs");
        }
        return results;
    }

    public async Task<IReadOnlyList<MissingIndexEntry>> GetMissingIndexesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<MissingIndexEntry>();
        const string sql = @"SELECT TOP 30 CAST(gs.avg_total_user_cost * gs.avg_user_impact * (gs.user_seeks + gs.user_scans) AS DECIMAL(18,2)) AS improvement_measure, OBJECT_NAME(id.object_id) AS table_name, id.equality_columns, id.inequality_columns, id.included_columns, gs.user_seeks, gs.user_scans, gs.user_updates, gs.last_user_seek FROM sys.dm_db_missing_index_group_stats gs JOIN sys.dm_db_missing_index_groups ig ON gs.group_handle = ig.index_group_handle JOIN sys.dm_db_missing_index_details id ON ig.index_handle = id.index_handle WHERE id.database_id = DB_ID() ORDER BY improvement_measure DESC";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new MissingIndexEntry
                {
                    ImprovementMeasure = reader.GetDecimal(0),
                    TableName = reader.GetString(1),
                    EqualityColumns = await reader.IsDBNullAsync(2, cancellationToken) ? null : reader.GetString(2),
                    InequalityColumns = await reader.IsDBNullAsync(3, cancellationToken) ? null : reader.GetString(3),
                    IncludedColumns = await reader.IsDBNullAsync(4, cancellationToken) ? null : reader.GetString(4),
                    UserSeeks = reader.GetInt64(5),
                    UserScans = reader.GetInt64(6),
                    UserUpdates = reader.GetInt64(7),
                    LastUserSeek = await reader.IsDBNullAsync(8, cancellationToken) ? null : reader.GetDateTime(8),
                });
            }
        }
        catch (Exception ex) { Log.Warning(ex, "Failed to query missing indexes from DMVs"); }
        return results;
    }

    public async Task<IReadOnlyList<TableScanEntry>> GetTableScansAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TableScanEntry>();
        const string sql = @"SELECT TOP 20 OBJECT_NAME(s.object_id) AS table_name, s.user_scans, s.user_seeks, CASE WHEN s.user_seeks > 0 THEN CAST(s.user_scans AS DECIMAL(18,2)) / s.user_seeks ELSE s.user_scans END AS scan_to_seek_ratio, ius.last_user_scan, p.rows AS row_count FROM sys.dm_db_index_usage_stats s JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id LEFT JOIN sys.dm_db_index_usage_stats ius ON s.object_id = ius.object_id AND ius.index_id = 0 LEFT JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id AND p.partition_number = 1 WHERE s.database_id = DB_ID() AND i.type_desc = 'HEAP' AND s.user_scans > 100 ORDER BY s.user_scans DESC";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new TableScanEntry
                {
                    TableName = reader.GetString(0),
                    UserScans = reader.GetInt64(1),
                    UserSeeks = reader.GetInt64(2),
                    ScanToSeekRatio = reader.GetDecimal(3),
                    LastUserScan = await reader.IsDBNullAsync(4, cancellationToken) ? null : reader.GetDateTime(4),
                    RowCount = await reader.IsDBNullAsync(5, cancellationToken) ? 0 : reader.GetInt64(5),
                });
            }
        }
        catch (Exception ex) { Log.Warning(ex, "Failed to query table scans from DMVs"); }
        return results;
    }

    public async Task<IReadOnlyList<NPlusOneCandidate>> DetectNPlusOneCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var candidates = new List<NPlusOneCandidate>();
        const string sql = @"SELECT TOP 20 SUBSTRING(qt.text, (qs.statement_start_offset / 2) + 1, ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(qt.text) ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2) + 1) AS query_text, qs.execution_count, qs.total_logical_reads / qs.execution_count AS avg_reads, qs.total_elapsed_time / qs.execution_count / 1000.0 AS avg_elapsed_ms, OBJECT_NAME(qt.objectid) AS object_name FROM sys.dm_exec_query_stats qs CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt WHERE qs.execution_count > 1000 AND qs.total_logical_reads / qs.execution_count > 100 AND qt.text LIKE '%WHERE%=%' AND qt.text NOT LIKE '%IN (%' AND qt.text NOT LIKE '%BULK%' ORDER BY qs.execution_count DESC, qs.total_logical_reads / qs.execution_count DESC";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var executionCount = reader.GetInt64(1);
                var avgReads = reader.GetInt64(2);
                candidates.Add(new NPlusOneCandidate
                {
                    QueryText = reader.GetString(0),
                    ExecutionCount = executionCount,
                    AvgLogicalReads = avgReads,
                    AvgElapsedTimeMs = reader.GetDouble(3),
                    ObjectName = await reader.IsDBNullAsync(4, cancellationToken) ? null : reader.GetString(4),
                    Confidence = executionCount > 10000 && avgReads > 500 ? "High" : executionCount > 1000 ? "Medium" : "Low",
                });
            }
        }
        catch (Exception ex) { Log.Warning(ex, "Failed to detect N+1 query candidates"); }
        return candidates;
    }

    private static double CalculateImprovement(IReadOnlyList<SlowQueryEntry> slowQueries, IReadOnlyList<MissingIndexEntry> missingIndexes, IReadOnlyList<TableScanEntry> tableScans)
    {
        double score = 0;
        if (slowQueries.Count > 0) score += Math.Min(slowQueries.Count * 5, 30);
        if (missingIndexes.Count > 0) score += Math.Min(missingIndexes.Count * 3, 40);
        if (tableScans.Count > 0) score += Math.Min(tableScans.Count * 3, 30);
        return Math.Min(score, 100);
    }
}

public sealed class QueryAuditReport
{
    [JsonPropertyName("generatedAt")] public DateTimeOffset GeneratedAt { get; set; }
    [JsonPropertyName("totalQueriesAnalyzed")] public int TotalQueriesAnalyzed { get; set; }
    [JsonPropertyName("slowQueries")] public IReadOnlyList<SlowQueryEntry> SlowQueries { get; set; } = [];
    [JsonPropertyName("missingIndexes")] public IReadOnlyList<MissingIndexEntry> MissingIndexes { get; set; } = [];
    [JsonPropertyName("tableScans")] public IReadOnlyList<TableScanEntry> TableScans { get; set; } = [];
    [JsonPropertyName("nPlusOneCandidates")] public IReadOnlyList<NPlusOneCandidate> NPlusOneCandidates { get; set; } = [];
    [JsonPropertyName("summary")] public AuditSummary Summary { get; set; } = new();
}

public sealed class AuditSummary
{
    [JsonPropertyName("slowQueryCount")] public int SlowQueryCount { get; set; }
    [JsonPropertyName("missingIndexCount")] public int MissingIndexCount { get; set; }
    [JsonPropertyName("tableScanCount")] public int TableScanCount { get; set; }
    [JsonPropertyName("nPlusOneCandidateCount")] public int NPlusOneCandidateCount { get; set; }
    [JsonPropertyName("estimatedImprovementPercent")] public double EstimatedImprovementPercent { get; set; }
}

public sealed class SlowQueryEntry
{
    [JsonPropertyName("avgElapsedTimeMs")] public double AvgElapsedTimeMs { get; set; }
    [JsonPropertyName("executionCount")] public long ExecutionCount { get; set; }
    [JsonPropertyName("avgLogicalReads")] public long AvgLogicalReads { get; set; }
    [JsonPropertyName("avgCpuTimeMs")] public double AvgCpuTimeMs { get; set; }
    [JsonPropertyName("lastElapsedTimeMs")] public double LastElapsedTimeMs { get; set; }
    [JsonPropertyName("totalElapsedSec")] public double TotalElapsedSec { get; set; }
    [JsonPropertyName("queryText")] public string QueryText { get; set; } = string.Empty;
    [JsonPropertyName("objectName")] public string? ObjectName { get; set; }
}

public sealed class MissingIndexEntry
{
    [JsonPropertyName("improvementMeasure")] public decimal ImprovementMeasure { get; set; }
    [JsonPropertyName("tableName")] public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("equalityColumns")] public string? EqualityColumns { get; set; }
    [JsonPropertyName("inequalityColumns")] public string? InequalityColumns { get; set; }
    [JsonPropertyName("includedColumns")] public string? IncludedColumns { get; set; }
    [JsonPropertyName("userSeeks")] public long UserSeeks { get; set; }
    [JsonPropertyName("userScans")] public long UserScans { get; set; }
    [JsonPropertyName("userUpdates")] public long UserUpdates { get; set; }
    [JsonPropertyName("lastUserSeek")] public DateTime? LastUserSeek { get; set; }
}

public sealed class TableScanEntry
{
    [JsonPropertyName("tableName")] public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("userScans")] public long UserScans { get; set; }
    [JsonPropertyName("userSeeks")] public long UserSeeks { get; set; }
    [JsonPropertyName("scanToSeekRatio")] public decimal ScanToSeekRatio { get; set; }
    [JsonPropertyName("lastUserScan")] public DateTime? LastUserScan { get; set; }
    [JsonPropertyName("rowCount")] public long RowCount { get; set; }
}

public sealed class NPlusOneCandidate
{
    [JsonPropertyName("queryText")] public string QueryText { get; set; } = string.Empty;
    [JsonPropertyName("executionCount")] public long ExecutionCount { get; set; }
    [JsonPropertyName("avgLogicalReads")] public long AvgLogicalReads { get; set; }
    [JsonPropertyName("avgElapsedTimeMs")] public double AvgElapsedTimeMs { get; set; }
    [JsonPropertyName("objectName")] public string? ObjectName { get; set; }
    [JsonPropertyName("confidence")] public string Confidence { get; set; } = "Low";
}
