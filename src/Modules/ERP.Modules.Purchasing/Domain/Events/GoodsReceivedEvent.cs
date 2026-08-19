// <copyright file="GoodsReceivedEvent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Events;

public record GoodsReceivedEvent(
    Guid ReceiptId,
    string ReceiptNumber,
    Guid CompanyId,
    Guid? PurchaseOrderId,
    Guid? VendorId,
    DateTime ReceivedDate,
    List<GoodsReceivedLine> Lines) : DomainEvent
{
    public override string EventType => "Purchasing.GoodsReceived";
}

public class GoodsReceivedLine
{
    public Guid ReceiptLineId { get; set; }

    public Guid? PurchaseOrderLineId { get; set; }

    public string? ItemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal QuantityReceived { get; set; }

    public string UnitOfMeasure { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }

    public Guid? TaskId { get; set; }
}
