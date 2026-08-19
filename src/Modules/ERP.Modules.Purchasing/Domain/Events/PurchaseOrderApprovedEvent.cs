// <copyright file="PurchaseOrderApprovedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Events;

public record PurchaseOrderApprovedEvent(
    Guid PurchaseOrderId,
    string PONumber,
    Guid CompanyId,
    Guid VendorId,
    Guid ApprovedById,
    decimal TotalAmount) : DomainEvent
{
    public override string EventType => "Purchasing.PurchaseOrderApproved";
}
