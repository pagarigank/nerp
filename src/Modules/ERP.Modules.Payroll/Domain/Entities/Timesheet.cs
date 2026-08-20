// <copyright file="Timesheet.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Employee timesheet: a week-ending period with daily hours recorded by project/task/pay code.
/// Approval posts the labor cost to Project Accounting (via <see cref="LaborPostedToProjectEvent"/>)
/// and to the payroll register. Supervisor approval precedes PM approval of project hours.
/// </summary>
public class Timesheet : AuditableEntity
{
    private readonly List<TimesheetLine> _lines = [];

    protected Timesheet() { }

    public Timesheet(Guid companyId, Guid employeeId, DateTime weekEnding)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        WeekEnding = weekEnding;
        Status = TimesheetStatus.Draft;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTime WeekEnding { get; private set; }
    public TimesheetStatus Status { get; private set; }
    public Guid? SupervisorId { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTime? ApprovedOn { get; private set; }
    public string? RejectionReason { get; private set; }

    /// <summary>Links the timesheet to the Phase 1 Approval Workflow request raised on submit (item #1103).</summary>
    public Guid? ApprovalRequestId { get; private set; }

    public void SetApprovalRequestId(Guid approvalRequestId) => ApprovalRequestId = approvalRequestId;
    public decimal TotalHours => _lines.Sum(l => l.Hours);
    public decimal TotalRegularHours => _lines.Where(l => !l.IsOvertime).Sum(l => l.Hours);
    public decimal TotalOvertimeHours => _lines.Where(l => l.IsOvertime).Sum(l => l.Hours);

    public IReadOnlyCollection<TimesheetLine> Lines => _lines.AsReadOnly();

    public TimesheetLine AddLine(
        Guid? projectId,
        Guid? taskId,
        Guid payCodeId,
        DateTime workDate,
        decimal hours,
        decimal rate,
        string? tradeClassification = null,
        bool isBillable = true,
        bool isOvertime = false)
    {
        if (hours <= 0)
            throw new ArgumentException("Hours must be positive.", nameof(hours));
        var line = new TimesheetLine(Id, projectId, taskId, payCodeId, workDate, hours, rate, tradeClassification, isBillable, isOvertime);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is not null)
            _lines.Remove(line);
    }

    /// <summary>Submits the timesheet for approval (cannot submit if already approved/rejected).</summary>
    public void Submit(Guid supervisorId)
    {
        if (Status == TimesheetStatus.Approved)
            throw new InvalidOperationException("Cannot submit an already-approved timesheet.");
        SupervisorId = supervisorId;
        Status = TimesheetStatus.Submitted;
        RejectionReason = null;
    }

    /// <summary>Approves the timesheet. The labor cost is posted by the controller, which
    /// dispatches <see cref="LaborPostedToProjectEvent"/> to Project Accounting + payroll register.</summary>
    /// <param name="approvedById">The supervisor/approver identifier.</param>
    public void Approve(Guid approvedById)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only submitted timesheets can be approved.");
        Status = TimesheetStatus.Approved;
        ApprovedById = approvedById;
        ApprovedOn = DateTime.UtcNow;
        RejectionReason = null;
    }

    /// <summary>Rejects the timesheet, returning it to the employee for correction.</summary>
    /// <param name="reason">The rejection reason.</param>
    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        Status = TimesheetStatus.Rejected;
        RejectionReason = reason;
    }
}

public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
}
