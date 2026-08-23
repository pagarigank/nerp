// <copyright file="BomServiceContracts.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Core.Common;

/// <summary>
/// Snapshot of an inventory item's identity, status and costing fields exposed by the
/// Inventory module for the Bill of Materials validation and cost roll-up jobs without
/// a compile-time dependency on the Inventory module (mirrors the
/// <see cref="IInventoryReorderSource"/> pattern).
/// </summary>
/// <param name="ItemId">Inventory item identifier.</param>
/// <param name="ItemCode">Human-readable item code.</param>
/// <param name="IsActive">True when the item is in active status.</param>
/// <param name="UnitCost">Current unit cost, when one is tracked.</param>
/// <param name="StandardCost">Configured standard cost.</param>
public record InventoryItemInfo(
    Guid ItemId,
    string ItemCode,
    bool IsActive,
    decimal? UnitCost,
    decimal? StandardCost);

/// <summary>
/// Shared contract exposing inventory item master data. Implemented by the Inventory
/// module against its own DbContext; consumed by Bill of Materials background jobs.
/// </summary>
public interface IInventoryItemLookup
{
    Task<IReadOnlyList<InventoryItemInfo>> GetItemsAsync(IReadOnlyList<Guid> itemIds, CancellationToken ct);
}

/// <summary>
/// A single component quantity to reserve for a production build order.
/// </summary>
/// <param name="ItemId">Component item identifier.</param>
/// <param name="Quantity">Quantity to reserve.</param>
/// <param name="UnitOfMeasure">Unit of measure of the quantity.</param>
public record ComponentReservationRequest(Guid ItemId, decimal Quantity, string UnitOfMeasure);

/// <summary>
/// Shared contract for reserving component stock against a build order so the
/// reserved quantity is not available to other demand. Implemented by the
/// Inventory module using its ItemReservation entity; consumed by the Bill of
/// Materials build-order release flow.
/// </summary>
public interface IComponentReservationService
{
    /// <summary>
    /// Reserves the given components for the build order. Idempotent: returns zero
    /// without creating anything when reservations already exist for the build order.
    /// </summary>
    /// <param name="companyId">Company owning the build order.</param>
    /// <param name="buildOrderId">Build order used as the reservation source id.</param>
    /// <param name="components">Component quantities to reserve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of reservations created (zero when they already existed).</returns>
    Task<int> ReserveForBuildOrderAsync(Guid companyId, Guid buildOrderId, IReadOnlyList<ComponentReservationRequest> components, CancellationToken ct);
}
