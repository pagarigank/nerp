// <copyright file="ReportUsageLog.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class ReportUsageLog : AuditableAggregateRoot
{
    protected ReportUsageLog() { }

    public ReportUsageLog(
        Guid companyId,
        string reportType,
        Guid? reportDefinitionId,
        string? savedQueryId,
        string? executedByUser,
        string? parametersJson,
        string exportFormat,
        long executionTimeMs,
        int rowCount) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ReportType = reportType ?? throw new ArgumentNullException(nameof(reportType));
        ReportDefinitionId = reportDefinitionId;
        SavedQueryId = savedQueryId;
        ExecutedByUser = executedByUser ?? string.Empty;
        ParametersJson = parametersJson;
        ExportFormat = exportFormat ?? "Screen";
        ExecutionTimeMs = executionTimeMs;
        RowCount = rowCount;
        Status = "Success";
        ExecutedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public string ReportType { get; private set; } = string.Empty;
    public Guid? ReportDefinitionId { get; private set; }
    public string? SavedQueryId { get; private set; }
    public string ExecutedByUser { get; private set; } = string.Empty;
    public string? ParametersJson { get; private set; }
    public string ExportFormat { get; private set; } = string.Empty;
    public long ExecutionTimeMs { get; private set; }
    public int RowCount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset ExecutedOn { get; private set; }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error;
    }
}
