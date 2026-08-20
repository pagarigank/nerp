// <copyright file="PtoLedger.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Employee leave/PTO balance ledger: accrued, used, available, carryover, payout.</summary>
public class PtoLedger : AuditableEntity
{
    private readonly List<PtoTransaction> _transactions = [];

    protected PtoLedger() { }

    public PtoLedger(Guid employeeId, string policyName, decimal accrualRate, decimal maxAccrual, decimal carryoverLimit)
        : base(Guid.NewGuid())
    {
        EmployeeId = employeeId;
        PolicyName = policyName;
        AccrualRate = accrualRate;
        MaxAccrual = maxAccrual;
        CarryoverLimit = carryoverLimit;
        Accrued = 0m;
        Used = 0m;
    }

    public Guid EmployeeId { get; private set; }
    public string PolicyName { get; private set; } = string.Empty;
    public decimal AccrualRate { get; private set; }
    public decimal MaxAccrual { get; private set; }
    public decimal CarryoverLimit { get; private set; }
    public decimal Accrued { get; private set; }
    public decimal Used { get; private set; }
    public decimal Available => Math.Min(Accrued - Used, MaxAccrual);
    public decimal Carryover => Math.Max(0m, Available - CarryoverLimit);

    public IReadOnlyCollection<PtoTransaction> Transactions => _transactions.AsReadOnly();

    public void Accrue(decimal hours, DateTime asOf)
    {
        var projected = Accrued + hours;
        Accrued = projected > MaxAccrual ? MaxAccrual : projected;
        _transactions.Add(new PtoTransaction(Id, PtoTransactionType.Accrual, hours, asOf));
    }

    public void Use(decimal hours, DateTime asOf)
    {
        if (hours > Available)
            throw new InvalidOperationException("Insufficient PTO available.");
        Used += hours;
        _transactions.Add(new PtoTransaction(Id, PtoTransactionType.Usage, hours, asOf));
    }

    public decimal PayoutValue(decimal payRate) => Carryover * payRate;
}

public class PtoTransaction : AuditableEntity
{
    protected PtoTransaction() { }

    public PtoTransaction(Guid ptoLedgerId, PtoTransactionType type, decimal hours, DateTime asOf)
        : base(Guid.NewGuid())
    {
        PtoLedgerId = ptoLedgerId;
        Type = type;
        Hours = hours;
        AsOf = asOf;
    }

    public Guid PtoLedgerId { get; private set; }
    public PtoTransactionType Type { get; private set; }
    public decimal Hours { get; private set; }
    public DateTime AsOf { get; private set; }
}

public enum PtoTransactionType
{
    Accrual = 0,
    Usage = 1,
    Carryover = 2,
    Payout = 3,
}
