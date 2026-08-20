// <copyright file="ServiceRateCard.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public class ServiceRateCard : AuditableEntity
{
    protected ServiceRateCard()
    {
    }

    public ServiceRateCard(
        Guid companyId,
        string name,
        DateTime effectiveDate,
        DateTime? expirationDate,
        bool isActive,
        decimal laborRatePerHour,
        decimal overtimeRatePerHour,
        decimal tripCharge,
        decimal partsMarkupPercent)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        IsActive = isActive;
        LaborRatePerHour = laborRatePerHour;
        OvertimeRatePerHour = overtimeRatePerHour;
        TripCharge = tripCharge;
        PartsMarkupPercent = partsMarkupPercent;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal LaborRatePerHour { get; private set; }
    public decimal OvertimeRatePerHour { get; private set; }
    public decimal TripCharge { get; private set; }
    public decimal PartsMarkupPercent { get; private set; }
}
