// <copyright file="FiscalPeriod.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class FiscalPeriod : AuditableAggregateRoot
{
    protected FiscalPeriod() { }

    public FiscalPeriod(
        Guid fiscalYearId,
        Guid companyId,
        int periodNumber,
        string description,
        DateTimeOffset startDate,
        DateTimeOffset endDate) : base(Guid.NewGuid())
    {
        FiscalYearId = fiscalYearId;
        CompanyId = companyId;
        PeriodNumber = periodNumber;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        StartDate = startDate;
        EndDate = endDate;
        Status = PeriodStatus.Open;
    }

    public Guid FiscalYearId { get; private set; }

    public Guid CompanyId { get; private set; }

    public int PeriodNumber { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset StartDate { get; private set; }

    public DateTimeOffset EndDate { get; private set; }

    public PeriodStatus Status { get; private set; }

    public void Close()
    {
        Status = PeriodStatus.Closed;
    }

    public void Open()
    {
        Status = PeriodStatus.Open;
    }

    public void Lock()
    {
        Status = PeriodStatus.Locked;
    }
}

public enum PeriodStatus
{
    Open = 0,
    Closed = 1,
    Locked = 2
}
