// <copyright file="ShippingMethod.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class ShippingMethod : AuditableEntity
{
    protected ShippingMethod() { }

    public ShippingMethod(
        string code,
        string description,
        string? carrierName,
        string? carrierAccountNumber,
        decimal standardLeadTimeDays,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Shipping method code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Code = code;
        Description = description;
        CarrierName = carrierName;
        CarrierAccountNumber = carrierAccountNumber;
        StandardLeadTimeDays = standardLeadTimeDays;
        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string? CarrierName { get; private set; }

    public string? CarrierAccountNumber { get; private set; }

    public decimal StandardLeadTimeDays { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Description is required.", nameof(newDescription));

        Description = newDescription;
    }

    public void UpdateCarrierInfo(string? carrierName, string? carrierAccountNumber)
    {
        CarrierName = carrierName;
        CarrierAccountNumber = carrierAccountNumber;
    }

    public void UpdateLeadTime(decimal days)
    {
        if (days < 0)
            throw new ArgumentException("Lead time cannot be negative.", nameof(days));

        StandardLeadTimeDays = days;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
