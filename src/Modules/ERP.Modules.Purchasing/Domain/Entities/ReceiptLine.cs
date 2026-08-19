// <copyright file="ReceiptLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class ReceiptLine : AuditableEntity
{
    protected ReceiptLine() { }

    public ReceiptLine(
        Guid receiptId,
        int lineNumber,
        Guid? purchaseOrderLineId,
        string? itemId,
        string description,
        decimal quantityReceived,
        string unitOfMeasure,
        string? lotNumber,
        string? serialNumber,
        bool qualityInspectionRequired,
        Guid? warehouseId,
        Guid? binLocationId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (quantityReceived <= 0)
            throw new ArgumentException("Quantity received must be greater than zero.", nameof(quantityReceived));

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
            throw new ArgumentException("Unit of measure is required.", nameof(unitOfMeasure));

        ReceiptId = receiptId;
        LineNumber = lineNumber;
        PurchaseOrderLineId = purchaseOrderLineId;
        ItemId = itemId;
        Description = description;
        QuantityReceived = quantityReceived;
        UnitOfMeasure = unitOfMeasure;
        LotNumber = lotNumber;
        SerialNumber = serialNumber;
        QualityInspectionRequired = qualityInspectionRequired;
        WarehouseId = warehouseId;
        BinLocationId = binLocationId;
    }

    public Guid ReceiptId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid? PurchaseOrderLineId { get; private set; }

    public string? ItemId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal QuantityReceived { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public string? LotNumber { get; private set; }

    public string? SerialNumber { get; private set; }

    public bool QualityInspectionRequired { get; private set; }

    public Guid? WarehouseId { get; private set; }

    public Guid? BinLocationId { get; private set; }

    public bool InspectionPassed { get; private set; }

    public DateTime? InspectionDate { get; private set; }

    public string? InspectionNotes { get; private set; }

    public void RecordInspection(bool passed, string? notes = null)
    {
        if (!QualityInspectionRequired)
            throw new InvalidOperationException("Inspection not required for this line.");

        InspectionPassed = passed;
        InspectionDate = DateTime.UtcNow;
        InspectionNotes = notes;
    }
}
