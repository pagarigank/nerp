// <copyright file="DropShipConfirmedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Events;

/// <summary>
/// Raised when a vendor confirms a drop-ship sales-order line was shipped directly
/// to the customer. Complements <see cref="ShipmentConfirmedEvent"/> for lines the
/// vendor fulfills without passing through company inventory.
/// </summary>
public record DropShipConfirmedEvent(
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    Guid? VendorId) : DomainEvent
{
    public override string EventType => "OrderManagement.DropShipConfirmed";
}
