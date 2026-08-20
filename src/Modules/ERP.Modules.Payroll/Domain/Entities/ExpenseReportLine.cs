// <copyright file="ExpenseReportLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>One expense line. Mileage and per-diem amounts are computed server-side when provided.</summary>
public class ExpenseReportLine : AuditableEntity
{
    protected ExpenseReportLine() { }

    public ExpenseReportLine(
        Guid expenseReportId,
        ExpenseType type,
        decimal amount,
        DateTime expenseDate,
        string? description = null,
        Guid? projectId = null,
        Guid? taskId = null,
        string? glAccountNumber = null,
        bool clientBillable = false,
        decimal? mileageMiles = null,
        decimal? mileageRate = null,
        decimal? perDiemDays = null,
        decimal? perDiemRate = null)
        : base(Guid.NewGuid())
    {
        ExpenseReportId = expenseReportId;
        Type = type;
        ExpenseDate = expenseDate;
        Description = description ?? string.Empty;
        ProjectId = projectId;
        TaskId = taskId;
        GlAccountNumber = glAccountNumber;
        ClientBillable = clientBillable;

        // Server-computed amounts take precedence over a manually supplied amount.
        if (type == ExpenseType.Mileage && mileageMiles.HasValue && mileageRate.HasValue)
        {
            Amount = Math.Round(mileageMiles.Value * mileageRate.Value, 2);
            MileageMiles = mileageMiles;
            MileageRate = mileageRate;
        }
        else if (type == ExpenseType.PerDiem && perDiemDays.HasValue && perDiemRate.HasValue)
        {
            Amount = Math.Round(perDiemDays.Value * perDiemRate.Value, 2);
            PerDiemDays = perDiemDays;
            PerDiemRate = perDiemRate;
        }
        else
        {
            Amount = amount;
        }
    }

    public Guid ExpenseReportId { get; private set; }
    public ExpenseType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public string? GlAccountNumber { get; private set; }
    public bool ClientBillable { get; private set; }

    // Mileage (IRS-rate) tracking
    public decimal? MileageMiles { get; private set; }
    public decimal? MileageRate { get; private set; }

    // Per-diem (GSA-rate) tracking
    public decimal? PerDiemDays { get; private set; }
    public decimal? PerDiemRate { get; private set; }
}
