// <copyright file="ItemQuarantine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemQuarantine : AuditableEntity
{
    private readonly List<QuarantineDisposition> _dispositions = [];

    protected ItemQuarantine() { }

    public ItemQuarantine(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        Guid? binId,
        Guid? lotId,
        Guid? serialNumberId,
        decimal quantity,
        string unitOfMeasure,
        string reason,
        Guid quarantinedBy,
        string? referenceNumber = null,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        BinId = binId;
        LotId = lotId;
        SerialNumberId = serialNumberId;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        Reason = reason;
        QuarantinedBy = quarantinedBy;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        Status = QuarantineStatus.Active;
        QuarantineDate = DateTime.UtcNow;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? BinId { get; private set; }

    public Guid? LotId { get; private set; }

    public Guid? SerialNumberId { get; private set; }

    public decimal Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public string? ReferenceNumber { get; private set; }

    public string? Notes { get; private set; }

    public QuarantineStatus Status { get; private set; }

    public DateTime QuarantineDate { get; private set; }

    public Guid QuarantinedBy { get; private set; }

    public DateTime? ReleasedDate { get; private set; }

    public Guid? ReleasedBy { get; private set; }

    public string? ReleaseReason { get; private set; }

    public IReadOnlyCollection<QuarantineDisposition> Dispositions => _dispositions.AsReadOnly();

    public void Release(Guid releasedBy, string reason)
    {
        Status = QuarantineStatus.Released;
        ReleasedDate = DateTime.UtcNow;
        ReleasedBy = releasedBy;
        ReleaseReason = reason;
    }

    public void MarkAsDisposed(Guid disposedBy, string reason)
    {
        Status = QuarantineStatus.Disposed;
        ReleasedDate = DateTime.UtcNow;
        ReleasedBy = disposedBy;
        ReleaseReason = reason;
    }

    public void Reject(Guid rejectedBy, string reason)
    {
        Status = QuarantineStatus.Rejected;
        ReleasedDate = DateTime.UtcNow;
        ReleasedBy = rejectedBy;
        ReleaseReason = reason;
    }

    public void AddDisposition(QuarantineDisposition disposition)
    {
        _dispositions.Add(disposition);
    }

    public void UpdateQuantity(decimal quantity)
    {
        Quantity = quantity;
    }

    public void UpdateWarehouse(Guid warehouseId, Guid? binId = null)
    {
        WarehouseId = warehouseId;
        BinId = binId;
    }

    public void UpdateStatus(QuarantineStatus status)
    {
        Status = status;
    }
}

public class QuarantineDisposition : AuditableEntity
{
    protected QuarantineDisposition() { }

    public QuarantineDisposition(
        Guid quarantineId,
        QuarantineAction action,
        decimal quantity,
        Guid? destinationWarehouseId = null,
        Guid? destinationBinId = null,
        string? notes = null,
        Guid? performedBy = null)
        : base(Guid.NewGuid())
    {
        QuarantineId = quarantineId;
        Action = action;
        Quantity = quantity;
        DestinationWarehouseId = destinationWarehouseId;
        DestinationBinId = destinationBinId;
        Notes = notes;
        PerformedBy = performedBy;
        DispositionDate = DateTime.UtcNow;
    }

    public Guid QuarantineId { get; private set; }

    public QuarantineAction Action { get; private set; }

    public decimal Quantity { get; private set; }

    public Guid? DestinationWarehouseId { get; private set; }

    public Guid? DestinationBinId { get; private set; }

    public string? Notes { get; private set; }

    public Guid? PerformedBy { get; private set; }

    public DateTime DispositionDate { get; private set; }
}

public enum QuarantineStatus
{
    None = 0,
    Active = 1,
    Released = 2,
    Disposed = 3,
    Rejected = 4,
    PartiallyReleased = 5,
}

public enum QuarantineAction
{
    None = 0,
    Release = 1,
    Dispose = 2,
    Reject = 3,
    Transfer = 4,
    Rework = 5,
}