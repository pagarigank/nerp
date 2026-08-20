// <copyright file="W4Record.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Employee W-4 / state withholding record. Supports the 2020+ IRS Pub 15-T
/// percentage method (filing status, multiple jobs, dependents credit, other income, deductions)
/// and the legacy pre-2020 allowance method for grandfathered employees.</summary>
public class W4Record : AuditableEntity
{
    protected W4Record() { }

    public W4Record(
        Guid employeeId,
        FilingStatus filingStatus,
        int allowances = 0,
        bool isLegacyPre2020 = false,
        decimal? additionalWithholding = null,
        bool multipleJobs = false,
        int dependentsCredit = 0,
        decimal? otherIncome = null,
        decimal? deductions = null)
        : base(Guid.NewGuid())
    {
        EmployeeId = employeeId;
        FilingStatus = filingStatus;
        Allowances = allowances;
        IsLegacyPre2020 = isLegacyPre2020;
        AdditionalWithholding = additionalWithholding;
        MultipleJobs = multipleJobs;
        DependentsCredit = dependentsCredit;
        OtherIncome = otherIncome;
        Deductions = deductions;
        EffectiveDate = DateTime.UtcNow;
    }

    public Guid EmployeeId { get; private set; }
    public FilingStatus FilingStatus { get; private set; }
    public int Allowances { get; private set; }
    public bool IsLegacyPre2020 { get; private set; }
    public decimal? AdditionalWithholding { get; private set; }
    public bool MultipleJobs { get; private set; }
    public int DependentsCredit { get; private set; }
    public decimal? OtherIncome { get; private set; }
    public decimal? Deductions { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    public void Supersede(DateTime endDate) => EndDate = endDate;
}

public enum FilingStatus
{
    SingleFiler = 0,
    MarriedFilingJointly = 1,
    HeadOfHousehold = 2,
    MarriedFilingSeparately = 3,
}
