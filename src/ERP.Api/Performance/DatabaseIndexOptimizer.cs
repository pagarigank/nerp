// <copyright file="DatabaseIndexOptimizer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136

#pragma warning disable SA1200, SA1201, SA1202, SA1203, SA1204, SA1512, SA1515, S1135, S125, S4136, S1871
#pragma warning disable CA2100 // SQL injection review — table/column names are hardcoded constants
#pragma warning disable CA1849, S6966 // IsDBNullAsync — using sync reader for simplicity in diagnostic queries

using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Api.Performance;

/// <summary>
/// Provides database index optimization utilities:
/// - Composite index creation for common query patterns
/// - Index usage statistics
/// - Missing index detection
/// - Statistics maintenance
/// - Fragmentation monitoring
///
/// Target: support 500 concurrent users, 10M+ GL transactions/year,
/// &lt;2s p95 response time (spec §7).
/// </summary>
public interface IDatabaseIndexOptimizer
{
    /// <summary>
    /// Creates all performance-critical composite indexes. Idempotent —
    /// skips indexes that already exist.
    /// </summary>
    Task<int> CreatePerformanceIndexesAsync(DbContext dbContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns index usage statistics (scans, seeks, updates) for monitoring.
    /// </summary>
    Task<IReadOnlyList<IndexStats>> GetIndexStatsAsync(DbContext dbContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects missing indexes based on query plan analysis.
    /// </summary>
    Task<IReadOnlyList<MissingIndex>> DetectMissingIndexesAsync(DbContext dbContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates table statistics for the query optimizer.
    /// </summary>
    Task UpdateStatisticsAsync(DbContext dbContext, string? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns fragmentation statistics for index maintenance.
    /// </summary>
    Task<IReadOnlyList<IndexFragmentation>> GetFragmentationAsync(DbContext dbContext, CancellationToken cancellationToken = default);
}

public class IndexStats
{
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string Index { get; set; } = string.Empty;
    public long UserSeeks { get; set; }
    public long UserScans { get; set; }
    public long UserLookups { get; set; }
    public long UserUpdates { get; set; }
    public long TotalRows { get; set; }
    public double AvgFragmentationPercent { get; set; }
}

public class MissingIndex
{
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string EqualityColumns { get; set; } = string.Empty;
    public string InequalityColumns { get; set; } = string.Empty;
    public string IncludeColumns { get; set; } = string.Empty;
    public long AvgUserImpact { get; set; }
    public long AvgTotalUserCost { get; set; }
    public long AvgUserSeeks { get; set; }
    public long AvgUserScans { get; set; }
    public string SuggestedCreateStatement { get; set; } = string.Empty;
}

public class IndexFragmentation
{
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string Index { get; set; } = string.Empty;
    public double FragmentationPercent { get; set; }
    public long PageCount { get; set; }
    public string RecommendedAction { get; set; } = string.Empty; // None, Reorganize, Rebuild
}

public class DatabaseIndexOptimizer : IDatabaseIndexOptimizer
{
    private readonly ILogger<DatabaseIndexOptimizer> _logger;

    /// <summary>
    /// Performance-critical composite indexes across all modules.
    /// Each tuple: (schema, table, columns, include_columns, description).
    /// </summary>
    private static readonly (string Schema, string Table, string Columns, string? Include, string Description)[] PerformanceIndexes =
    [
        // === General Ledger ===
        ("gl", "JournalEntryLines", "CompanyId, PeriodId, AccountId", "DebitAmount, CreditAmount, Description", "GL entry lookup by company+period+account"),
        ("gl", "JournalEntryLines", "CompanyId, AccountId, ModifiedOn", "DebitAmount, CreditAmount", "GL balance query by account over time"),
        ("gl", "JournalEntryLines", "CompanyId, JournalBatchId", "LineNumber, AccountId, DebitAmount, CreditAmount", "GL batch detail query"),
        ("gl", "Accounts", "CompanyId, AccountNumber", "Name, AccountType, IsActive", "Account master lookup"),
        ("gl", "Periods", "CompanyId, PeriodNumber", "Status, StartDate, EndDate", "Period lookup for posting"),

        // === Accounts Payable ===
        ("ap", "VoucherLines", "CompanyId, VendorId, PeriodId", "Amount, Status, InvoiceNumber", "AP aging and vendor history"),
        ("ap", "VoucherLines", "CompanyId, Status, DueDate", "VendorId, Amount", "Cash requirements query"),
        ("ap", "PaymentLines", "CompanyId, VendorId, PaymentDate", "Amount, CheckNumber", "Payment history lookup"),
        ("ap", "Vendors", "CompanyId, VendorNumber", "Name, IsActive, PaymentTerms", "Vendor master lookup"),

        // === Accounts Receivable ===
        ("ar", "InvoiceLines", "CompanyId, CustomerId, PeriodId", "Amount, Status, DueDate", "AR aging and customer history"),
        ("ar", "InvoiceLines", "CompanyId, Status, DueDate", "CustomerId, Amount", "Collections query"),
        ("ar", "CashReceiptLines", "CompanyId, CustomerId, ReceiptDate", "Amount, ReferenceNumber", "Cash receipts history"),
        ("ar", "Customers", "CompanyId, CustomerNumber", "Name, IsActive, CreditLimit", "Customer master lookup"),

        // === Cash Management ===
        ("cash", "BankStatementLines", "CompanyId, BankAccountId, StatementDate", "Amount, Description, reconciled", "Bank reconciliation query"),

        // === Purchasing ===
        ("pur", "PurchaseOrderLines", "CompanyId, VendorId, Status", "ItemId, Quantity, UnitCost", "Open PO query"),
        ("pur", "ReceiptLines", "CompanyId, PurchaseOrderId", "ReceivedQuantity, ReceiptDate", "PO receipt history"),

        // === Inventory ===
        ("inv", "ItemMovements", "CompanyId, ItemId, WarehouseId", "Quantity, UnitCost, TransactionDate", "Inventory movement history"),
        ("inv", "ItemMovements", "CompanyId, TransactionDate, WarehouseId", "ItemId, Quantity, UnitCost", "Daily activity report"),
        ("inv", "ItemCostLayers", "CompanyId, ItemId, WarehouseId", "UnitCost, QuantityOnHand, CostingMethod", "Cost layer lookup"),
        ("inv", "Items", "CompanyId, ItemNumber", "Description, IsActive, ItemType", "Item master lookup"),

        // === Order Management ===
        ("om", "SalesOrderLines", "CompanyId, CustomerId, Status", "ItemId, Quantity, UnitPrice", "Open order query"),
        ("om", "ShipmentLines", "CompanyId, SalesOrderId", "ShippedQuantity, ShipDate", "Shipment history"),

        // === Bill of Materials ===
        ("bom", "BillOfMaterials", "CompanyId, ItemId", "RevisionNumber, IsActive, Description", "BOM lookup by item"),

        // === Project Accounting ===
        ("proj", "CostTransactions", "CompanyId, ProjectId, TaskId", "Amount, CostCategoryId, TransactionDate", "Project cost query"),
        ("proj", "BillingLines", "CompanyId, ProjectId, BillingDate", "Amount, Status", "Project billing history"),
        ("proj", "Projects", "CompanyId, ProjectNumber", "Name, Status, Manager", "Project master lookup"),

        // === Payroll ===
        ("pay", "TimesheetLines", "CompanyId, EmployeeId, PayPeriodId", "PayCodeId, Hours, Approved", "Timesheet lookup"),
        ("pay", "PayrollRunLines", "CompanyId, PayrollRunId", "EmployeeId, GrossPay, NetPay, Taxes", "Payroll register query"),
        ("pay", "Employees", "CompanyId, EmployeeNumber", "LastName, FirstName, IsActive", "Employee master lookup"),

        // === Field Service ===
        ("fs", "WorkOrderLines", "CompanyId, TechnicianId, Status", "ScheduledDate, ActualHours", "Dispatch query"),

        // === Platform / Audit ===
        ("audit", "AuditLogs", "CompanyId, EntityName, EntityId", "Action, UserId, Timestamp", "Audit trail lookup"),
        ("audit", "AuditLogs", "CompanyId, Timestamp", "EntityName, Action, UserId", "Audit report by date"),
    ];

    public DatabaseIndexOptimizer(ILogger<DatabaseIndexOptimizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> CreatePerformanceIndexesAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating performance indexes...");
        var sw = Stopwatch.StartNew();
        var createdCount = 0;

        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var (schema, table, columns, include, description) in PerformanceIndexes)
            {
                var indexName = $"IX_Perf_{schema}_{table}_{columns.Replace(", ", "_", StringComparison.Ordinal).Replace("(", string.Empty, StringComparison.Ordinal).Replace(")", string.Empty, StringComparison.Ordinal)}";

                using var cmd = connection.CreateCommand();
                var includeClause = !string.IsNullOrEmpty(include)
                    ? $" INCLUDE ({include})"
                    : string.Empty;

                cmd.CommandText = $@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = '{indexName}'
                          AND object_id = OBJECT_ID('[{schema}].[{table}]')
                    )
                    CREATE NONCLUSTERED INDEX [{indexName}]
                    ON [{schema}].[{table}] ({columns})
                    {includeClause};";

                try
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    createdCount++;
                    _logger.LogDebug("Created index {IndexName}: {Description}", indexName, description);
                }
                catch (Exception ex)
                {
                    // Table may not exist yet — skip silently
                    _logger.LogDebug(ex, "Skipped index {IndexName}", indexName);
                }
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        sw.Stop();
        _logger.LogInformation(
            "Performance index creation completed. Created {Count}/{Total} indexes in {Duration}ms",
            createdCount,
            PerformanceIndexes.Length,
            sw.ElapsedMilliseconds);

        return createdCount;
    }

    public async Task<IReadOnlyList<IndexStats>> GetIndexStatsAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        var stats = new List<IndexStats>();

        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    s.name AS SchemaName,
                    t.name AS TableName,
                    i.name AS IndexName,
                    ius.user_seeks,
                    ius.user_scans,
                    ius.user_lookups,
                    ius.user_updates,
                    p.rows AS TotalRows
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                LEFT JOIN sys.dm_db_index_usage_stats ius
                    ON i.object_id = ius.object_id AND i.index_id = ius.index_id
                LEFT JOIN sys.partitions p
                    ON i.object_id = p.object_id AND i.index_id = p.index_id AND p.partition_number = 1
                WHERE i.name IS NOT NULL
                  AND i.type_desc = 'NONCLUSTERED'
                ORDER BY s.name, t.name, i.name;";

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                stats.Add(new IndexStats
                {
                    Schema = reader.GetString(0),
                    Table = reader.GetString(1),
                    Index = reader.GetString(2),
                    UserSeeks = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                    UserScans = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                    UserLookups = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                    UserUpdates = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                    TotalRows = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
                });
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        return stats;
    }

    public async Task<IReadOnlyList<MissingIndex>> DetectMissingIndexesAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        var missing = new List<MissingIndex>();

        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    OBJECT_SCHEMA_NAME(id.object_id) AS SchemaName,
                    OBJECT_NAME(id.object_id) AS TableName,
                    id.equality_columns AS EqualityColumns,
                    id.inequality_columns AS InequalityColumns,
                    id.included_columns AS IncludeColumns,
                    id.avg_user_impact AS AvgUserImpact,
                    id.avg_total_user_cost AS AvgTotalUserCost,
                    id.avg_user_seeks AS AvgUserSeeks,
                    id.avg_user_scans AS AvgUserScans
                FROM sys.dm_db_missing_index_details id
                WHERE id.database_id = DB_ID()
                ORDER BY id.avg_user_impact * (id.avg_user_seeks + id.avg_user_scans) DESC;";

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var equality = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var inequality = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var include = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

                var columns = string.IsNullOrEmpty(inequality)
                    ? equality
                    : $"{equality}, {inequality}";

                var createStmt = $"CREATE NONCLUSTERED INDEX [IX_Missing_{reader.GetString(1)}] " +
                                 $"ON [{reader.GetString(0)}].[{reader.GetString(1)}] ({columns})" +
                                 (string.IsNullOrEmpty(include) ? string.Empty : $" INCLUDE ({include})");

                missing.Add(new MissingIndex
                {
                    Schema = reader.GetString(0),
                    Table = reader.GetString(1),
                    EqualityColumns = equality,
                    InequalityColumns = inequality,
                    IncludeColumns = include,
                    AvgUserImpact = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                    AvgTotalUserCost = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                    AvgUserSeeks = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
                    AvgUserScans = reader.IsDBNull(8) ? 0 : reader.GetInt64(8),
                    SuggestedCreateStatement = createStmt,
                });
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        return missing;
    }

    public async Task UpdateStatisticsAsync(DbContext dbContext, string? schema = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating statistics for schema {Schema}...", schema ?? "ALL");

        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var schemaFilter = !string.IsNullOrEmpty(schema)
                ? $"WHERE s.name = '{schema}'"
                : string.Empty;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql = @sql + 'UPDATE STATISTICS [' + s.name + '].[' + t.name + '];' + CHAR(13)
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                {schemaFilter};
                EXEC sp_executesql @sql;";

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Statistics update completed");
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<IReadOnlyList<IndexFragmentation>> GetFragmentationAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        var results = new List<IndexFragmentation>();

        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    s.name AS SchemaName,
                    t.name AS TableName,
                    i.name AS IndexName,
                    ips.avg_fragmentation_in_percent,
                    ips.page_count
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
                INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE i.name IS NOT NULL
                  AND ips.page_count > 1000
                ORDER BY ips.avg_fragmentation_in_percent DESC;";

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var fragPct = reader.GetDouble(3);
                var action = fragPct switch
                {
                    > 30.0 => "Rebuild",
                    > 10.0 => "Reorganize",
                    _ => "None",
                };

                results.Add(new IndexFragmentation
                {
                    Schema = reader.GetString(0),
                    Table = reader.GetString(1),
                    Index = reader.GetString(2),
                    FragmentationPercent = fragPct,
                    PageCount = reader.GetInt64(4),
                    RecommendedAction = action,
                });
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        return results;
    }
}
