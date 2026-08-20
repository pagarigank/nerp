// <copyright file="PayrollRunLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>A single employee's computed pay within a payroll run (gross, taxes, net, employer taxes, fringes).</summary>
public class PayrollRunLine : AuditableEntity
{
    protected PayrollRunLine() { }

    public PayrollRunLine(
        Guid payrollRunId,
        Guid employeeId,
        decimal regularHours,
        decimal overtimeHours,
        decimal regularRate,
        decimal overtimeRate,
        decimal grossPay,
        decimal employeeTax,
        decimal deductions,
        decimal employerTax,
        decimal netPay,
        decimal? prevailingWageRate = null,
        decimal? fringeRate = null,
        string? tradeClassification = null)
        : base(Guid.NewGuid())
    {
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        RegularHours = regularHours;
        OvertimeHours = overtimeHours;
        RegularRate = regularRate;
        OvertimeRate = overtimeRate;
        GrossPay = grossPay;
        EmployeeTax = employeeTax;
        Deductions = deductions;
        EmployerTax = employerTax;
        NetPay = netPay;
        PrevailingWageRate = prevailingWageRate;
        FringeRate = fringeRate;
        TradeClassification = tradeClassification;
    }

    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal RegularHours { get; private set; }
    public decimal OvertimeHours { get; private set; }
    public decimal RegularRate { get; private set; }
    public decimal OvertimeRate { get; private set; }
    public decimal GrossPay { get; private set; }
    public decimal EmployeeTax { get; private set; }
    public decimal Deductions { get; private set; }
    public decimal EmployerTax { get; private set; }
    public decimal NetPay { get; private set; }

    /// <summary>Gets the Davis-Bacon prevailing wage rate for the line's trade (certified payroll).</summary>
    public decimal? PrevailingWageRate { get; private set; }

    /// <summary>Gets the fringe benefit rate for the line's trade (certified payroll).</summary>
    public decimal? FringeRate { get; private set; }

    /// <summary>Gets the trade/classification (certified payroll).</summary>
    public string? TradeClassification { get; private set; }

    /// <summary>Gets the total fringe cost (fringe rate × hours) for certified payroll reporting.</summary>
    public decimal FringeCost => (FringeRate ?? 0m) * (RegularHours + OvertimeHours);

    /// <summary>Gets the fully-burdened rate (prevailing base + fringe) for certified payroll reporting.</summary>
    public decimal TotalPrevailingRate => (PrevailingWageRate ?? 0m) + (FringeRate ?? 0m);
}
