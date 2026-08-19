// <copyright file="InventoryTransaction.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class InventoryTransaction : AuditableEntity
{
    protected InventoryTransaction() { }

    public InventoryTransaction(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        TransactionType transactionType,
        decimal quantity,
        string unitOfMeasure,
        decimal unitCost,
        DateTime transactionDate,
        Guid? binId = null,
        Guid? lotId = null,
        string? serialNumber = null,
        string? referenceNumber = null,
        Guid? projectId = null,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        TransactionType = transactionType;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        UnitCost = unitCost;
        ExtendedCost = quantity * unitCost;
        TransactionDate = transactionDate;
        BinId = binId;
        LotId = lotId;
        SerialNumber = serialNumber;
        ReferenceNumber = referenceNumber;
        ProjectId = projectId;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public TransactionType TransactionType { get; private set; }

    public decimal Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public decimal UnitCost { get; private set; }

    public decimal ExtendedCost { get; private set; }

    public DateTime TransactionDate { get; private set; }

    public Guid? BinId { get; private set; }

    public Guid? LotId { get; private set; }

    public string? SerialNumber { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public Guid? ProjectId { get; private set; }

    public string? Notes { get; private set; }
}

public enum TransactionType
{
    None = 0,
    Receipt = 1,
    Issue = 2,
    Transfer = 3,
    Adjustment = 4,
    TransferIn = 5,
    TransferOut = 6,
    ProductionReceipt = 7,
    Shipment = 8,
    Scrap = 9,
}
