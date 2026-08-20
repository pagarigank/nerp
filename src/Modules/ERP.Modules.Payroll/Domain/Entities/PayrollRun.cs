// <copyright file="PayrollRun.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// A payroll run: a batch of employee pay computations for a pay period. Starts as a draft
/// (calculated from approved timesheets + compensation), then is posted (final) which creates
/// the GL wage/expense/liability entries via the canonical posting contract.
/// </summary>
public class PayrollRun : AuditableEntity
{
    private readonly List<PayrollRunLine> _lines = [];

    protected PayrollRun() { }

    public PayrollRun(
        Guid companyId,
        Guid? calendarId,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime payDate)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        CalendarId = calendarId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PayDate = payDate;
        Status = PayrollRunStatus.Draft;
    }

    public Guid CompanyId { get; private set; }
    public Guid? CalendarId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public DateTime PayDate { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public Guid? PostedById { get; private set; }
    public DateTime? PostedOn { get; private set; }
    public string? GlBatchReference { get; private set; }

    public decimal TotalGross => _lines.Sum(l => l.GrossPay);
    public decimal TotalEmployeeTax => _lines.Sum(l => l.EmployeeTax);
    public decimal TotalDeductions => _lines.Sum(l => l.Deductions);
    public decimal TotalNet => _lines.Sum(l => l.NetPay);
    public decimal TotalEmployerTax => _lines.Sum(l => l.EmployerTax);

    public IReadOnlyCollection<PayrollRunLine> Lines => _lines.AsReadOnly();

    public PayrollRunLine AddLine(
        Guid employeeId,
        decimal regularHours,
        decimal overtimeHours,
        decimal regularRate,
        decimal overtimeRate,
        decimal grossPay,
        decimal employeeTax,
        decimal deductions,
        decimal employerTax,
        decimal netPay,
        decimal? prevailingWageRate = null,
        decimal? fringeRate = null,
        string? tradeClassification = null)
    {
        var line = new PayrollRunLine(
            Id, employeeId, regularHours, overtimeHours, regularRate, overtimeRate,
            grossPay, employeeTax, deductions, employerTax, netPay, prevailingWageRate, fringeRate, tradeClassification);
        _lines.Add(line);
        return line;
    }

    public void MarkDraftCalculated()
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Run is not in draft state.");
    }

    /// <summary>Marks the run as posted (final) and records the GL batch reference.</summary>
    public void MarkPosted(Guid postedById, string glBatchReference)
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Only a draft run can be posted.");
        Status = PayrollRunStatus.Posted;
        PostedById = postedById;
        PostedOn = DateTime.UtcNow;
        GlBatchReference = glBatchReference;
    }

    /// <summary>Reverses a previously posted run (payroll correction / re-run).</summary>
    public void Reverse()
    {
        if (Status != PayrollRunStatus.Posted)
            throw new InvalidOperationException("Only a posted run can be reversed.");
        Status = PayrollRunStatus.Reversed;
    }
}

public enum PayrollRunStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2,
}
