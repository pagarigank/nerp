// <copyright file="SalesOrderConfirmedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Entities;

namespace ERP.Modules.OrderManagement.Domain.Events;

/// <summary>
/// Raised when a sales order is confirmed. Consumed by the Inventory module to
/// reserve/allocate stock against the order (Purchase -> Inventory -> Sales link).
/// </summary>
public record SalesOrderConfirmedEvent(
    Guid SalesOrderId,
    string OrderNumber,
    Guid CompanyId,
    Guid CustomerId,
    List<SalesOrderLine> Lines) : DomainEvent
{
    public override string EventType => "OrderManagement.SalesOrderConfirmed";
}
