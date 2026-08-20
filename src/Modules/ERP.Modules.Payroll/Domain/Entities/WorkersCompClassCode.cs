// <copyright file="WorkersCompClassCode.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Workers' compensation class code (rate per $100 payroll, state, experience mod).</summary>
public class WorkersCompClassCode : AuditableEntity
{
    protected WorkersCompClassCode() { }

    public WorkersCompClassCode(Guid companyId, string classCode, string description, string state, decimal ratePer100, decimal? experienceModification = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ClassCode = classCode;
        Description = description;
        State = state;
        RatePer100 = ratePer100;
        ExperienceModification = experienceModification ?? 1.0m;
    }

    public Guid CompanyId { get; private set; }
    public string ClassCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public decimal RatePer100 { get; private set; }
    public decimal ExperienceModification { get; private set; } = 1.0m;
}
