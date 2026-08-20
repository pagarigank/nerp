// <copyright file="LaborPostedToProjectEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Events;

/// <summary>
/// Raised when a Payroll timesheet is approved. Consumed by Project Accounting
/// (PayrollPostedToProjectHandler) to post the labor cost to the project ledger
/// and dual-post to GL, closing the Phase 10 / Phase 11 wiring dependency.
/// Lives in ERP.Core so both Payroll (publisher) and Project Accounting (consumer)
/// can reference it without creating a module cycle.
/// </summary>
public record LaborPostedToProjectEvent : ERP.Core.Domain.Common.DomainEvent
{
    public LaborPostedToProjectEvent(
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
    public override string EventType
    {
        get => "LaborPostedToProject";
    }
}

/// <summary>A flattened timesheet line for cross-module labor posting.</summary>
public sealed record LaborPostingLine
{
    public LaborPostingLine(
        Guid? projectId,
        Guid? taskId,
        Guid payCodeId,
        DateTime workDate,
        decimal hours,
        decimal rate,
        decimal amount,
        string? tradeClassification,
        bool isBillable,
        bool isOvertime)
    {
        ProjectId = projectId;
        TaskId = taskId;
        PayCodeId = payCodeId;
        WorkDate = workDate;
        Hours = hours;
        Rate = rate;
        Amount = amount;
        TradeClassification = tradeClassification;
        IsBillable = isBillable;
        IsOvertime = isOvertime;
    }

    public Guid? ProjectId { get; }
    public Guid? TaskId { get; }
    public Guid PayCodeId { get; }
    public DateTime WorkDate { get; }
    public decimal Hours { get; }
    public decimal Rate { get; }
    public decimal Amount { get; }
    public string? TradeClassification { get; }
    public bool IsBillable { get; }
    public bool IsOvertime { get; }
}
