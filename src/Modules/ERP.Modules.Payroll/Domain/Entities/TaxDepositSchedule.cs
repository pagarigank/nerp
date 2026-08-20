// <copyright file="TaxDepositSchedule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Scheduled federal/state payroll tax deposit (EFTPS). Per IRS, deposits are
/// monthly or semi-weekly based on the lookback period; this entity tracks the
/// scheduled deposit date, the tax type (941/FUTA/SUTA/state), the estimated
/// amount, and the actual deposited amount/date so the deposit-reminder job can alert.
/// </summary>
public class TaxDepositSchedule : AuditableEntity
{
    protected TaxDepositSchedule() { }

    public TaxDepositSchedule(
        Guid companyId,
        string taxType,
        string agency,
        Guid? payrollRunId,
        DateTime depositDate,
        decimal estimatedAmount,
        string frequency,
        string? formType)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(taxType)) throw new ArgumentException("Tax type is required.", nameof(taxType));
        CompanyId = companyId;
        TaxType = taxType;
        Agency = agency;
        PayrollRunId = payrollRunId;
        DepositDate = depositDate;
        EstimatedAmount = estimatedAmount;
        Frequency = frequency;
        FormType = formType;
        Deposited = false;
    }

    public Guid CompanyId { get; private set; }
    public string TaxType { get; private set; } = string.Empty;
    public string Agency { get; private set; } = string.Empty;
    public Guid? PayrollRunId { get; private set; }
    public DateTime DepositDate { get; private set; }
    public decimal EstimatedAmount { get; private set; }
    public decimal? DepositedAmount { get; private set; }
    public DateTime? DepositedOn { get; private set; }
    public string Frequency { get; private set; } = string.Empty;
    public string? FormType { get; private set; }
    public bool Deposited { get; private set; }

    public void MarkDeposited(decimal depositedAmount, DateTime depositedOn)
    {
        DepositedAmount = depositedAmount;
        DepositedOn = depositedOn;
        Deposited = true;
    }
}
