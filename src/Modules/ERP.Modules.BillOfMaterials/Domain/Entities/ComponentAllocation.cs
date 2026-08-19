// <copyright file="ComponentAllocation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

/// <summary>
/// Reserves component quantity for a build order so it cannot be consumed by other demand.
/// Integrates with Phase 7 item reservation (reserved qty not available to sales orders).
/// </summary>
public class ComponentAllocation : AuditableEntity
{
    protected ComponentAllocation() { }

    public ComponentAllocation(
        Guid bomHeaderId,
        Guid buildOrderId,
        Guid componentItemId,
        decimal quantity,
        string unitOfMeasure,
        Guid? warehouseId = null)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
            throw new ArgumentException("Allocation quantity must be greater than zero.", nameof(quantity));

        BomHeaderId = bomHeaderId;
        BuildOrderId = buildOrderId;
        ComponentItemId = componentItemId;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        WarehouseId = warehouseId;
    }

    public Guid BomHeaderId { get; private set; }
    public Guid BuildOrderId { get; private set; }
    public Guid ComponentItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal FulfilledQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public Guid? WarehouseId { get; private set; }
    public bool IsReleased { get; private set; }

    public decimal RemainingQuantity => Quantity - FulfilledQuantity;

    public void Fulfill(decimal quantity) => FulfilledQuantity += quantity;

    public void Release() => IsReleased = true;

    public void Update(decimal quantity, string unitOfMeasure)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Allocation quantity must be greater than zero.");
        }

        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
    }
}
