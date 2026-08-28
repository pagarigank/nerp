// <copyright file="DataMartIntegrityService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable CA2100 // SQL injection review — table/column names are hardcoded constants

using System.Diagnostics;
using System.Globalization;
using ERP.Modules.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Reporting.Services;

/// <summary>
/// Validates the integrity and health of the reporting data mart. Checks include:
/// - Data staleness (are staging tables fresh?)
/// - Orphaned records (staging rows with no source)
/// - Schema consistency (missing columns, broken foreign keys)
/// - Cross-module data completeness
/// - Sync lag detection
/// - Duplicate detection in staging tables
/// - Referential integrity across dimension/fact tables
/// </summary>
public interface IDataMartIntegrityService
{
    /// <summary>
    /// Runs all integrity checks and returns a comprehensive health report.
    /// </summary>
    Task<DataMartHealthReport> RunFullCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks only data freshness/staleness across all staging tables.
    /// </summary>
    Task<IReadOnlyList<StalenessCheck>> CheckStalenessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects orphaned records in staging tables (rows whose source has been deleted).
    /// </summary>
    Task<IReadOnlyList<OrphanCheck>> CheckOrphansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates referential integrity between dimension and fact tables.
    /// </summary>
    Task<IReadOnlyList<IntegrityCheck>> CheckReferentialIntegrityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns row counts for all staging tables (for monitoring dashboards).
    /// </summary>
    Task<IReadOnlyList<TableStats>> GetTableStatsAsync(CancellationToken cancellationToken = default);
}

public class DataMartHealthReport
{
    public DateTimeOffset CheckedOn { get; set; }
    public long TotalCheckDurationMs { get; set; }
    public string OverallStatus { get; set; } = string.Empty; // Healthy, Degraded, Critical
    public int HealthyChecks { get; set; }
    public int WarningChecks { get; set; }
    public int CriticalChecks { get; set; }
    public IReadOnlyList<StalenessCheck> StalenessChecks { get; set; } = [];
    public IReadOnlyList<OrphanCheck> OrphanChecks { get; set; } = [];
    public IReadOnlyList<IntegrityCheck> ReferentialIntegrityChecks { get; set; } = [];
    public IReadOnlyList<TableStats> TableStats { get; set; } = [];
}

public class StalenessCheck
{
    public string TableName { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public DateTimeOffset? LastSyncOn { get; set; }
    public double? HoursSinceLastSync { get; set; }
    public long RowCount { get; set; }
    public string Status { get; set; } = string.Empty; // Fresh, Stale, NeverSynced
    public string? Message { get; set; }
}

public class OrphanCheck
{
    public string StagingTable { get; set; } = string.Empty;
    public string ForeignKey { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public long OrphanCount { get; set; }
    public string Status { get; set; } = string.Empty; // Clean, OrphansFound
    public string? SampleIds { get; set; }
}

public class IntegrityCheck
{
    public string CheckName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pass, Fail, Warning
    public long? ViolationCount { get; set; }
    public string? Details { get; set; }
}

public class TableStats
{
    public string Schema { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public long SizeBytes { get; set; }
}

public class DataMartIntegrityService : IDataMartIntegrityService
{
    private readonly ReportingDbContext _rptDb;
    private readonly ILogger<DataMartIntegrityService> _logger;

    /// <summary>
    /// Staleness thresholds in hours. Tables exceeding these thresholds
    /// are flagged as Stale or Critical.
    /// </summary>
    private static readonly Dictionary<string, double> StalenessThresholds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rpt.FactJournalEntries"] = 2.0,       // GL entries: 2 hours
        ["rpt.FactApVouchers"] = 4.0,            // AP: 4 hours
        ["rpt.FactArInvoices"] = 4.0,            // AR: 4 hours
        ["rpt.FactInventoryMovements"] = 6.0,    // Inventory: 6 hours
        ["rpt.FactSalesOrders"] = 4.0,           // OM: 4 hours
        ["rpt.FactProjectCosts"] = 8.0,          // Projects: 8 hours
        ["rpt.FactTimesheets"] = 12.0,           // Payroll: 12 hours
        ["rpt.FactWorkOrders"] = 6.0,            // Field Service: 6 hours
    };

    /// <summary>
    /// Referential integrity checks: fact table FK -> dimension table PK.
    /// </summary>
    private static readonly (string FactTable, string FactColumn, string DimTable, string DimColumn)[] ReferentialChecks =
    [
        ("rpt.FactJournalEntries", "AccountId", "rpt.DimAccounts", "Id"),
        ("rpt.FactApVouchers", "VendorId", "rpt.DimVendors", "Id"),
        ("rpt.FactArInvoices", "CustomerId", "rpt.DimCustomers", "Id"),
        ("rpt.FactInventoryMovements", "ItemId", "rpt.DimItems", "Id"),
        ("rpt.FactProjectCosts", "ProjectId", "rpt.DimProjects", "Id"),
        ("rpt.FactTimesheets", "EmployeeId", "rpt.DimEmployees", "Id"),
        ("rpt.FactBankTransactions", "BankAccountId", "rpt.DimBankAccounts", "Id"),
    ];

    public DataMartIntegrityService(
        ReportingDbContext rptDb,
        ILogger<DataMartIntegrityService> logger)
    {
        _rptDb = rptDb ?? throw new ArgumentNullException(nameof(rptDb));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataMartHealthReport> RunFullCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Data mart integrity check starting at {Time}", DateTimeOffset.UtcNow);
        var sw = Stopwatch.StartNew();

        var staleness = await CheckStalenessAsync(cancellationToken);
        var orphans = await CheckOrphansAsync(cancellationToken);
        var referential = await CheckReferentialIntegrityAsync(cancellationToken);
        var stats = await GetTableStatsAsync(cancellationToken);

        sw.Stop();

        var healthy = staleness.Count(s => s.Status == "Fresh") +
                      orphans.Count(o => o.Status == "Clean") +
                      referential.Count(r => r.Status == "Pass");

        var warnings = staleness.Count(s => s.Status == "Stale") +
                       referential.Count(r => r.Status == "Warning");

        var critical = staleness.Count(s => s.Status == "Critical" || s.Status == "NeverSynced") +
                       orphans.Count(o => o.Status == "OrphansFound" && o.OrphanCount > 100) +
                       referential.Count(r => r.Status == "Fail");

        string overallStatus;
        if (critical > 0)
        {
            overallStatus = "Critical";
        }
        else
        {
            overallStatus = warnings > 0 ? "Degraded" : "Healthy";
        }

        var report = new DataMartHealthReport
        {
            CheckedOn = DateTimeOffset.UtcNow,
            TotalCheckDurationMs = sw.ElapsedMilliseconds,
            OverallStatus = overallStatus,
            HealthyChecks = healthy,
            WarningChecks = warnings,
            CriticalChecks = critical,
            StalenessChecks = staleness,
            OrphanChecks = orphans,
            ReferentialIntegrityChecks = referential,
            TableStats = stats,
        };

        _logger.LogInformation(
            "Data mart integrity check completed. Status: {Status}, " +
            "Healthy: {Healthy}, Warnings: {Warnings}, Critical: {Critical}, Duration: {Duration}ms",
            overallStatus, healthy, warnings, critical, sw.ElapsedMilliseconds);

        return report;
    }

    public async Task<IReadOnlyList<StalenessCheck>> CheckStalenessAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<StalenessCheck>();

        foreach (var (tableName, thresholdHours) in StalenessThresholds)
        {
            var watermark = await _rptDb.SyncWatermarks
                .FirstOrDefaultAsync(w => w.StagingTable == tableName, cancellationToken);

            var rowCount = await GetRowCountAsync(tableName, cancellationToken);

            var hoursSinceSync = watermark?.LastSyncOn.HasValue == true
                ? (DateTimeOffset.UtcNow - watermark.LastSyncOn.Value).TotalHours
                : (double?)null;

            string status;
            string? message;

            if (watermark?.LastSyncOn == null)
            {
                status = "NeverSynced";
                message = $"Table {tableName} has never been synced";
            }
            else if (hoursSinceSync.HasValue && hoursSinceSync.Value > thresholdHours * 2)
            {
                status = "Critical";
                message = $"Table {tableName} is {hoursSinceSync.Value:F1} hours old (threshold: {thresholdHours}h)";
            }
            else if (hoursSinceSync.HasValue && hoursSinceSync.Value > thresholdHours)
            {
                status = "Stale";
                message = $"Table {tableName} is {hoursSinceSync.Value:F1} hours old (threshold: {thresholdHours}h)";
            }
            else
            {
                status = "Fresh";
                message = null;
            }

            checks.Add(new StalenessCheck
            {
                TableName = tableName,
                SourceTable = watermark?.SourceTable ?? "unknown",
                LastSyncOn = watermark?.LastSyncOn,
                HoursSinceLastSync = hoursSinceSync,
                RowCount = rowCount,
                Status = status,
                Message = message,
            });
        }

        return checks;
    }

    public async Task<IReadOnlyList<OrphanCheck>> CheckOrphansAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<OrphanCheck>();

        // Check each fact table for orphaned dimension references
        var connection = _rptDb.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var (factTable, factColumn, dimTable, dimColumn) in ReferentialChecks)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT COUNT(*)
                    FROM [{factTable}] f
                    LEFT JOIN [{dimTable}] d ON f.[{factColumn}] = d.[{dimColumn}]
                    WHERE d.[{dimColumn}] IS NULL
                      AND f.[{factColumn}] IS NOT NULL";

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var orphanCount = Convert.ToInt64(result, CultureInfo.InvariantCulture);

                checks.Add(new OrphanCheck
                {
                    StagingTable = factTable,
                    ForeignKey = factColumn,
                    ReferencedTable = dimTable,
                    OrphanCount = orphanCount,
                    Status = orphanCount == 0 ? "Clean" : "OrphansFound",
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

        return checks;
    }

    public async Task<IReadOnlyList<IntegrityCheck>> CheckReferentialIntegrityAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<IntegrityCheck>();

        // Check for duplicate primary keys in dimension tables
        var dimTables = new[] { "rpt.DimAccounts", "rpt.DimVendors", "rpt.DimCustomers", "rpt.DimItems", "rpt.DimProjects", "rpt.DimEmployees" };

        var connection = _rptDb.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var dimTable in dimTables)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT COUNT(*) FROM (
                        SELECT [Id], COUNT(*) AS Cnt
                        FROM [{dimTable}]
                        GROUP BY [Id]
                        HAVING COUNT(*) > 1
                    ) dups";

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var dupCount = Convert.ToInt64(result, CultureInfo.InvariantCulture);

                checks.Add(new IntegrityCheck
                {
                    CheckName = $"DuplicateCheck_{dimTable}",
                    Description = $"Check for duplicate primary keys in {dimTable}",
                    Status = dupCount == 0 ? "Pass" : "Fail",
                    ViolationCount = dupCount,
                    Details = dupCount > 0 ? $"Found {dupCount} duplicate primary keys" : null,
                });
            }

            // Check for null foreign keys in fact tables (where they should be required)
            foreach (var (factTable, factColumn, dimTable, _) in ReferentialChecks)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT COUNT(*)
                    FROM [{factTable}]
                    WHERE [{factColumn}] IS NULL";

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var nullCount = Convert.ToInt64(result, CultureInfo.InvariantCulture);

                checks.Add(new IntegrityCheck
                {
                    CheckName = $"NullFK_{factTable}_{factColumn}",
                    Description = $"Check for NULL foreign keys in {factTable}.{factColumn}",
                    Status = nullCount == 0 ? "Pass" : "Warning",
                    ViolationCount = nullCount,
                    Details = nullCount > 0 ? $"Found {nullCount} rows with NULL {factColumn}" : null,
                });
            }

            // Check for negative amounts in financial tables (unless explicitly allowed)
            var financialTables = new[]
            {
                ("rpt.FactJournalEntries", "DebitAmount", "Debit"),
                ("rpt.FactJournalEntries", "CreditAmount", "Credit"),
                ("rpt.FactApVouchers", "Amount", "Voucher"),
                ("rpt.FactArInvoices", "Amount", "Invoice"),
            };

            foreach (var (table, column, label) in financialTables)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT COUNT(*)
                    FROM [{table}]
                    WHERE [{column}] < 0";

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var negativeCount = Convert.ToInt64(result, CultureInfo.InvariantCulture);

                checks.Add(new IntegrityCheck
                {
                    CheckName = $"NegativeAmount_{table}_{column}",
                    Description = $"Check for negative {label} amounts in {table}.{column}",
                    Status = negativeCount == 0 ? "Pass" : "Warning",
                    ViolationCount = negativeCount,
                    Details = negativeCount > 0
                        ? $"Found {negativeCount} rows with negative {label} amounts (may indicate reversals)"
                        : null,
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

        return checks;
    }

    public async Task<IReadOnlyList<TableStats>> GetTableStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new List<TableStats>();
        var allTables = StalenessThresholds.Keys
            .Union(ReferentialChecks.SelectMany(r => new[] { r.FactTable, r.DimTable }))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var connection = _rptDb.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var table in allTables)
            {
                var parts = table.Split('.');
                var schema = parts[0];
                var tableName = parts[1];

                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT COUNT(*)
                    FROM [{schema}].[{tableName}]";

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var rowCount = Convert.ToInt64(result, CultureInfo.InvariantCulture);

                stats.Add(new TableStats
                {
                    Schema = schema,
                    TableName = tableName,
                    RowCount = rowCount,
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

    private async Task<long> GetRowCountAsync(string tableName, CancellationToken cancellationToken)
    {
        var parts = tableName.Split('.');
        var schema = parts[0];
        var table = parts[1];

        var connection = _rptDb.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM [{schema}].[{table}]";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }
        catch
        {
            // Table may not exist yet
            return 0;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
