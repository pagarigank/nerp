// <copyright file="ItemVendorAssignment.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemVendorAssignment : AuditableEntity
{
    protected ItemVendorAssignment() { }

    public ItemVendorAssignment(
        Guid itemId,
        Guid vendorId,
        bool isPrimaryVendor,
        string? vendorItemCode,
        string? vendorDescription,
        decimal? vendorCost,
        int? leadTimeDays,
        decimal? minimumOrderQuantity)
        : base(Guid.NewGuid())
    {
        ItemId = itemId;
        VendorId = vendorId;
        IsPrimaryVendor = isPrimaryVendor;
        VendorItemCode = vendorItemCode;
        VendorDescription = vendorDescription;
        VendorCost = vendorCost;
        LeadTimeDays = leadTimeDays;
        MinimumOrderQuantity = minimumOrderQuantity;
        IsActive = true;
    }

    public Guid ItemId { get; private set; }

    public Guid VendorId { get; private set; }

    public bool IsPrimaryVendor { get; private set; }

    public string? VendorItemCode { get; private set; }

    public string? VendorDescription { get; private set; }

    public decimal? VendorCost { get; private set; }

    public int? LeadTimeDays { get; private set; }

    public decimal? MinimumOrderQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public void SetPrimaryVendor(bool isPrimary)
    {
        IsPrimaryVendor = isPrimary;
    }

    public void UpdateVendorDetails(
        string? vendorItemCode,
        string? vendorDescription,
        decimal? vendorCost,
        int? leadTimeDays,
        decimal? minimumOrderQuantity)
    {
        VendorItemCode = vendorItemCode;
        VendorDescription = vendorDescription;
        VendorCost = vendorCost;
        LeadTimeDays = leadTimeDays;
        MinimumOrderQuantity = minimumOrderQuantity;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}