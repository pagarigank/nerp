// <copyright file="ReturnToVendor.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Return-to-Vendor (RTV) disposition for a customer RMA (Phase 8 gap 585). When a
/// customer return is dispositioned back to the original vendor for credit, this
/// record captures the vendor, the returned quantity and the expected vendor credit.
/// A confirmed RTV can be wired to Purchasing to create a debit memo / return PO.
/// </summary>
public class ReturnToVendor : AuditableEntity
{
    protected ReturnToVendor() { }

    public ReturnToVendor(
        Guid companyId,
        Guid returnId,
        Guid returnLineId,
        Guid vendorId,
        decimal quantity,
        decimal unitCost,
        string? reference = null)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        CompanyId = companyId;
        ReturnId = returnId;
        ReturnLineId = returnLineId;
        VendorId = vendorId;
        Quantity = quantity;
        UnitCost = unitCost;
        Reference = reference;
        Status = RtvStatus.Open;
    }

    public Guid CompanyId { get; private set; }
    public Guid ReturnId { get; private set; }
    public Guid ReturnLineId { get; private set; }
    public Guid VendorId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal ExpectedCredit => Quantity * UnitCost;
    public string? Reference { get; private set; }
    public RtvStatus Status { get; private set; }
    public DateTime? ShippedToVendorDate { get; private set; }
    public Guid? PurchasingReturnId { get; private set; }

    public void MarkShippedToVendor(Guid? purchasingReturnId = null)
    {
        if (Status != RtvStatus.Open)
            throw new InvalidOperationException($"Cannot ship an RTV in {Status} status.");
        Status = RtvStatus.ShippedToVendor;
        ShippedToVendorDate = DateTime.UtcNow;
        PurchasingReturnId = purchasingReturnId;
    }

    public void ReceiveVendorCredit()
    {
        if (Status != RtvStatus.ShippedToVendor)
            throw new InvalidOperationException("Vendor credit can only be received after shipping to vendor.");
        Status = RtvStatus.Credited;
    }
}

public enum RtvStatus
{
    Open = 0,
    ShippedToVendor = 1,
    Credited = 2,
}
