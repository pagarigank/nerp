// <copyright file="CompanyPayrollSetup.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Company-level payroll configuration: federal/state tax IDs, EFTPS, deposit
/// schedules (monthly vs semi-weekly per IRS lookback), wage-base defaults, and
/// default GL accounts used when payroll posts.
/// </summary>
public class CompanyPayrollSetup : AuditableEntity
{
    protected CompanyPayrollSetup() { }

    public CompanyPayrollSetup(
        Guid companyId,
        string ein,
        string federalTaxId,
        string? stateTaxId,
        string? sutAState,
        string eftpsPin,
        string depositSchedule,
        decimal socialSecurityRate,
        decimal medicareRate,
        decimal futaRate,
        decimal sutARate,
        Guid wageExpenseAccountId,
        Guid payrollTaxExpenseAccountId,
        Guid payrollLiabilityAccountId,
        Guid clearingAccountId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(ein)) throw new ArgumentException("EIN is required.", nameof(ein));
        CompanyId = companyId;
        Ein = ein;
        FederalTaxId = federalTaxId;
        StateTaxId = stateTaxId;
        SutaState = sutAState;
        EftpsPin = eftpsPin;
        DepositSchedule = depositSchedule;
        SocialSecurityRate = socialSecurityRate;
        MedicareRate = medicareRate;
        FutaRate = futaRate;
        SutaRate = sutARate;
        WageExpenseAccountId = wageExpenseAccountId;
        PayrollTaxExpenseAccountId = payrollTaxExpenseAccountId;
        PayrollLiabilityAccountId = payrollLiabilityAccountId;
        ClearingAccountId = clearingAccountId;
    }

    public Guid CompanyId { get; private set; }
    public string Ein { get; private set; } = string.Empty;
    public string FederalTaxId { get; private set; } = string.Empty;
    public string? StateTaxId { get; private set; }
    public string? SutaState { get; private set; }
    public string EftpsPin { get; private set; } = string.Empty;
    public string DepositSchedule { get; private set; } = string.Empty;
    public decimal SocialSecurityRate { get; private set; }
    public decimal MedicareRate { get; private set; }
    public decimal FutaRate { get; private set; }
    public decimal SutaRate { get; private set; }
    public Guid WageExpenseAccountId { get; private set; }
    public Guid PayrollTaxExpenseAccountId { get; private set; }
    public Guid PayrollLiabilityAccountId { get; private set; }
    public Guid ClearingAccountId { get; private set; }

    public void Update(
        string? stateTaxId, string? sutAState, string eftpsPin, string depositSchedule,
        decimal socialSecurityRate, decimal medicareRate, decimal futaRate, decimal sutARate)
    {
        if (stateTaxId is not null) StateTaxId = stateTaxId;
        if (sutAState is not null) SutaState = sutAState;
        EftpsPin = eftpsPin;
        DepositSchedule = depositSchedule;
        SocialSecurityRate = socialSecurityRate;
        MedicareRate = medicareRate;
        FutaRate = futaRate;
        SutaRate = sutARate;
    }
}
