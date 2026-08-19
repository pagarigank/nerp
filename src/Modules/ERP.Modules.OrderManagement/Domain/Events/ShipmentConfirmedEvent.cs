// <copyright file="ShipmentConfirmedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Entities;

namespace ERP.Modules.OrderManagement.Domain.Events;

/// <summary>
/// Raised when a shipment is confirmed. This is the hub of the order-to-cash flow:
/// the Inventory module consumes it to relieve stock (COGS) and the Accounts
/// Receivable module consumes it to generate the customer invoice, which then posts
/// to the General Ledger. It is the Sales leg of the Purchase -> Inventory -> Sales
/// integration chain.
/// </summary>
public record ShipmentConfirmedEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTime ShipmentDate,
    string? Carrier,
    string? TrackingNumber,
    decimal FreightCost,
    List<ShipmentLine> Lines) : DomainEvent
{
    public override string EventType => "OrderManagement.ShipmentConfirmed";
}
