// <copyright file="ExpenseReport.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Employee expense report (mileage, meals, lodging, other). Lines may be billable to a
/// project/task; reimbursement posts an AP liability (via GL) and, when billable, a project cost.
/// </summary>
public class ExpenseReport : AuditableEntity
{
    private readonly List<ExpenseReportLine> _lines = [];

    protected ExpenseReport() { }

    public ExpenseReport(Guid companyId, Guid employeeId, DateTime reportDate, string? description = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        ReportDate = reportDate;
        Description = description ?? string.Empty;
        Status = ExpenseReportStatus.Draft;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTime ReportDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ExpenseReportStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTime? ReimbursedAt { get; private set; }
    public decimal TotalAmount => _lines.Sum(l => l.Amount);

    public IReadOnlyCollection<ExpenseReportLine> Lines => _lines.AsReadOnly();

    public ExpenseReportLine AddLine(
        ExpenseType type, decimal amount, DateTime expenseDate, string? description = null,
        Guid? projectId = null, Guid? taskId = null, string? glAccountNumber = null,
        bool clientBillable = false, decimal? mileageMiles = null, decimal? mileageRate = null,
        decimal? perDiemDays = null, decimal? perDiemRate = null)
    {
        if (Status != ExpenseReportStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a non-draft expense report.");
        var line = new ExpenseReportLine(Id, type, amount, expenseDate, description, projectId, taskId, glAccountNumber, clientBillable, mileageMiles, mileageRate, perDiemDays, perDiemRate);
        _lines.Add(line);
        return line;
    }

    public void Submit()
    {
        if (Status != ExpenseReportStatus.Draft)
            throw new InvalidOperationException("Only a draft report can be submitted.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit an empty expense report.");
        Status = ExpenseReportStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    public void Approve(Guid approvedById)
    {
        if (Status != ExpenseReportStatus.Submitted)
            throw new InvalidOperationException("Only a submitted report can be approved.");
        Status = ExpenseReportStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedById = approvedById;
    }

    public void Reject(string reason)
    {
        if (Status is ExpenseReportStatus.Approved or ExpenseReportStatus.Reimbursed)
            throw new InvalidOperationException("Cannot reject an approved/reimbursed report.");
        Status = ExpenseReportStatus.Rejected;
        RejectionReason = reason;
    }

    public void MarkReimbursed()
    {
        if (Status != ExpenseReportStatus.Approved)
            throw new InvalidOperationException("Only an approved report can be reimbursed.");
        Status = ExpenseReportStatus.Reimbursed;
        ReimbursedAt = DateTime.UtcNow;
    }

    public string? RejectionReason { get; private set; }
}

public enum ExpenseReportStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Reimbursed = 4,
}

public enum ExpenseType
{
    Mileage = 0,
    Meals = 1,
    Lodging = 2,
    Other = 3,
    PerDiem = 4,
}
