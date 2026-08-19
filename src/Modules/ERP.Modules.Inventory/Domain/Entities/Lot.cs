// <copyright file="Lot.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class Lot : AuditableEntity
{
    protected Lot() { }

    public Lot(
        string lotNumber,
        Guid itemId,
        Guid warehouseId,
        DateTime receivedDate,
        DateTime? expirationDate = null,
        string? vendorLotNumber = null,
        LotStatus status = LotStatus.Active)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(lotNumber))
            throw new ArgumentException("Lot number is required.", nameof(lotNumber));

        LotNumber = lotNumber;
        ItemId = itemId;
        WarehouseId = warehouseId;
        ReceivedDate = receivedDate;
        ExpirationDate = expirationDate;
        VendorLotNumber = vendorLotNumber;
        Status = status;
    }

    public string LotNumber { get; private set; } = string.Empty;

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public DateTime ReceivedDate { get; private set; }

    public DateTime? ExpirationDate { get; private set; }

    public string? VendorLotNumber { get; private set; }

    public LotStatus Status { get; private set; }

    public bool IsExpired() => ExpirationDate.HasValue && DateTime.UtcNow > ExpirationDate.Value;

    public void Quarantine() => Status = LotStatus.Quarantine;

    public void Release() => Status = LotStatus.Active;

    public void Expire() => Status = LotStatus.Expired;

    // Navigation properties
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
}

public enum LotStatus
{
    None = 0,
    Active = 1,
    Quarantine = 2,
    Expired = 3,
}
