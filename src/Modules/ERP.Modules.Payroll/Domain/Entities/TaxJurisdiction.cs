// <copyright file="TaxJurisdiction.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Level of a taxing jurisdiction (drives which tax tables apply).</summary>
public enum TaxJurisdictionLevel
{
    Federal = 0,
    State = 1,
    County = 2,
    City = 3,
    SchoolDistrict = 4
}

/// <summary>
/// A taxing jurisdiction (state, county, city, school district) with special
/// withholding rules such as reciprocal agreements (employee lives in one state,
/// works in another) and local surtaxes.
/// </summary>
public class TaxJurisdiction : AuditableEntity
{
    protected TaxJurisdiction() { }

    public TaxJurisdiction(
        Guid companyId,
        string code,
        string name,
        TaxJurisdictionLevel level,
        string? stateCode,
        bool hasReciprocalAgreement,
        string? reciprocalWithState,
        decimal? localRate,
        int? filingFrequency)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Jurisdiction code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Jurisdiction name is required.", nameof(name));

        CompanyId = companyId;
        Code = code;
        Name = name;
        Level = level;
        StateCode = stateCode;
        HasReciprocalAgreement = hasReciprocalAgreement;
        ReciprocalWithState = reciprocalWithState;
        LocalRate = localRate;
        FilingFrequency = filingFrequency;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public TaxJurisdictionLevel Level { get; private set; }
    public string? StateCode { get; private set; }
    public bool HasReciprocalAgreement { get; private set; }
    public string? ReciprocalWithState { get; private set; }
    public decimal? LocalRate { get; private set; }
    public int? FilingFrequency { get; private set; }
}
