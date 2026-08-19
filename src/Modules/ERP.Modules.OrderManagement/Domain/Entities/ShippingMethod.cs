// <copyright file="ShippingMethod.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class ShippingMethod : AuditableEntity
{
    private ShippingMethod() { }

#pragma warning disable CA1054
    public ShippingMethod(Guid companyId, string code, string description, string? carrier, decimal baseCost, string? trackingUrlTemplate)
#pragma warning restore CA1054
    {
        CompanyId = companyId;
        Code = code;
        Description = description;
        Carrier = carrier;
        BaseCost = baseCost;
        TrackingUrlTemplate = trackingUrlTemplate;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Carrier { get; private set; }
    public decimal BaseCost { get; private set; }
    public bool IsActive { get; private set; } = true;

#pragma warning disable CA1056
    public string? TrackingUrlTemplate { get; private set; }
#pragma warning restore CA1056

#pragma warning disable CA1054
    public void Update(string description, string? carrier, decimal baseCost, bool isActive, string? trackingUrlTemplate)
#pragma warning restore CA1054
    {
        Description = description;
        Carrier = carrier;
        BaseCost = baseCost;
        IsActive = isActive;
        TrackingUrlTemplate = trackingUrlTemplate;
    }
}
