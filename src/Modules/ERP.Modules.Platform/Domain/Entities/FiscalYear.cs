// <copyright file="FiscalYear.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class FiscalYear : AuditableAggregateRoot
{
    protected FiscalYear() { }

    public Guid CompanyId { get; private set; }

    public int Year { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset StartDate { get; private set; }

    public DateTimeOffset EndDate { get; private set; }

    public bool IsClosed { get; private set; }

    /// <summary>Calendar pattern used to generate periods (12-month, 13-period, 4-4-5).</summary>
    public FiscalCalendarType CalendarType { get; private set; } = FiscalCalendarType.Standard;

    /// <summary>Whether the year end is a fixed calendar date or a floating business rule.</summary>
    public FiscalYearEndType YearEndType { get; private set; } = FiscalYearEndType.Calendar;

    public FiscalYear(
        Guid companyId,
        int year,
        string description,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        FiscalCalendarType calendarType = FiscalCalendarType.Standard,
        FiscalYearEndType yearEndType = FiscalYearEndType.Calendar) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Year = year;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        StartDate = startDate;
        EndDate = endDate;
        IsClosed = false;
        CalendarType = calendarType;
        YearEndType = yearEndType;
    }

    public void Update(string description, DateTimeOffset startDate, DateTimeOffset endDate, FiscalCalendarType calendarType, FiscalYearEndType yearEndType)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        StartDate = startDate;
        EndDate = endDate;
        CalendarType = calendarType;
        YearEndType = yearEndType;
    }

    public void Close()
    {
        IsClosed = true;
    }

    public void Reopen()
    {
        IsClosed = false;
    }
}
