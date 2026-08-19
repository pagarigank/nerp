// <copyright file="IPurchaseOrderService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Purchasing.Infrastructure;

public interface IPurchaseOrderService
{
    Task<Guid> CreateChangeOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<bool> IsAutoClosureEligibleAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task AutoClosePurchaseOrdersAsync(int daysOld = 90, CancellationToken cancellationToken = default);
    Task<decimal> CalculateCommittedCostAsync(Guid? projectId, Guid? accountId, CancellationToken cancellationToken = default);
}
