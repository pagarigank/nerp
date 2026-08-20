// <copyright file="EmployeeCompensation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Compensation history record for an employee (rate, OT/DT rates, salary), effective-dated.</summary>
public class EmployeeCompensation : AuditableEntity
{
    protected EmployeeCompensation() { }

    public EmployeeCompensation(
        Guid employeeId,
        decimal payRate,
        DateTime effectiveDate,
        decimal? overtimeRate = null,
        decimal? doubleTimeRate = null,
        bool isSalary = false,
        decimal? salaryAmount = null)
        : base(Guid.NewGuid())
    {
        EmployeeId = employeeId;
        PayRate = payRate;
        OvertimeRate = overtimeRate ?? payRate * 1.5m;
        DoubleTimeRate = doubleTimeRate ?? payRate * 2m;
        IsSalary = isSalary;
        SalaryAmount = salaryAmount;
        EffectiveDate = effectiveDate;
    }

    public Guid EmployeeId { get; private set; }
    public decimal PayRate { get; private set; }
    public decimal OvertimeRate { get; private set; }
    public decimal DoubleTimeRate { get; private set; }
    public bool IsSalary { get; private set; }
    public decimal? SalaryAmount { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    public void End(DateTime endDate)
    {
        EndDate = endDate;
    }

    public void Update(decimal? payRate, decimal? overtimeRate, decimal? doubleTimeRate, decimal? salaryAmount)
    {
        if (payRate.HasValue) PayRate = payRate.Value;
        if (overtimeRate.HasValue) OvertimeRate = overtimeRate.Value;
        if (doubleTimeRate.HasValue) DoubleTimeRate = doubleTimeRate.Value;
        if (salaryAmount.HasValue) SalaryAmount = salaryAmount.Value;
    }
}
