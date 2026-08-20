// <copyright file="AchReturn.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Bank ACH return (R01-R16) for a failed direct-deposit. Captures the return code,
/// action (reissue / notify / reverse), and processing state so the ACH-return
/// monitoring job can act without double-posting.
/// </summary>
public class AchReturn : AuditableEntity
{
    protected AchReturn() { }

    public AchReturn(
        Guid companyId,
        Guid? payrollRunId,
        Guid? employeeId,
        string traceNumber,
        string returnCode,
        string description,
        decimal amount,
        string action)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(returnCode)) throw new ArgumentException("Return code is required.", nameof(returnCode));
        CompanyId = companyId;
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        TraceNumber = traceNumber;
        ReturnCode = returnCode;
        Description = description;
        Amount = amount;
        ReturnAction = action;
        Processed = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid? PayrollRunId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public string TraceNumber { get; private set; } = string.Empty;
    public string ReturnCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string ReturnAction { get; private set; } = string.Empty;
    public bool Processed { get; private set; }

    public void MarkProcessed()
    {
        Processed = true;
    }
}
