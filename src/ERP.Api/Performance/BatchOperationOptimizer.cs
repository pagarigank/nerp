// <copyright file="BatchOperationOptimizer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413, SA1208, SA1512, SA1204, SA1117, SA1134
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, CA1002, CA2227, S1118, SA1501, SA1107, SA1513, CA2100, CA1307, S2486, S108, S3358

using System.Data;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Serilog;

namespace ERP.Api.Performance;

public interface IBatchOperationOptimizer
{
    Task<int> BulkInsertAsync<T>(string schemaName, string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, CancellationToken cancellationToken = default);
    Task<int> BulkUpdateAsync<T>(string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, string keyColumn, CancellationToken cancellationToken = default);
    Task<int> BulkUpsertAsync<T>(string schemaName, string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, string keyColumn, CancellationToken cancellationToken = default);
    Task<BatchOperationStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

public sealed class BatchOperationOptimizer : IBatchOperationOptimizer
{
    private readonly string connectionString;
    private readonly List<BatchOperationRecord> operationLog = [];
    private long totalRowsProcessed;
    private long totalOperations;

    public BatchOperationOptimizer(IConfiguration configuration)
    {
        this.connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<int> BulkInsertAsync<T>(string schemaName, string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var totalInserted = 0;
        try
        {
            const int chunkSize = 1000;
            for (var i = 0; i < rows.Count; i += chunkSize)
            {
                var chunk = rows.Skip(i).Take(chunkSize).ToList();
                totalInserted += await InsertChunkAsync(schemaName, tableName, chunk, rowMapper, cancellationToken);
            }
        }
        catch (Exception ex) { Log.Error(ex, "Bulk insert failed for {Schema}.{Table}", schemaName, tableName); throw; }
        sw.Stop();
        RecordOperation("BulkInsert", tableName, rows.Count, sw.Elapsed);
        return totalInserted;
    }

    public async Task<int> BulkUpdateAsync<T>(string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var totalUpdated = 0;
        try
        {
            const int chunkSize = 1000;
            for (var i = 0; i < rows.Count; i += chunkSize)
            {
                var chunk = rows.Skip(i).Take(chunkSize).ToList();
                totalUpdated += await UpdateChunkAsync(tableName, chunk, rowMapper, keyColumn, cancellationToken);
            }
        }
        catch (Exception ex) { Log.Error(ex, "Bulk update failed for {Table}", tableName); throw; }
        sw.Stop();
        RecordOperation("BulkUpdate", tableName, rows.Count, sw.Elapsed);
        return totalUpdated;
    }

    public async Task<int> BulkUpsertAsync<T>(string schemaName, string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var totalAffected = 0;
        try
        {
            const int chunkSize = 1000;
            for (var i = 0; i < rows.Count; i += chunkSize)
            {
                var chunk = rows.Skip(i).Take(chunkSize).ToList();
                totalAffected += await UpsertChunkAsync(schemaName, tableName, chunk, rowMapper, keyColumn, cancellationToken);
            }
        }
        catch (Exception ex) { Log.Error(ex, "Bulk upsert failed for {Schema}.{Table}", schemaName, tableName); throw; }
        sw.Stop();
        RecordOperation("BulkUpsert", tableName, rows.Count, sw.Elapsed);
        return totalAffected;
    }

    public Task<BatchOperationStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        lock (operationLog)
        {
            var stats = new BatchOperationStats
            {
                TotalOperations = totalOperations,
                TotalRowsProcessed = totalRowsProcessed,
                RecentOperations = operationLog.TakeLast(50).ToList(),
            };
            if (operationLog.Count > 0)
            {
                stats.AvgRowsPerOperation = (double)totalRowsProcessed / totalOperations;
                stats.AvgDurationMs = operationLog.Average(o => o.Duration.TotalMilliseconds);
            }
            return Task.FromResult(stats);
        }
    }

    private async Task<int> InsertChunkAsync<T>(string schemaName, string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, CancellationToken cancellationToken)
    {
        var sampleRow = rowMapper(rows[0]).ToList();
        var columns = sampleRow.Select(r => r.Column).ToList();
        var tvpTypeName = $"BulkInsert_{tableName.Replace(".", "_")}_TVP";
        var fullTable = $"{schemaName}.{tableName}";
        var createTvpSql = $@"IF TYPE_ID('{tvpTypeName}') IS NOT NULL DROP TYPE {tvpTypeName}; CREATE TYPE {tvpTypeName} AS TABLE ({string.Join(", ", columns.Select(c => $"[{c}] NVARCHAR(MAX) NULL"))});";
        var insertSql = $@"INSERT INTO {fullTable} ({string.Join(", ", columns.Select(c => $"[{c}]"))}) SELECT {string.Join(", ", columns.Select(c => $"CAST([{c}] AS NVARCHAR(MAX))"))} FROM @rows;";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using (var cmd = new SqlCommand(createTvpSql, connection) { CommandTimeout = 30 })
                { await cmd.ExecuteNonQueryAsync(cancellationToken); }
            var dataTable = new DataTable();
            foreach (var col in columns) dataTable.Columns.Add(col, typeof(string));
            foreach (var row in rows)
            {
                var values = rowMapper(row).Select(r => r.Value?.ToString() ?? (object)DBNull.Value).ToArray();
                dataTable.Rows.Add(values);
            }
            await using (var cmd = new SqlCommand(insertSql, connection) { CommandTimeout = 120 })
            {
                var param = cmd.Parameters.AddWithValue("@rows", dataTable);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = tvpTypeName;
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            try { await using var c = new SqlConnection(this.connectionString); await c.OpenAsync(cancellationToken); await using var cmd = new SqlCommand($"IF TYPE_ID('{tvpTypeName}') IS NOT NULL DROP TYPE {tvpTypeName}", c); await cmd.ExecuteNonQueryAsync(cancellationToken); } catch { }
        }
    }

    private async Task<int> UpdateChunkAsync<T>(string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, string keyColumn, CancellationToken cancellationToken)
    {
        var sampleRow = rowMapper(rows[0]).ToList();
        var columns = sampleRow.Where(r => r.Column != keyColumn).Select(r => r.Column).ToList();
        var tvpTypeName = $"BulkUpdate_{tableName.Replace(".", "_")}_TVP";
        var allColumns = new List<string> { keyColumn };
        allColumns.AddRange(columns);
        var createTvpSql = $@"IF TYPE_ID('{tvpTypeName}') IS NOT NULL DROP TYPE {tvpTypeName}; CREATE TYPE {tvpTypeName} AS TABLE ({string.Join(", ", allColumns.Select(c => $"[{c}] NVARCHAR(MAX) NULL"))});";
        var updateClauses = string.Join(", ", columns.Select(c => $"t.[{c}] = s.[{c}]"));
        var updateSql = $@"UPDATE t SET {updateClauses} FROM {tableName} t INNER JOIN @rows s ON t.[{keyColumn}] = s.[{keyColumn}];";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using (var cmd = new SqlCommand(createTvpSql, connection) { CommandTimeout = 30 })
                { await cmd.ExecuteNonQueryAsync(cancellationToken); }
            var dataTable = new DataTable();
            foreach (var col in allColumns) dataTable.Columns.Add(col, typeof(string));
            foreach (var row in rows)
            {
                var values = rowMapper(row).Select(r => r.Value?.ToString() ?? (object)DBNull.Value).ToArray();
                dataTable.Rows.Add(values);
            }
            await using (var cmd = new SqlCommand(updateSql, connection) { CommandTimeout = 120 })
            {
                var param = cmd.Parameters.AddWithValue("@rows", dataTable);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = tvpTypeName;
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            try { await using var c = new SqlConnection(this.connectionString); await c.OpenAsync(cancellationToken); await using var cmd = new SqlCommand($"IF TYPE_ID('{tvpTypeName}') IS NOT NULL DROP TYPE {tvpTypeName}", c); await cmd.ExecuteNonQueryAsync(cancellationToken); } catch { }
        }
    }

    private async Task<int> UpsertChunkAsync<T>(string schemaName, string tableName, IReadOnlyList<T> rows, Func<T, IEnumerable<(string Column, object? Value)>> rowMapper, string keyColumn, CancellationToken cancellationToken)
    {
        var sampleRow = rowMapper(rows[0]).ToList();
        var columns = sampleRow.Select(r => r.Column).ToList();
        var tvpTypeName = $"BulkUpsert_{tableName.Replace(".", "_")}_TVP";
        var fullTable = $"{schemaName}.{tableName}";
        var createTvpSql = $@"IF TYPE_ID('{tvpTypeName}') IS NOT NULL DROP TYPE {tvpTypeName}; CREATE TYPE {tvpTypeName} AS TABLE ({string.Join(", ", columns.Select(c => $"[{c}] NVARCHAR(MAX) NULL"))});";
        var nonKeyColumns = columns.Where(c => c != keyColumn).ToList();
        var updateSet = string.Join(", ", nonKeyColumns.Select(c => $"t.[{c}] = s.[{c}]"));
        var insertCols = string.Join(", ", columns.Select(c => $"[{c}]"));
        var insertVals = string.Join(", ", columns.Select(c => $"s.[{c}]"));
        var mergeSql = $@"MERGE {fullTable} AS t USING @rows AS s ON t.[{keyColumn}] = s.[{keyColumn}] WHEN MATCHED THEN UPDATE SET {updateSet} WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals});";
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await using (var cmd = new SqlCommand(createTvpSql, connection) { CommandTimeout = 30 })
                { await cmd.ExecuteNonQueryAsync(cancellationToken); }
            var dataTable = new DataTable();
            foreach (var col in columns) dataTable.Columns.Add(col, typeof(string));
            foreach (var row in rows)
            {
                var values = rowMapper(row).Select(r => r.Value?.ToString() ?? (object)DBNull.Value).ToArray();
                dataTable.Rows.Add(values);
            }
            await using (var cmd = new SqlCommand(mergeSql, connection) { CommandTimeout = 120 })
            {
                var param = cmd.Parameters.AddWithValue("@rows", dataTable);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = tvpTypeName;
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            try { await using var c = new SqlConnection(this.connectionString); await c.OpenAsync(cancellationToken); await using var cmd = new SqlCommand($"IF TYPE_ID('{tvpTypeName}') IS NOT NULL DROP TYPE {tvpTypeName}", c); await cmd.ExecuteNonQueryAsync(cancellationToken); } catch { }
        }
    }

    private void RecordOperation(string operationType, string tableName, int rowCount, TimeSpan duration)
    {
        lock (operationLog)
        {
            operationLog.Add(new BatchOperationRecord
            {
                OperationType = operationType,
                TableName = tableName,
                RowCount = rowCount,
                Duration = duration,
                RowsPerSecond = duration.TotalSeconds > 0 ? rowCount / duration.TotalSeconds : 0,
                ExecutedAt = DateTimeOffset.UtcNow,
            });
            Interlocked.Add(ref totalRowsProcessed, rowCount);
            Interlocked.Increment(ref totalOperations);
        }
    }
}

public sealed class BatchOperationStats
{
    [JsonPropertyName("totalOperations")] public long TotalOperations { get; set; }
    [JsonPropertyName("totalRowsProcessed")] public long TotalRowsProcessed { get; set; }
    [JsonPropertyName("avgRowsPerOperation")] public double AvgRowsPerOperation { get; set; }
    [JsonPropertyName("avgDurationMs")] public double AvgDurationMs { get; set; }
    [JsonPropertyName("recentOperations")] public IReadOnlyList<BatchOperationRecord> RecentOperations { get; set; } = [];
}

public sealed class BatchOperationRecord
{
    [JsonPropertyName("operationType")] public string OperationType { get; set; } = string.Empty;
    [JsonPropertyName("tableName")] public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("rowCount")] public int RowCount { get; set; }
    [JsonPropertyName("duration")] public TimeSpan Duration { get; set; }
    [JsonPropertyName("rowsPerSecond")] public double RowsPerSecond { get; set; }
    [JsonPropertyName("executedAt")] public DateTimeOffset ExecutedAt { get; set; }
}
