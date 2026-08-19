// <copyright file="ReturnConfirmedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Entities;

namespace ERP.Modules.OrderManagement.Domain.Events;

/// <summary>
/// Raised when a customer return (RMA) is confirmed. The Inventory and AR modules
/// consume it: Inventory restocks the returned items (receipt transaction) and AR
/// generates a credit memo against the original shipment, which posts to GL. This
/// is the reverse leg of the Sales -> Inventory / Sales -> AR integration.
/// </summary>
public record ReturnConfirmedEvent(
    Guid ReturnId,
    string ReturnNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? ShipmentId,
    Guid? SalesOrderId,
    DateTime ReturnDate,
    string? ReasonCode,
    List<ReturnLine> Lines) : DomainEvent
{
    public override string EventType => "ReturnConfirmed";
}
