// <copyright file="ConsignmentConsumedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Events;

/// <summary>
/// Raised when vendor-owned consignment stock is consumed into owned inventory.
/// The Accounts Payable module can subscribe to create a payable to the vendor.
/// </summary>
public record ConsignmentConsumedEvent : DomainEvent
{
    public ConsignmentConsumedEvent(
        Guid consignmentStockId,
        Guid companyId,
        Guid vendorId,
        Guid itemId,
        decimal quantity,
        decimal unitCost,
        DateTime consumedOn)
    {
        ConsignmentStockId = consignmentStockId;
        CompanyId = companyId;
        VendorId = vendorId;
        ItemId = itemId;
        Quantity = quantity;
        UnitCost = unitCost;
        ConsumedOn = consumedOn;
    }

    public Guid ConsignmentStockId { get; }
    public Guid CompanyId { get; }
    public Guid VendorId { get; }
    public Guid ItemId { get; }
    public decimal Quantity { get; }
    public decimal UnitCost { get; }
    public DateTime ConsumedOn { get; }

    public override string EventType => "ConsignmentConsumed";
}
