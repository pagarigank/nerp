// <copyright file="PayrollDomainEvents.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Events;

/// <summary>
/// Raised when a Payroll timesheet is approved (architecture.md §4). Carries the
/// flattened labor lines so downstream consumers (Project Accounting job-costing,
/// GL) can react without a module reference cycle. The Project Accounting handler
/// (<c>PayrollPostedToProjectHandler</c>) also consumes <see cref="LaborPostedToProjectEvent"/>,
/// which remains the detailed labor-posting payload; this event is the architecture-named
/// lifecycle signal consumed by integration/audit/EDI subscribers.
/// </summary>
public record TimesheetApprovedEvent : ERP.Core.Domain.Common.DomainEvent
{
    public TimesheetApprovedEvent(
        Guid timesheetId,
        Guid companyId,
        Guid employeeId,
        DateTime weekEnding,
        IReadOnlyList<LaborPostingLine> lines)
    {
        TimesheetId = timesheetId;
        CompanyId = companyId;
        EmployeeId = employeeId;
        WeekEnding = weekEnding;
        Lines = lines;
    }

    public Guid TimesheetId { get; }
    public Guid CompanyId { get; }
    public Guid EmployeeId { get; }
    public DateTime WeekEnding { get; }
    public IReadOnlyList<LaborPostingLine> Lines { get; }

    public override string EventType => "TimesheetApproved";
}

/// <summary>
/// Raised when a final Payroll run is posted (architecture.md §4). Carries run
/// totals and the issued pay instruments so Cash Management can reconcile the
/// pay run and Banking/positive-pay can pick up the issued checks/direct deposits.
/// </summary>
public record PayrollPostedEvent : ERP.Core.Domain.Common.DomainEvent
{
    public PayrollPostedEvent(
        Guid payrollRunId,
        Guid companyId,
        DateTime payDate,
        decimal totalGross,
        decimal totalNet,
        decimal totalEmployeeTax,
        decimal totalEmployerTax,
        string glBatchNumber,
        IReadOnlyList<PayrollPostedPayment> payments)
    {
        PayrollRunId = payrollRunId;
        CompanyId = companyId;
        PayDate = payDate;
        TotalGross = totalGross;
        TotalNet = totalNet;
        TotalEmployeeTax = totalEmployeeTax;
        TotalEmployerTax = totalEmployerTax;
        GlBatchNumber = glBatchNumber;
        Payments = payments;
    }

    public Guid PayrollRunId { get; }
    public Guid CompanyId { get; }
    public DateTime PayDate { get; }
    public decimal TotalGross { get; }
    public decimal TotalNet { get; }
    public decimal TotalEmployeeTax { get; }
    public decimal TotalEmployerTax { get; }
    public string GlBatchNumber { get; }
    public IReadOnlyList<PayrollPostedPayment> Payments { get; }

    public override string EventType => "PayrollPosted";
}

/// <summary>A single pay instrument (check or direct deposit) issued by a payroll run.</summary>
public sealed record PayrollPostedPayment(
    Guid EmployeeId,
    string PaymentMethod, // "Check" | "DirectDeposit"
    string? CheckNumber,
    decimal Amount,
    string? BankAccountLast4);
