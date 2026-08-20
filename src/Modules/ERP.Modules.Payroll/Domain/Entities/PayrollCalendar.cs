// <copyright file="PayrollCalendar.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Payroll calendar: defines pay frequency and the recurring pay periods for a company.</summary>
public class PayrollCalendar : AuditableEntity
{
    protected PayrollCalendar() { }

    public PayrollCalendar(Guid companyId, string name, PayrollFrequency frequency, DateTime startDate)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Calendar name is required.", nameof(name));
        CompanyId = companyId;
        Name = name;
        Frequency = frequency;
        StartDate = startDate;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PayrollFrequency Frequency { get; private set; }
    public DateTime StartDate { get; private set; }

    /// <summary>Gets or sets the employer FICA rate (default 7.65% = OASDI 6.2% + Medicare 1.45%).</summary>
    public decimal EmployerFicaRate { get; set; } = 0.0765m;

    /// <summary>Gets or sets the employee FICA rate (default 7.65%).</summary>
    public decimal EmployeeFicaRate { get; set; } = 0.0765m;

    /// <summary>Gets or sets the federal unemployment rate (default 0.6%).</summary>
    public decimal FutaRate { get; set; } = 0.006m;

    /// <summary>Gets or sets the state unemployment rate (default 3.4%).</summary>
    public decimal SutaRate { get; set; } = 0.034m;

    public void Update(string? name, PayrollFrequency? frequency)
    {
        if (name is not null) Name = name;
        if (frequency.HasValue) Frequency = frequency.Value;
    }
}

public enum PayrollFrequency
{
    Weekly = 0,
    BiWeekly = 1,
    SemiMonthly = 2,
    Monthly = 3,
}
