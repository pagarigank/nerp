// <copyright file="GoodsReceiptMatch.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

/// <summary>
/// Records the "received" leg of the Accounts Payable 3-way match. When a
/// purchasing goods receipt is posted, <see cref="GoodsReceivedEvent"/> is raised
/// and consumed by <c>GoodsReceivedToApHandler</c>, which writes one row per
/// receipt line here. The 3-way match (PO &lt;-&gt; Receipt &lt;-&gt; Invoice) then
/// correlates this received quantity against the matched voucher's invoice quantity.
/// </summary>
public class GoodsReceiptMatch : Entity
{
    protected GoodsReceiptMatch() { }

    public GoodsReceiptMatch(
        Guid companyId,
        Guid receiptId,
        string receiptNumber,
        Guid? purchaseOrderId,
        Guid? vendorId,
        Guid? purchaseOrderLineId,
        string? itemId,
        string description,
        decimal quantityReceived,
        string unitOfMeasure,
        DateTimeOffset receivedDate,
        bool overReceiptFlag)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ReceiptId = receiptId;
        ReceiptNumber = receiptNumber;
        PurchaseOrderId = purchaseOrderId;
        VendorId = vendorId;
        PurchaseOrderLineId = purchaseOrderLineId;
        ItemId = itemId;
        Description = description;
        QuantityReceived = quantityReceived;
        UnitOfMeasure = unitOfMeasure;
        ReceivedDate = receivedDate;
        OverReceiptFlag = overReceiptFlag;
    }

    public Guid CompanyId { get; private set; }

    public Guid ReceiptId { get; private set; }

    public string ReceiptNumber { get; private set; } = string.Empty;

    public Guid? PurchaseOrderId { get; private set; }

    public Guid? VendorId { get; private set; }

    public Guid? PurchaseOrderLineId { get; private set; }

    public string? ItemId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal QuantityReceived { get; private set; }

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedDate { get; private set; }

    /// <summary>
    /// True when the received quantity exceeds the ordered quantity by more than
    /// the standard tolerance, requiring buyer-manager over-receipt approval
    /// (spec §6: Over-receipt exception approval workflow).
    /// </summary>
    public bool OverReceiptFlag { get; private set; }
}
