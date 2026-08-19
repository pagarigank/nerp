// <copyright file="VendorItem.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class VendorItem : AuditableEntity
{
    private readonly List<VendorItemHistory> _history = [];

    protected VendorItem() { }

    public VendorItem(
        Guid vendorId,
        string itemId,
        string? vendorItemCode,
        string? vendorDescription,
        decimal cost,
        int leadTimeDays,
        decimal minimumOrderQuantity,
        bool isActive = true,
        bool isPrimaryVendor = false)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("Item ID is required.", nameof(itemId));

        if (cost < 0)
            throw new ArgumentException("Cost cannot be negative.", nameof(cost));

        if (leadTimeDays < 0)
            throw new ArgumentException("Lead time cannot be negative.", nameof(leadTimeDays));

        VendorId = vendorId;
        ItemId = itemId;
        VendorItemCode = vendorItemCode;
        VendorDescription = vendorDescription;
        Cost = cost;
        LeadTimeDays = leadTimeDays;
        MinimumOrderQuantity = minimumOrderQuantity;
        IsActive = isActive;
        IsPrimaryVendor = isPrimaryVendor;
        LastPurchaseDate = null;
    }

    public Guid VendorId { get; private set; }

    public string ItemId { get; private set; } = string.Empty;

    public string? VendorItemCode { get; private set; }

    public string? VendorDescription { get; private set; }

    public decimal Cost { get; private set; }

    public int LeadTimeDays { get; private set; }

    public decimal MinimumOrderQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPrimaryVendor { get; private set; }

    public DateTime? LastPurchaseDate { get; private set; }

    public decimal? LastPurchasePrice { get; private set; }

    public IReadOnlyCollection<VendorItemHistory> History => _history.AsReadOnly();

    public void UpdateCost(decimal newCost, DateTime effectiveDate)
    {
        if (newCost < 0)
            throw new ArgumentException("Cost cannot be negative.", nameof(newCost));

        if (newCost != Cost)
        {
            var historyEntry = new VendorItemHistory(
                Id,
                effectiveDate,
                Cost,
                newCost,
                "Cost updated");

            _history.Add(historyEntry);
            Cost = newCost;
        }
    }

    public void UpdateLeadTime(int newLeadTimeDays)
    {
        if (newLeadTimeDays < 0)
            throw new ArgumentException("Lead time cannot be negative.", nameof(newLeadTimeDays));

        LeadTimeDays = newLeadTimeDays;
    }

    public void UpdateMinimumOrderQuantity(decimal newMinimumOrderQuantity)
    {
        if (newMinimumOrderQuantity < 0)
            throw new ArgumentException("Minimum order quantity cannot be negative.", nameof(newMinimumOrderQuantity));

        MinimumOrderQuantity = newMinimumOrderQuantity;
    }

    public void RecordPurchase(decimal purchasePrice, DateTime purchaseDate)
    {
        LastPurchaseDate = purchaseDate;
        LastPurchasePrice = purchasePrice;
    }

    public void SetPrimaryVendor(bool isPrimary)
    {
        IsPrimaryVendor = isPrimary;
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
