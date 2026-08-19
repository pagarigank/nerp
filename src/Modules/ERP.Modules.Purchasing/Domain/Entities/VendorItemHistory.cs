// <copyright file="VendorItemHistory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class VendorItemHistory : AuditableEntity
{
    protected VendorItemHistory() { }

    public VendorItemHistory(
        Guid vendorItemId,
        DateTime effectiveDate,
        decimal previousCost,
        decimal newCost,
        string? notes)
        : base(Guid.NewGuid())
    {
        VendorItemId = vendorItemId;
        EffectiveDate = effectiveDate;
        PreviousCost = previousCost;
        NewCost = newCost;
        Notes = notes;
    }

    public Guid VendorItemId { get; private set; }

    public DateTime EffectiveDate { get; private set; }

    public decimal PreviousCost { get; private set; }

    public decimal NewCost { get; private set; }

    public string? Notes { get; private set; }
}
