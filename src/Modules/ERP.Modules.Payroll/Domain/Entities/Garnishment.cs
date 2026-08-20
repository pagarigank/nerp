// <copyright file="Garnishment.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// A court-ordered wage garnishment against an employee. Ordered by CCPA priority:
/// child support &gt; federal/state tax levy &gt; student loan &gt; creditor. Disposable-income
/// limits cap the deductible amount (child support 50-65%; other orders 25%).
/// </summary>
public class Garnishment : AuditableEntity
{
    protected Garnishment() { }

    public Garnishment(
        Guid companyId,
        Guid employeeId,
        GarnishmentType type,
        decimal disposableIncomePercent,
        decimal? fixedAmount = null,
        int? arrearsWeeks = null,
        string? caseNumber = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        Type = type;
        DisposableIncomePercent = disposableIncomePercent;
        FixedAmount = fixedAmount;
        ArrearsWeeks = arrearsWeeks;
        CaseNumber = caseNumber;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public GarnishmentType Type { get; private set; }

    /// <summary>Gets the percentage of disposable income to withhold (CCPA table).</summary>
    public decimal DisposableIncomePercent { get; private set; }

    /// <summary>Gets an optional fixed-dollar amount to withhold each period (e.g. support arrears).</summary>
    public decimal? FixedAmount { get; private set; }

    /// <summary>Gets the weeks of support arrears (raises the child-support cap to 55/65%).</summary>
    public int? ArrearsWeeks { get; private set; }

    public string? CaseNumber { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? TerminatedOn { get; private set; }

    /// <summary>Gets the CCPA priority (lower number = higher priority). Child support wins.</summary>
    public int Priority => Type switch
    {
        GarnishmentType.ChildSupport => 1,
        GarnishmentType.FederalTaxLevy => 2,
        GarnishmentType.StateTaxLevy => 3,
        GarnishmentType.StudentLoan => 4,
        GarnishmentType.Creditor => 5,
        _ => 9,
    };

    public void Terminate()
    {
        IsActive = false;
        TerminatedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Computes the allowable garnishment amount for this order given disposable income,
    /// respecting the CCPA percentage cap (aggregate across lower-priority orders is applied
    /// by the engine, not here).
    /// </summary>
    /// <param name="disposableIncome">Disposable (after-tax) income for the period.</param>
    /// <returns>The amount deductible under this order's individual cap.</returns>
    public decimal ComputeAllowedAmount(decimal disposableIncome)
    {
        if (!IsActive || disposableIncome <= 0m)
            return 0m;
        if (FixedAmount.HasValue)
            return Math.Min(FixedAmount.Value, disposableIncome);
        var cap = disposableIncome * (DisposableIncomePercent / 100m);
        return Math.Min(cap, disposableIncome);
    }

    /// <summary>Gets the CCPA disposable-income cap fraction for this order type.</summary>
    /// <remarks>
    /// Child support is capped at 50% of disposable income (55% when 12 or more weeks of
    /// support are in arrears). All other orders (tax levies, student loans, creditors) are
    /// capped at 25% of disposable income under Title III.
    /// </remarks>
    public decimal CcpaCapFraction()
    {
        if (Type == GarnishmentType.ChildSupport)
        {
            var basePct = ArrearsWeeks >= 12 ? 0.55m : 0.50m;
            // Higher bands apply when the employee supports a second family (not modeled here).
            // Default to the 55 or 50 percent band unless explicitly raised via DisposableIncomePercent.
            return DisposableIncomePercent / 100m == 0 ? basePct : DisposableIncomePercent / 100m;
        }

        // All non-support orders are capped at 25% of disposable income (Title III).
        return 0.25m;
    }
}

public enum GarnishmentType
{
    ChildSupport = 0,
    FederalTaxLevy = 1,
    StateTaxLevy = 2,
    StudentLoan = 3,
    Creditor = 4,
}
