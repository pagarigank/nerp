// <copyright file="PurchasingServiceContracts.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Core.Common;

/// <summary>
/// An item that has fallen below its reorder point, exposed by the Inventory module.
/// Published through ERP.Core so the Purchasing reorder-point scan can consume
/// stock levels without a compile-time dependency on the Inventory module
/// (Inventory already references Purchasing, so the reverse reference would
/// create a module cycle). Mirrors the <see cref="ICreditLimitCheck"/> /
/// <see cref="IInventoryAvailability"/> pattern.
/// </summary>
/// <param name="CompanyId">Company the item belongs to.</param>
/// <param name="ItemId">Inventory item identifier.</param>
/// <param name="ItemCode">Human-readable item code.</param>
/// <param name="PreferredVendorId">Primary vendor assignment, if one exists.</param>
/// <param name="OnHand">Total on-hand quantity across all warehouses.</param>
/// <param name="ReorderPoint">Configured reorder point.</param>
/// <param name="ReorderQuantity">Configured standard replenishment quantity.</param>
public record ReorderCandidate(
    Guid CompanyId,
    Guid ItemId,
    string ItemCode,
    Guid? PreferredVendorId,
    decimal OnHand,
    decimal ReorderPoint,
    decimal ReorderQuantity);

/// <summary>
/// Shared contract exposing items below their reorder point. Implemented by the
/// Inventory module against its own DbContext; consumed by Purchasing background jobs.
/// </summary>
public interface IInventoryReorderSource
{
    Task<IReadOnlyList<ReorderCandidate>> GetBelowReorderPointAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Shared contract for GL budget availability, exposed from ERP.Core so the
/// Purchasing module can enforce committed-cost-vs-budget checks at purchase-order
/// approval without taking a compile-time dependency on the General Ledger module.
/// Implemented by the General Ledger module against GlDbContext.
/// </summary>
public interface IBudgetAvailabilityCheck
{
    /// <summary>
    /// Returns the remaining budget amount for the given company and optional
    /// project / GL account scope. Returns zero when no budget data matches.
    /// </summary>
    /// <param name="companyId">Company whose budgets are queried.</param>
    /// <param name="projectId">Optional project tag to narrow the budget lines.</param>
    /// <param name="glAccountId">Optional GL account to narrow the budget lines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sum of matching active budget-line amounts.</returns>
    Task<decimal> GetRemainingBudgetAsync(Guid companyId, Guid? projectId, Guid? glAccountId, CancellationToken cancellationToken = default);
}
