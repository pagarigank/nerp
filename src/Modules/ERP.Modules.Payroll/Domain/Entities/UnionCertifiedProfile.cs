// <copyright file="UnionCertifiedProfile.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Union / certified-payroll profile: prevailing wage rate + fringe benefit rate
/// for a trade/classification under Davis-Bacon (closes Phase 10 gaps 823/1001/1009).
/// A labor posting whose wage is below the prevailing rate for the trade/jurisdiction
/// is rejected by prevailing-wage validation.
/// </summary>
public class UnionCertifiedProfile : AuditableEntity
{
    protected UnionCertifiedProfile() { }

    public UnionCertifiedProfile(
        Guid companyId,
        string tradeClassification,
        string? jurisdiction = null,
        decimal prevailingWageRate = 0,
        decimal fringeBenefitRate = 0,
        string? unionLocal = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(tradeClassification))
            throw new ArgumentException("Trade classification is required.", nameof(tradeClassification));

        CompanyId = companyId;
        TradeClassification = tradeClassification;
        Jurisdiction = jurisdiction;
        PrevailingWageRate = prevailingWageRate;
        FringeBenefitRate = fringeBenefitRate;
        UnionLocal = unionLocal;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>Gets the trade/classification (e.g., "Electrician", "Laborer").</summary>
    public string TradeClassification { get; private set; } = string.Empty;

    /// <summary>Gets the jurisdiction (state/county/city) this prevailing rate applies to.</summary>
    public string? Jurisdiction { get; private set; }

    /// <summary>Gets the Davis-Bacon prevailing wage rate (base hourly wage).</summary>
    public decimal PrevailingWageRate { get; private set; }

    /// <summary>Gets the fringe benefit rate (health/welfare/pension) per hour.</summary>
    public decimal FringeBenefitRate { get; private set; }

    /// <summary>Gets the union local identifier, if applicable.</summary>
    public string? UnionLocal { get; private set; }

    public void Update(decimal? prevailingWageRate, decimal? fringeBenefitRate, string? jurisdiction, string? unionLocal)
    {
        if (prevailingWageRate.HasValue) PrevailingWageRate = prevailingWageRate.Value;
        if (fringeBenefitRate.HasValue) FringeBenefitRate = fringeBenefitRate.Value;
        if (jurisdiction is not null) Jurisdiction = jurisdiction;
        if (unionLocal is not null) UnionLocal = unionLocal;
    }

    /// <summary>Total fully-burdened prevailing rate (base wage + fringe).</summary>
    public decimal TotalPrevailingRate => PrevailingWageRate + FringeBenefitRate;

    /// <summary>Validates that an actual wage meets the prevailing rate for this trade/jurisdiction.</summary>
    /// <param name="actualWage">The actual wage being posted.</param>
    /// <returns>True when the wage meets or exceeds the prevailing rate.</returns>
    public bool MeetsPrevailingWage(decimal actualWage)
    {
        return actualWage >= PrevailingWageRate;
    }
}
