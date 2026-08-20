// <copyright file="TimesheetLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>A single timesheet line: hours worked by project/task/pay code on a given date.</summary>
public class TimesheetLine : AuditableEntity
{
    protected TimesheetLine() { }

    public TimesheetLine(
        Guid timesheetId,
        Guid? projectId,
        Guid? taskId,
        Guid payCodeId,
        DateTime workDate,
        decimal hours,
        decimal rate,
        string? tradeClassification = null,
        bool isBillable = true,
        bool isOvertime = false)
        : base(Guid.NewGuid())
    {
        TimesheetId = timesheetId;
        ProjectId = projectId;
        TaskId = taskId;
        PayCodeId = payCodeId;
        WorkDate = workDate;
        Hours = hours;
        Rate = rate;
        TradeClassification = tradeClassification;
        IsBillable = isBillable;
        IsOvertime = isOvertime;
    }

    public Guid TimesheetId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid PayCodeId { get; private set; }
    public DateTime WorkDate { get; private set; }
    public decimal Hours { get; private set; }
    public decimal Rate { get; private set; }

    /// <summary>Gets the trade/classification used for prevailing-wage validation (Davis-Bacon).</summary>
    public string? TradeClassification { get; private set; }

    public bool IsBillable { get; private set; }
    public bool IsOvertime { get; private set; }

    /// <summary>Gets the gross amount for this line (hours × rate).</summary>
    public decimal Amount => Hours * Rate;

    public void Update(decimal? hours, decimal? rate, Guid? projectId, Guid? taskId, bool? isBillable, bool? isOvertime)
    {
        if (hours.HasValue) Hours = hours.Value;
        if (rate.HasValue) Rate = rate.Value;
        if (projectId.HasValue) ProjectId = projectId.Value;
        if (taskId.HasValue) TaskId = taskId.Value;
        if (isBillable.HasValue) IsBillable = isBillable.Value;
        if (isOvertime.HasValue) IsOvertime = isOvertime.Value;
    }
}
