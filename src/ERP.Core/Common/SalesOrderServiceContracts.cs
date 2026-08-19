// <copyright file="SalesOrderServiceContracts.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Core.Common;

/// <summary>
/// Shared contract for an Accounts-Receivable credit-limit check, exposed from
/// ERP.Core so the Order Management module can enforce credit policy on a sales
/// order without taking a compile-time dependency on the AR module (which would
/// create a module reference cycle: AR -> OM for the shipment-to-invoice handler).
/// The AR module implements this against its own DbContext.
/// </summary>
public interface ICreditLimitCheck
{
    Task<CreditLimitCheckResult> CheckAsync(Guid customerId, decimal proposedAmount, CancellationToken cancellationToken = default);
}

public record CreditLimitCheckResult(
    bool IsApproved,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal AvailableCredit,
    string? Message);

/// <summary>
/// Shared contract for real-time inventory availability, exposed from ERP.Core so the
/// Order Management module can gate a sales order on available stock without a
/// compile-time dependency on the Inventory module (Inventory -> OM for the
/// shipment-to-issue handler, so OM must not reference Inventory).
/// </summary>
public interface IInventoryAvailability
{
    Task<AvailabilityResult> CheckAsync(Guid itemId, Guid warehouseId, decimal requestedQuantity, CancellationToken cancellationToken = default);
}

public record AvailabilityResult(
    decimal OnHand,
    decimal Allocated,
    decimal Available,
    bool IsSufficient);
