// <copyright file="HolidayCalendar.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// A single working-day / holiday entry used by payroll pay-date calculation, PO
/// delivery alerts, AR dunning schedules and other date math that must skip
/// non-working days. Company-scoped; a date that is not present is treated as a
/// working day by default.
/// </summary>
public class HolidayCalendar : AuditableAggregateRoot
{
    protected HolidayCalendar() { }

    public HolidayCalendar(
        Guid companyId,
        DateOnly date,
        string description,
        bool isWorkingDay = false) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Date = date;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsWorkingDay = isWorkingDay;
    }

    public Guid CompanyId { get; private set; }

    public DateOnly Date { get; private set; }

    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// True when this date is an explicitly-working day (e.g. a weekend made up for a
    /// holiday). False means the date is a holiday / non-working day. Absent dates
    /// are working days.
    /// </summary>
    public bool IsWorkingDay { get; private set; }

    public void Update(string description, bool isWorkingDay)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsWorkingDay = isWorkingDay;
    }
}
