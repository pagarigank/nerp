// <copyright file="ItemMovement.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemMovement : AuditableEntity
{
    protected ItemMovement() { }

    public ItemMovement(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        Guid? binId,
        Guid? lotId,
        Guid? serialNumberId,
        MovementType movementType,
        decimal quantity,
        string unitOfMeasure,
        decimal? unitCost = null,
        string? referenceNumber = null,
        string? referenceType = null,
        Guid? referenceId = null,
        string? notes = null,
        Guid? createdBy = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        BinId = binId;
        LotId = lotId;
        SerialNumberId = serialNumberId;
        MovementType = movementType;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        UnitCost = unitCost;
        ReferenceNumber = referenceNumber;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
        if (createdBy.HasValue)
        {
            CreatedBy = createdBy.Value.ToString();
        }

        MovementDate = DateTime.UtcNow;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? BinId { get; private set; }

    public Guid? LotId { get; private set; }

    public Guid? SerialNumberId { get; private set; }

    public MovementType MovementType { get; private set; }

    public decimal Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public decimal? UnitCost { get; private set; }

    public decimal? ExtendedCost => UnitCost.HasValue ? Quantity * UnitCost.Value : null;

    public string? ReferenceNumber { get; private set; }

    public string? ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Notes { get; private set; }

    public DateTime MovementDate { get; private set; }
}

public enum MovementType
{
    None = 0,
    Receipt = 1,
    Issue = 2,
    TransferIn = 3,
    TransferOut = 4,
    AdjustmentIn = 5,
    AdjustmentOut = 6,
    CycleCountAdjustment = 7,
    PhysicalCountAdjustment = 8,
    Revaluation = 9,
    Quarantine = 10,
    QuarantineRelease = 11,
    QuarantineDispose = 12,
    QuarantineReject = 13,
    QuarantineTransfer = 14,
    ReservationAllocate = 15,
    ReservationRelease = 16,
    ProductionReceipt = 17,
    ProductionIssue = 18,
    Shipment = 19,
    Return = 20,
    CostAdjustment = 21,
}