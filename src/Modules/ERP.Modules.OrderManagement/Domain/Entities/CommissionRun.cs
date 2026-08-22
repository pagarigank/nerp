// <copyright file="CommissionRun.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Weekly sales-commission run. The commission job computes the previous ISO
/// week's shipped-line revenue per sales rep, snapshots each rep's commission
/// rate, and persists one header plus one line per rep. The unique
/// (PeriodStart, SalesRepId) index on the line makes a run idempotent: the same
/// period can never be paid twice.
/// </summary>
public class CommissionRun : AuditableAggregateRoot
{
    private readonly List<CommissionRunLine> _lines = [];

    protected CommissionRun() { }

    public CommissionRun(string runNumber, DateTime periodStart, DateTime periodEnd)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(runNumber))
            throw new ArgumentException("Run number is required.", nameof(runNumber));

        RunNumber = runNumber;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }

    public string RunNumber { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public decimal TotalRevenue => _lines.Sum(l => l.RevenueBase);
    public decimal TotalCommission => _lines.Sum(l => l.CommissionAmount);

    public IReadOnlyCollection<CommissionRunLine> Lines => _lines.AsReadOnly();

    public void AddLine(CommissionRunLine line)
    {
        if (_lines.Any(l => l.SalesRepId == line.SalesRepId))
            throw new InvalidOperationException($"Rep {line.SalesRepCode} already has a commission line on this run.");
        _lines.Add(line);
    }
}

public class CommissionRunLine : AuditableEntity
{
    protected CommissionRunLine() { }

    public CommissionRunLine(
        Guid commissionRunId,
        Guid salesRepId,
        string salesRepCode,
        DateTime periodStart,
        DateTime periodEnd,
        decimal revenueBase,
        decimal commissionRate)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(salesRepCode))
            throw new ArgumentException("Sales rep code is required.", nameof(salesRepCode));
        if (revenueBase < 0)
            throw new ArgumentException("Revenue base cannot be negative.", nameof(revenueBase));

        CommissionRunId = commissionRunId;
        SalesRepId = salesRepId;
        SalesRepCode = salesRepCode;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        RevenueBase = revenueBase;
        CommissionRate = commissionRate;
        CommissionAmount = decimal.Round(revenueBase * (commissionRate / 100m), 2);
    }

    public Guid CommissionRunId { get; private set; }
    public Guid SalesRepId { get; private set; }
    public string SalesRepCode { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }

    /// <summary>Shipped-line revenue for the period the commission is computed on.</summary>
    public decimal RevenueBase { get; private set; }

    /// <summary>Rep commission rate snapshot at run time (percent).</summary>
    public decimal CommissionRate { get; private set; }
    public decimal CommissionAmount { get; private set; }
}
