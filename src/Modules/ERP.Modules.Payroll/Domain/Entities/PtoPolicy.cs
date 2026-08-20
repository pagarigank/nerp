// <copyright file="PtoPolicy.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// PTO accrual policy: rate per hours worked (or per pay period), max accrual cap,
/// carryover limit, and cash-out rules. Employees are linked to a policy via their
/// <see cref="PtoLedger"/>.
/// </summary>
public class PtoPolicy : AuditableEntity
{
    protected PtoPolicy() { }

    public PtoPolicy(
        Guid companyId,
        string name,
        decimal accrualRate,
        string accrualBasis,
        decimal maxAccrual,
        decimal carryoverLimit,
        decimal? cashOutRate,
        bool cashOutAllowed)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Policy name is required.", nameof(name));
        CompanyId = companyId;
        Name = name;
        AccrualRate = accrualRate;
        AccrualBasis = accrualBasis;
        MaxAccrual = maxAccrual;
        CarryoverLimit = carryoverLimit;
        CashOutRate = cashOutRate;
        CashOutAllowed = cashOutAllowed;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    /// <summary>Hours accrued per unit of the accrual basis (e.g. 0.0462 per hour worked).</summary>
    public decimal AccrualRate { get; private set; }
    /// <summary>Accrual basis: 'PerHourWorked' or 'PerPayPeriod'.</summary>
    public string AccrualBasis { get; private set; } = string.Empty;
    public decimal MaxAccrual { get; private set; }
    public decimal CarryoverLimit { get; private set; }
    public decimal? CashOutRate { get; private set; }
    public bool CashOutAllowed { get; private set; }
}
