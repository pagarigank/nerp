// <copyright file="WageBaseLimit.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Annual wage-base / limit table used for FICA wage-base cap enforcement
/// (Social Security $ wage base, Medicare surtax threshold, FUTA $7,000 base, SUTA bases).</summary>
public class WageBaseLimit : AuditableEntity
{
    protected WageBaseLimit() { }

    public WageBaseLimit(Guid companyId, string name, WageBaseType type, int year, decimal limitAmount, decimal? surtaxThreshold = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Name = name;
        Type = type;
        Year = year;
        LimitAmount = limitAmount;
        SurtaxThreshold = surtaxThreshold;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WageBaseType Type { get; private set; }
    public int Year { get; private set; }
    public decimal LimitAmount { get; private set; }
    public decimal? SurtaxThreshold { get; private set; }
}

public enum WageBaseType
{
    SocialSecurity = 0,
    Medicare = 1,
    Futa = 2,
    Suta = 3,
}
