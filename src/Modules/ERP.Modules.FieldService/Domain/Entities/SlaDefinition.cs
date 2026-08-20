// <copyright file="SlaDefinition.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum SlaPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class SlaDefinition : AuditableEntity
{
    protected SlaDefinition()
    {
    }

    public SlaDefinition(
        Guid companyId,
        string name,
        SlaPriority priority,
        int responseMinutes,
        int resolutionMinutes,
        bool escalate,
        string? escalationTo)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name;
        Priority = priority;
        ResponseMinutes = responseMinutes;
        ResolutionMinutes = resolutionMinutes;
        Escalate = escalate;
        EscalationTo = escalationTo;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SlaPriority Priority { get; private set; }
    public int ResponseMinutes { get; private set; }
    public int ResolutionMinutes { get; private set; }
    public bool Escalate { get; private set; }
    public string? EscalationTo { get; private set; }
}
