// <copyright file="ItemReservation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemReservation : AuditableEntity
{
    protected ItemReservation() { }

    public ItemReservation(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        string unitOfMeasure,
        ReservationSourceType sourceType,
        Guid sourceId,
        Guid? binId = null,
        string? lotNumber = null,
        string? serialNumber = null,
        DateTime? expirationDate = null,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        SourceType = sourceType;
        SourceId = sourceId;
        BinId = binId;
        LotNumber = lotNumber;
        SerialNumber = serialNumber;
        ExpirationDate = expirationDate;
        Notes = notes;
        Status = ItemReservationStatus.Active;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? BinId { get; private set; }

    public decimal Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public ReservationSourceType SourceType { get; private set; }

    public Guid SourceId { get; private set; }

    public string? LotNumber { get; private set; }

    public string? SerialNumber { get; private set; }

    public DateTime? ExpirationDate { get; private set; }

    public string? Notes { get; private set; }

    public ItemReservationStatus Status { get; private set; }

    public decimal ReleasedQuantity { get; private set; }

    public decimal RemainingQuantity => Quantity - ReleasedQuantity;

    public void Release(decimal quantity)
    {
        if (quantity > RemainingQuantity)
            throw new InvalidOperationException($"Cannot release {quantity}, only {RemainingQuantity} remaining.");

        ReleasedQuantity += quantity;

        if (ReleasedQuantity >= Quantity)
        {
            Status = ItemReservationStatus.FullyReleased;
        }
        else if (ReleasedQuantity > 0)
        {
            Status = ItemReservationStatus.PartiallyReleased;
        }
    }

    public void Cancel(Guid cancelledBy)
    {
        if (Status == ItemReservationStatus.FullyReleased)
        {
            throw new InvalidOperationException("Cannot cancel a fully released reservation.");
        }

        Status = ItemReservationStatus.Cancelled;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    public void SetBinId(Guid binId)
    {
        BinId = binId;
    }

    public void SetLotNumber(string lotNumber)
    {
        LotNumber = lotNumber;
    }

    public void SetSerialNumber(string serialNumber)
    {
        SerialNumber = serialNumber;
    }
}

public enum ItemReservationStatus
{
    None = 0,
    Active = 1,
    PartiallyReleased = 2,
    FullyReleased = 3,
    Cancelled = 4,
}

public enum ReservationSourceType
{
    None = 0,
    SalesOrder = 1,
    PurchaseOrder = 2,
    ProductionOrder = 3,
    Project = 4,
    TransferOrder = 5,
    Manual = 99,
}