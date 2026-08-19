// <copyright file="CommissionAccrual.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

/// <summary>
/// Commission accrual for a sales rep, created when a shipment is confirmed (Sales -&gt; AP
/// integration). Each accrual records the commission earned on a shipment line, links to the
/// rep's AP vendor (the payable target), and drives an AP voucher (Commission Expense debit /
/// AP control credit) that posts to the General Ledger. Accruals are reversed/cancelled if the
/// underlying sale is later returned.
/// </summary>
public class CommissionAccrual : AuditableEntity
{
    private CommissionAccrual() { }

    public CommissionAccrual(
        Guid companyId,
        Guid salesRepId,
        Guid? vendorId,
        Guid shipmentId,
        string shipmentNumber,
        Guid? salesOrderId,
        string? salesOrderNumber,
        Guid? customerId,
        decimal baseAmount,
        decimal commissionRate,
        decimal commissionAmount,
        Guid? voucherId)
        : base(Guid.NewGuid())
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative.", nameof(baseAmount));
        if (commissionRate < 0 || commissionRate > 100)
            throw new ArgumentOutOfRangeException(nameof(commissionRate), "Commission rate must be between 0 and 100.");

        CompanyId = companyId;
        SalesRepId = salesRepId;
        VendorId = vendorId;
        ShipmentId = shipmentId;
        ShipmentNumber = shipmentNumber;
        SalesOrderId = salesOrderId;
        SalesOrderNumber = salesOrderNumber;
        CustomerId = customerId;
        BaseAmount = baseAmount;
        CommissionRate = commissionRate;
        CommissionAmount = commissionAmount;
        VoucherId = voucherId;
        Status = CommissionAccrualStatus.Accrued;
    }

    public Guid CompanyId { get; private set; }
    public Guid SalesRepId { get; private set; }
    public Guid? VendorId { get; private set; }
    public Guid ShipmentId { get; private set; }
    public string ShipmentNumber { get; private set; } = string.Empty;
    public Guid? SalesOrderId { get; private set; }
    public string? SalesOrderNumber { get; private set; }
    public Guid? CustomerId { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal CommissionRate { get; private set; }
    public decimal CommissionAmount { get; private set; }
    public Guid? VoucherId { get; private set; }
    public CommissionAccrualStatus Status { get; private set; }

    public void Cancel()
    {
        if (Status == CommissionAccrualStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a commission accrual that has already been paid.");
        Status = CommissionAccrualStatus.Cancelled;
    }

    public void SetVoucherId(Guid voucherId) => VoucherId = voucherId;

    public void MarkPaid()
    {
        if (Status == CommissionAccrualStatus.Cancelled)
            throw new InvalidOperationException("Cannot pay a cancelled commission accrual.");
        Status = CommissionAccrualStatus.Paid;
    }
}

public enum CommissionAccrualStatus
{
    Accrued = 0,
    Paid = 1,
    Cancelled = 2,
}
