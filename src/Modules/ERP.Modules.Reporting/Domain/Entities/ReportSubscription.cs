// <copyright file="ReportSubscription.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class ReportSubscription : AuditableAggregateRoot
{
    protected ReportSubscription() { }

    public ReportSubscription(
        Guid companyId,
        Guid reportDefinitionId,
        string name,
        string? parametersJson,
        string exportFormat,
        string scheduleType,
        string? scheduleConfigJson,
        string? recipientsJson) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ReportDefinitionId = reportDefinitionId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParametersJson = parametersJson;
        ExportFormat = exportFormat ?? "PDF";
        ScheduleType = scheduleType ?? throw new ArgumentNullException(nameof(scheduleType));
        ScheduleConfigJson = scheduleConfigJson;
        RecipientsJson = recipientsJson;
        IsActive = true;
        Status = "Active";
    }

    public Guid CompanyId { get; private set; }
    public Guid ReportDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ParametersJson { get; private set; }
    public string ExportFormat { get; private set; } = string.Empty; // PDF, Excel, CSV
    public string ScheduleType { get; private set; } = string.Empty; // Daily, Weekly, Monthly, OnDemand
    public string? ScheduleConfigJson { get; private set; } // Cron expression or config
    public string? RecipientsJson { get; private set; }
    public DateTimeOffset? LastRunOn { get; private set; }
    public string? LastRunStatus { get; private set; }
    public string? LastRunError { get; private set; }
    public int RunCount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string name, string? parametersJson, string exportFormat,
        string scheduleType, string? scheduleConfigJson, string? recipientsJson)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParametersJson = parametersJson;
        ExportFormat = exportFormat ?? "PDF";
        ScheduleType = scheduleType ?? throw new ArgumentNullException(nameof(scheduleType));
        ScheduleConfigJson = scheduleConfigJson;
        RecipientsJson = recipientsJson;
    }

    public void RecordRun(string status, string? error = null)
    {
        RunCount++;
        LastRunOn = DateTimeOffset.UtcNow;
        LastRunStatus = status;
        LastRunError = error;
    }

    public void Activate()
    {
        IsActive = true;
        Status = "Active";
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = "Inactive";
    }
}
