// <copyright file="DatabaseArchivalService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413, SA1208, SA1512, SA1204, SA1117, SA1134
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, CA1002, CA2227, S1118, SA1501, SA1107, SA1513, CA2100, CA1307, S2486, S108, S3358

using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Serilog;

namespace ERP.Api.Performance;

public interface IDatabaseArchivalService
{
    Task<ArchivalReport> RunArchivalAsync(int yearsToRetain = 3, CancellationToken cancellationToken = default);
    Task<ArchivalEstimate> EstimateArchivalSizeAsync(int yearsToRetain = 3, CancellationToken cancellationToken = default);
}

public sealed class DatabaseArchivalService : IDatabaseArchivalService
{
    private readonly string connectionString;

    public DatabaseArchivalService(IConfiguration configuration)
    {
        this.connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<ArchivalEstimate> EstimateArchivalSizeAsync(int yearsToRetain = 3, CancellationToken cancellationToken = default)
    {
        var estimate = new ArchivalEstimate
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            YearsToRetain = yearsToRetain,
            CutoffDate = DateTime.UtcNow.AddYears(-yearsToRetain),
        };
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            const string sizeSql = @"SELECT s.name AS schema_name, t.name AS table_name, p.rows AS row_count, SUM(a.total_pages) * 8 / 1024.0 AS total_size_mb FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1) JOIN sys.allocation_units a ON p.partition_id = a.container_id WHERE t.name IN ('JournalBatchLines', 'GlTransactions', 'VoucherLines', 'PaymentLines', 'InvoiceLines', 'CashReceiptLines', 'PurchaseOrderLines', 'ReceiptLines', 'InventoryTransactions', 'OrderLines', 'ShipmentLines', 'ProjectCostEntries', 'PayrollLines', 'AuditLogs') GROUP BY s.name, t.name, p.rows ORDER BY total_size_mb DESC";
            await using var cmd = new SqlCommand(sizeSql, connection) { CommandTimeout = 30 };
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var tableSize = new TableSizeInfo
                {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1),
                    RowCount = reader.GetInt64(2),
                    SizeMb = reader.GetDouble(3),
                };
                estimate.Tables.Add(tableSize);
                estimate.TotalOperationalSizeMb += tableSize.SizeMb;
            }
            foreach (var table in estimate.Tables)
            {
                var estimatedArchivableRows = await EstimateOldRowsAsync(connection, table.SchemaName, table.TableName, estimate.CutoffDate, cancellationToken);
                table.EstimatedArchivableRows = estimatedArchivableRows;
                table.EstimatedArchivableSizeMb = table.RowCount > 0 ? table.SizeMb * ((double)estimatedArchivableRows / table.RowCount) : 0;
                estimate.TotalArchivableRows += estimatedArchivableRows;
                estimate.TotalArchivableSizeMb += table.EstimatedArchivableSizeMb;
            }
        }
        catch (Exception ex) { Log.Warning(ex, "Failed to estimate archival size"); }
        return estimate;
    }

    public async Task<ArchivalReport> RunArchivalAsync(int yearsToRetain = 3, CancellationToken cancellationToken = default)
    {
        var report = new ArchivalReport
        {
            StartedAt = DateTimeOffset.UtcNow,
            YearsToRetain = yearsToRetain,
            CutoffDate = DateTime.UtcNow.AddYears(-yearsToRetain),
        };
        try
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(connection, "IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'archive') EXEC('CREATE SCHEMA archive')", cancellationToken);
            string[][] archiveableTables = [
                ["gl", "JournalBatchLines"], ["gl", "GlTransactions"],
                ["ap", "VoucherLines"], ["ap", "PaymentLines"],
                ["ar", "InvoiceLines"], ["ar", "CashReceiptLines"],
                ["pur", "PurchaseOrderLines"], ["pur", "ReceiptLines"],
                ["inv", "InventoryTransactions"], ["om", "OrderLines"],
                ["om", "ShipmentLines"], ["proj", "ProjectCostEntries"],
                ["pay", "PayrollLines"], ["audit", "AuditLogs"],
            ];
            foreach (var table in archiveableTables)
            {
                try
                {
                    var result = await ArchiveTableAsync(connection, table[0], table[1], report.CutoffDate, cancellationToken);
                    report.TableResults.Add(result);
                    report.TotalRowsArchived += result.RowsArchived;
                    report.TotalRowsRemaining += result.RowsRemaining;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to archive table {Schema}.{Table}", table[0], table[1]);
                    report.TableResults.Add(new TableArchivalResult { SchemaName = table[0], TableName = table[1], Status = "Error", ErrorMessage = ex.Message });
                }
            }
        }
        catch (Exception ex) { Log.Error(ex, "Archival process failed"); report.ErrorMessage = ex.Message; }
        report.CompletedAt = DateTimeOffset.UtcNow;
        report.Duration = report.CompletedAt - report.StartedAt;
        return report;
    }

    private static async Task<TableArchivalResult> ArchiveTableAsync(SqlConnection connection, string schemaName, string tableName, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var result = new TableArchivalResult { SchemaName = schemaName, TableName = tableName, CutoffDate = cutoffDate };
        var dateColumn = await FindDateColumnAsync(connection, schemaName, tableName, cancellationToken);
        if (dateColumn == null) { result.Status = "Skipped"; result.ErrorMessage = "No date column found"; return result; }
        var archiveTable = $"archive.{tableName}_archive";
        var createArchiveSql = $@"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{tableName}_archive' AND schema_id = (SELECT schema_id FROM sys.schemas WHERE name = 'archive')) BEGIN SELECT * INTO {archiveTable} FROM {schemaName}.{tableName} WHERE 1 = 0; ALTER TABLE {archiveTable} ADD ArchivedAt DATETIME2 DEFAULT SYSUTCDATETIME(); END";
        await ExecuteNonQueryAsync(connection, createArchiveSql, cancellationToken);
        var countSql = $"SELECT COUNT(*) FROM {schemaName}.{tableName} WHERE {dateColumn} < @cutoff";
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            countCmd.Parameters.AddWithValue("@cutoff", cutoffDate);
            countCmd.CommandTimeout = 120;
            var count = await countCmd.ExecuteScalarAsync(cancellationToken);
            result.RowsToArchive = Convert.ToInt64(count);
        }
        if (result.RowsToArchive == 0) { result.Status = "Skipped"; result.ErrorMessage = "No rows older than cutoff"; return result; }
        const int batchSize = 10000;
        long totalMoved = 0;
        while (totalMoved < result.RowsToArchive)
        {
            var moveSql = $@";WITH cte AS (SELECT TOP ({batchSize}) * FROM {schemaName}.{tableName} WHERE {dateColumn} < @cutoff ORDER BY {dateColumn}) INSERT INTO {archiveTable} SELECT *, SYSUTCDATETIME() FROM cte; DELETE cte;";
            await using var moveCmd = new SqlCommand(moveSql, connection);
            moveCmd.Parameters.AddWithValue("@cutoff", cutoffDate);
            moveCmd.CommandTimeout = 300;
            var moved = await moveCmd.ExecuteNonQueryAsync(cancellationToken);
            if (moved == 0) break;
            totalMoved += moved;
        }
        result.RowsArchived = totalMoved;
        result.Status = "Completed";
        var remainSql = $"SELECT COUNT(*) FROM {schemaName}.{tableName}";
        await using (var remainCmd = new SqlCommand(remainSql, connection))
        {
            remainCmd.CommandTimeout = 60;
            var remain = await remainCmd.ExecuteScalarAsync(cancellationToken);
            result.RowsRemaining = Convert.ToInt64(remain);
        }
        return result;
    }

    private static async Task<string?> FindDateColumnAsync(SqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT TOP 1 c.name FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id WHERE c.object_id = OBJECT_ID(@fullTableName) AND t.name IN ('datetime', 'datetime2', 'date') AND c.name IN ('CreatedDate', 'TransactionDate', 'PostDate', 'LogDate', 'ModifiedDate', 'VoucherDate', 'InvoiceDate', 'BatchDate', 'ReceiptDate', 'OrderDate', 'ShipmentDate', 'PayrollDate') ORDER BY CASE c.name WHEN 'TransactionDate' THEN 1 WHEN 'PostDate' THEN 2 WHEN 'BatchDate' THEN 3 WHEN 'CreatedDate' THEN 4 ELSE 5 END";
        try
        {
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@fullTableName", $"{schemaName}.{tableName}");
            cmd.CommandTimeout = 10;
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result as string;
        }
        catch { return null; }
    }

    private static async Task<long> EstimateOldRowsAsync(SqlConnection connection, string schemaName, string tableName, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var dateColumn = await FindDateColumnAsync(connection, schemaName, tableName, cancellationToken);
        if (dateColumn == null) return 0;
        try
        {
            var sql = $"SELECT COUNT(*) FROM {schemaName}.{tableName} WHERE {dateColumn} < @cutoff";
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@cutoff", cutoffDate);
            cmd.CommandTimeout = 60;
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        catch { return 0; }
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class ArchivalReport
{
    [JsonPropertyName("startedAt")] public DateTimeOffset StartedAt { get; set; }
    [JsonPropertyName("completedAt")] public DateTimeOffset CompletedAt { get; set; }
    [JsonPropertyName("duration")] public TimeSpan Duration { get; set; }
    [JsonPropertyName("yearsToRetain")] public int YearsToRetain { get; set; }
    [JsonPropertyName("cutoffDate")] public DateTime CutoffDate { get; set; }
    [JsonPropertyName("totalRowsArchived")] public long TotalRowsArchived { get; set; }
    [JsonPropertyName("totalRowsRemaining")] public long TotalRowsRemaining { get; set; }
    [JsonPropertyName("tableResults")] public List<TableArchivalResult> TableResults { get; } = [];
    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; set; }
}

public sealed class TableArchivalResult
{
    [JsonPropertyName("schemaName")] public string SchemaName { get; set; } = string.Empty;
    [JsonPropertyName("tableName")] public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("cutoffDate")] public DateTime CutoffDate { get; set; }
    [JsonPropertyName("rowsToArchive")] public long RowsToArchive { get; set; }
    [JsonPropertyName("rowsArchived")] public long RowsArchived { get; set; }
    [JsonPropertyName("rowsRemaining")] public long RowsRemaining { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; set; }
}

public sealed class ArchivalEstimate
{
    [JsonPropertyName("generatedAt")] public DateTimeOffset GeneratedAt { get; set; }
    [JsonPropertyName("yearsToRetain")] public int YearsToRetain { get; set; }
    [JsonPropertyName("cutoffDate")] public DateTime CutoffDate { get; set; }
    [JsonPropertyName("tables")] public List<TableSizeInfo> Tables { get; } = [];
    [JsonPropertyName("totalOperationalSizeMb")] public double TotalOperationalSizeMb { get; set; }
    [JsonPropertyName("totalArchivableRows")] public long TotalArchivableRows { get; set; }
    [JsonPropertyName("totalArchivableSizeMb")] public double TotalArchivableSizeMb { get; set; }
}

public sealed class TableSizeInfo
{
    [JsonPropertyName("schemaName")] public string SchemaName { get; set; } = string.Empty;
    [JsonPropertyName("tableName")] public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("rowCount")] public long RowCount { get; set; }
    [JsonPropertyName("sizeMb")] public double SizeMb { get; set; }
    [JsonPropertyName("estimatedArchivableRows")] public long EstimatedArchivableRows { get; set; }
    [JsonPropertyName("estimatedArchivableSizeMb")] public double EstimatedArchivableSizeMb { get; set; }
}
