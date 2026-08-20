// <copyright file="ManualCheck.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Off-cycle manual check (bonus, termination, advance) integrated into the payroll register.</summary>
public class ManualCheck : AuditableEntity
{
    protected ManualCheck() { }

    public ManualCheck(Guid companyId, Guid employeeId, decimal amount, DateTime checkDate, string? reason = null, string? checkNumber = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        Amount = amount;
        CheckDate = checkDate;
        Reason = reason;
        CheckNumber = checkNumber;
        Status = ManualCheckStatus.Issued;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CheckDate { get; private set; }
    public string? Reason { get; private set; }
    public string? CheckNumber { get; private set; }
    public ManualCheckStatus Status { get; private set; }

    public void Void() => Status = ManualCheckStatus.Void;
}

public enum ManualCheckStatus
{
    Issued = 0,
    Void = 1,
    Cleared = 2,
}

/// <summary>A generated payroll check/stub for a run line (check printing / pay stub preview).</summary>
public class PayrollCheck : AuditableEntity
{
    protected PayrollCheck() { }

    public PayrollCheck(Guid payrollRunId, Guid employeeId, decimal netPay, string checkNumber, DateTime checkDate)
        : base(Guid.NewGuid())
    {
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        NetPay = netPay;
        CheckNumber = checkNumber;
        CheckDate = checkDate;
    }

    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal NetPay { get; private set; }
    public string CheckNumber { get; private set; } = string.Empty;
    public DateTime CheckDate { get; private set; }
    public bool IsDirectDeposit { get; private set; }
    public string? AchTraceNumber { get; private set; }

    public void SetDirectDeposit(bool isDirectDeposit, string? achTraceNumber = null)
    {
        IsDirectDeposit = isDirectDeposit;
        AchTraceNumber = achTraceNumber;
    }
}
