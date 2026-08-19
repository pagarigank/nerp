// <copyright file="ItemCostLayer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemCostLayer : AuditableEntity
{
    protected ItemCostLayer() { }

    public ItemCostLayer(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        decimal unitCost,
        DateTime receivedDate,
        Guid? lotId = null,
        string? referenceNumber = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        Quantity = quantity;
        RemainingQuantity = quantity;
        UnitCost = unitCost;
        ReceivedDate = receivedDate;
        LotId = lotId;
        ReferenceNumber = referenceNumber;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal RemainingQuantity { get; private set; }

    public decimal UnitCost { get; private set; }

    public DateTime ReceivedDate { get; private set; }

    public Guid? LotId { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public bool IsFullyConsumed => RemainingQuantity <= 0;

    public void Consume(decimal quantity)
    {
        if (quantity > RemainingQuantity)
            throw new InvalidOperationException($"Cannot consume {quantity} from layer with only {RemainingQuantity} remaining.");
        RemainingQuantity -= quantity;
    }

    public void Restore(decimal quantity) => RemainingQuantity += quantity;

    public void SetLotId(Guid lotId)
    {
        LotId = lotId;
    }
}
