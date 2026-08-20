// <copyright file="DeductionBenefit.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Deduction / benefit master (pre-tax 401k/HSA/health, post-tax Roth/life/garnishment).</summary>
public class DeductionBenefit : AuditableEntity
{
    protected DeductionBenefit() { }

    public DeductionBenefit(Guid companyId, string code, string description, DeductionBenefitType type, bool isPreTax, decimal? defaultRate = null, string? glAccountNumber = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Code = code;
        Description = description;
        Type = type;
        IsPreTax = isPreTax;
        DefaultRate = defaultRate;
        GlAccountNumber = glAccountNumber;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DeductionBenefitType Type { get; private set; }
    public bool IsPreTax { get; private set; }
    public decimal? DefaultRate { get; private set; }
    public string? GlAccountNumber { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string description, bool isPreTax, decimal? defaultRate)
    {
        Description = description;
        IsPreTax = isPreTax;
        DefaultRate = defaultRate;
    }

    public void Deactivate() => IsActive = false;
}

public enum DeductionBenefitType
{
    PreTaxDeduction = 0,
    PostTaxDeduction = 1,
    Benefit = 2,
}

/// <summary>Employee-specific enrollment in a deduction/benefit plan.</summary>
public class EmployeeDeductionBenefit : AuditableEntity
{
    protected EmployeeDeductionBenefit() { }

    public EmployeeDeductionBenefit(Guid employeeId, Guid deductionBenefitId, decimal amount, decimal? percent = null, DateTime? startDate = null, DateTime? endDate = null)
        : base(Guid.NewGuid())
    {
        EmployeeId = employeeId;
        DeductionBenefitId = deductionBenefitId;
        Amount = amount;
        Percent = percent;
        StartDate = startDate;
        EndDate = endDate;
    }

    public Guid EmployeeId { get; private set; }
    public Guid DeductionBenefitId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? Percent { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;
}
