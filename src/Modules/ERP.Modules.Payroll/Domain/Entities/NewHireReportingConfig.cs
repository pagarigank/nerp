// <copyright file="NewHireReportingConfig.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Per-state new-hire reporting configuration: agency registration, reporting
/// due window, and transmission method. Drives the new-hire reporting background job.
/// </summary>
public class NewHireReportingConfig : AuditableEntity
{
    protected NewHireReportingConfig() { }

    public NewHireReportingConfig(
        Guid companyId,
        string stateCode,
        string agencyName,
        int dueWindowDays,
        string transmissionMethod,
        string? sftpEndpoint,
        string? agencyId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(stateCode)) throw new ArgumentException("State code is required.", nameof(stateCode));
        CompanyId = companyId;
        StateCode = stateCode;
        AgencyName = agencyName;
        DueWindowDays = dueWindowDays;
        TransmissionMethod = transmissionMethod;
        SftpEndpoint = sftpEndpoint;
        AgencyId = agencyId;
    }

    public Guid CompanyId { get; private set; }
    public string StateCode { get; private set; } = string.Empty;
    public string AgencyName { get; private set; } = string.Empty;
    public int DueWindowDays { get; private set; }
    public string TransmissionMethod { get; private set; } = string.Empty;
    public string? SftpEndpoint { get; private set; }
    public string? AgencyId { get; private set; }

    /// <summary>When the new-hire reporting file was transmitted to the state agency (null = pending).</summary>
    public DateTimeOffset? SubmittedOn { get; private set; }

    public void MarkSubmitted(DateTimeOffset submittedOn) => SubmittedOn = submittedOn;
}
