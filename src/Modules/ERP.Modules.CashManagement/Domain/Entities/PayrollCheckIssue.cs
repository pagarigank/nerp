// <copyright file="PayrollCheckIssue.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

/// <summary>
/// Records a payroll check / direct-deposit issued by a Payroll run so Cash Management can
/// reconcile it against the bank statement. Populated by the <see cref="ERP.Core.Domain.Events.PayrollPostedEvent"/>
/// consumer (Phase 11 item #1102). Direct-deposit entries carry only the masked account tail.
/// </summary>
public class PayrollCheckIssue : AuditableAggregateRoot
{
    protected PayrollCheckIssue() { }

    public PayrollCheckIssue(
        Guid companyId,
        Guid payrollRunId,
        Guid employeeId,
        string paymentMethod,
        string? checkNumber,
        decimal amount,
        DateTime issuedOn,
        string? bankAccountLast4)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        PaymentMethod = paymentMethod;
        CheckNumber = checkNumber ?? string.Empty;
        Amount = amount;
        IssuedOn = issuedOn;
        BankAccountLast4 = bankAccountLast4 ?? string.Empty;
        IsReconciled = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string CheckNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime IssuedOn { get; private set; }
    public string BankAccountLast4 { get; private set; } = string.Empty;
    public bool IsReconciled { get; private set; }

    public void MarkReconciled()
    {
        IsReconciled = true;
    }
}
