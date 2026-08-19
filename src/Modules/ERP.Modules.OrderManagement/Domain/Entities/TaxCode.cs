// <copyright file="TaxCode.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// A tax code / rate master keyed by jurisdiction, with an effective-dated rate
/// and a taxable flag. The tax engine selects the active rate for (jurisdiction, date).
/// </summary>
public class TaxCode : AuditableEntity
{
    private TaxCode() { }

    public TaxCode(
        Guid companyId,
        string code,
        string description,
        string jurisdiction,
        decimal rate,
        bool isTaxable,
        DateTime? effectiveFrom,
        DateTime? effectiveTo)
    {
        CompanyId = companyId;
        Code = code;
        Description = description;
        Jurisdiction = jurisdiction;
        Rate = rate;
        IsTaxable = isTaxable;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Jurisdiction { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public bool IsTaxable { get; private set; } = true;
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string description, decimal rate, bool isTaxable, DateTime? effectiveFrom, DateTime? effectiveTo, bool isActive)
    {
        Description = description;
        Rate = rate;
        IsTaxable = isTaxable;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = isActive;
    }

    public bool IsEffectiveOn(DateTime asOf) =>
        IsActive &&
        (!EffectiveFrom.HasValue || EffectiveFrom.Value <= asOf) &&
        (!EffectiveTo.HasValue || EffectiveTo.Value >= asOf);
}
