// <copyright file="RequisitionApprovedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Events;

public record RequisitionApprovedEvent(
    Guid RequisitionId,
    string RequisitionNumber,
    Guid CompanyId,
    Guid ApprovedById,
    decimal TotalAmount) : DomainEvent
{
    public override string EventType => "Purchasing.RequisitionApproved";
}
