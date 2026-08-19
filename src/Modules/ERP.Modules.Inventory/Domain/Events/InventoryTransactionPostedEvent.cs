// <copyright file="InventoryTransactionPostedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Events;

/// <summary>
/// Raised when an inventory transaction (receipt, issue, adjustment, transfer)
/// is committed. Consumed by <c>InventoryPostedToGlHandler</c>, which posts the
/// movement to the General Ledger through the canonical posting contract. This
/// closes the previously-missing Inventory -> GL integration: inventory
/// movements now debit/credit the inventory asset, COGS and goods-received
/// accounts instead of being orphaned in the sub-ledger.
/// </summary>
public record InventoryTransactionPostedEvent : DomainEvent
{
    public InventoryTransactionPostedEvent(
        Guid transactionId,
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        string transactionType,
        decimal quantity,
        decimal unitCost,
        decimal extendedCost,
        DateTime transactionDate,
        Guid? projectId)
    {
        TransactionId = transactionId;
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        TransactionType = transactionType;
        Quantity = quantity;
        UnitCost = unitCost;
        ExtendedCost = extendedCost;
        TransactionDate = transactionDate;
        ProjectId = projectId;
    }

    public Guid TransactionId { get; }

    public Guid CompanyId { get; }

    public Guid ItemId { get; }

    public Guid WarehouseId { get; }

    public string TransactionType { get; }

    public decimal Quantity { get; }

    public decimal UnitCost { get; }

    public decimal ExtendedCost { get; }

    public DateTime TransactionDate { get; }

    public Guid? ProjectId { get; }

    public override string EventType => "InventoryTransactionPosted";
}
