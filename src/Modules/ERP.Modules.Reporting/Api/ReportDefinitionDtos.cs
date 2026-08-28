// <copyright file="ReportDefinitionDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Reporting.Api;

public record ReportDefinitionDto(
    Guid Id, Guid CompanyId, string Name, string Module, string Category,
    string Description, string ReportType, string? DataSource, string? SqlQuery,
    string? ParametersJson, string? LayoutJson, bool IsShared, bool IsActive,
    DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateReportDefinitionRequest(
    Guid CompanyId, string Name, string Module, string Category,
    string Description, string ReportType, string? DataSource,
    string? SqlQuery, string? ParametersJson, string? LayoutJson);

public record UpdateReportDefinitionRequest(
    string Name, string Module, string Category, string Description,
    string ReportType, string? DataSource, string? SqlQuery,
    string? ParametersJson, string? LayoutJson);

public record ShareReportRequest(bool IsShared);

// --- SavedQuery DTOs ---
public record SavedQueryDto(
    Guid Id, Guid CompanyId, string Name, string Module, string QueryType,
    string? EntityName, string? FilterJson, string? SortJson, string? ColumnSelectionJson,
    string CreatedByUser, int RunCount, DateTimeOffset? LastRunOn,
    bool IsShared, bool IsActive, DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateSavedQueryRequest(
    Guid CompanyId, string Name, string Module, string QueryType,
    string? EntityName, string? FilterJson, string? SortJson,
    string? ColumnSelectionJson, string? CreatedByUser);

public record UpdateSavedQueryRequest(
    string Name, string Module, string QueryType, string? EntityName,
    string? FilterJson, string? SortJson, string? ColumnSelectionJson);

// --- DashboardWidget DTOs ---
public record DashboardWidgetDto(
    Guid Id, Guid CompanyId, string DashboardId, string Name, string WidgetType,
    string? DataSourceType, string? DataSourceConfigJson, string? DisplayConfigJson,
    int PositionX, int PositionY, int Width, int Height,
    int RefreshIntervalSeconds, bool IsActive, DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateDashboardWidgetRequest(
    Guid CompanyId, string DashboardId, string Name, string WidgetType,
    string? DataSourceType, string? DataSourceConfigJson, string? DisplayConfigJson,
    int PositionX, int PositionY, int Width, int Height);

public record UpdateDashboardWidgetRequest(
    string Name, string WidgetType, string? DataSourceType,
    string? DataSourceConfigJson, string? DisplayConfigJson,
    int PositionX, int PositionY, int Width, int Height);

// --- ReportSubscription DTOs ---
public record ReportSubscriptionDto(
    Guid Id, Guid CompanyId, Guid ReportDefinitionId, string Name,
    string? ParametersJson, string ExportFormat, string ScheduleType,
    string? ScheduleConfigJson, string? RecipientsJson,
    DateTimeOffset? LastRunOn, string? LastRunStatus, string? LastRunError,
    int RunCount, string Status, bool IsActive, DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateReportSubscriptionRequest(
    Guid CompanyId, Guid ReportDefinitionId, string Name, string? ParametersJson,
    string ExportFormat, string ScheduleType, string? ScheduleConfigJson,
    string? RecipientsJson);

public record UpdateReportSubscriptionRequest(
    string Name, string? ParametersJson, string ExportFormat,
    string ScheduleType, string? ScheduleConfigJson, string? RecipientsJson);

// --- FinancialStatementLayout DTOs ---
public record FinancialStatementLayoutDto(
    Guid Id, Guid CompanyId, string Name, string StatementType, string Description,
    string RowDefinitionsJson, string ColumnDefinitionsJson, string? TreeJson,
    bool SuppressZero, bool RoundToNearestDollar, int Version, bool IsApproved,
    bool IsActive, DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateStatementLayoutRequest(
    Guid CompanyId, string Name, string StatementType, string? Description,
    string RowDefinitionsJson, string ColumnDefinitionsJson, string? TreeJson,
    bool SuppressZero, bool RoundToNearestDollar);

public record UpdateStatementLayoutRequest(
    string Name, string StatementType, string? Description,
    string RowDefinitionsJson, string ColumnDefinitionsJson, string? TreeJson,
    bool SuppressZero, bool RoundToNearestDollar);

// --- QuickQuery DTOs ---
public record QuickQueryDto(
    Guid Id, Guid CompanyId, string Name, string EntityName,
    string? FilterJson, string? SortJson, string? ColumnSelectionJson,
    bool IncludeArchived, string CreatedByUser, int RunCount,
    DateTimeOffset? LastRunOn, bool IsShared, bool IsActive,
    DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateQuickQueryRequest(
    Guid CompanyId, string Name, string EntityName, string? FilterJson,
    string? SortJson, string? ColumnSelectionJson, bool IncludeArchived,
    string? CreatedByUser);

public record UpdateQuickQueryRequest(
    string Name, string EntityName, string? FilterJson,
    string? SortJson, string? ColumnSelectionJson, bool IncludeArchived);

// --- ReportExecution DTOs ---
public record ExecuteReportRequest(
    Guid? ReportDefinitionId,
    string? ReportType,
    string? ParametersJson,
    string ExportFormat);

public record ReportExecutionResult(
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    int TotalCount,
    long ExecutionTimeMs,
    Uri? ExportUrl);

public record ReportUsageLogDto(
    Guid Id, string ReportType, Guid? ReportDefinitionId,
    string? SavedQueryId, string ExecutedByUser, string? ParametersJson,
    string ExportFormat, long ExecutionTimeMs, int RowCount,
    string Status, string? ErrorMessage, DateTimeOffset ExecutedOn);

public record ReportUsageStatsDto(
    int TotalRuns,
    int UniqueReports,
    double AvgExecutionTimeMs,
    IReadOnlyList<ReportUsageLogDto> RecentRuns);

// --- Cache Statistics DTOs ---
public record CacheStatisticsDto(
    long Hits,
    long Misses,
    long Evictions,
    int EntryCount,
    double HitRatePercent);
