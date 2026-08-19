// <copyright file="VoucherBatch.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class VoucherBatch : AuditableAggregateRoot
{
    private readonly List<Voucher> _vouchers = [];

    protected VoucherBatch() { }

    public VoucherBatch(
        Guid companyId,
        string batchNumber,
        string description,
        DateTimeOffset postingDate,
        Guid fiscalPeriodId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("Batch number is required.", nameof(batchNumber));

        CompanyId = companyId;
        BatchNumber = batchNumber;
        Description = description ?? string.Empty;
        PostingDate = postingDate;
        FiscalPeriodId = fiscalPeriodId;
        Status = VoucherBatchStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public string BatchNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset PostingDate { get; private set; }

    public Guid FiscalPeriodId { get; private set; }

    public VoucherBatchStatus Status { get; private set; }

    public IReadOnlyList<Voucher> Vouchers => _vouchers.AsReadOnly();

    public Voucher AddVoucher(
        Guid vendorId,
        VoucherType voucherType,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset dueDate,
        decimal totalAmount,
        decimal discountAmount,
        string? description,
        Guid? paymentTermId,
        Guid? purchaseOrderId = null,
        Guid? receiptLineId = null)
    {
        if (Status != VoucherBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        var voucher = new Voucher(
            Id,
            vendorId,
            voucherType,
            invoiceNumber,
            invoiceDate,
            dueDate,
            totalAmount,
            discountAmount,
            description,
            paymentTermId,
            purchaseOrderId,
            receiptLineId);

        _vouchers.Add(voucher);
        return voucher;
    }

    public bool IsBalanced()
    {
        return _vouchers.TrueForAll(v =>
        {
            var totalDebits = v.Distributions.Sum(d => d.Debit);
            var totalCredits = v.Distributions.Sum(d => d.Credit);
            return Math.Round(totalDebits, 2) == Math.Round(totalCredits, 2);
        });
    }

    public void Release()
    {
        if (Status != VoucherBatchStatus.Draft)
            throw new InvalidOperationException("Only a Draft batch can be released.");

        if (!IsBalanced())
            throw new InvalidOperationException("Batch must be balanced (debits = credits) before release.");

        if (_vouchers.Count == 0)
            throw new InvalidOperationException("Batch must have at least one voucher.");

        Status = VoucherBatchStatus.Batched;
    }

    public void Post()
    {
        if (Status != VoucherBatchStatus.Batched)
            throw new InvalidOperationException("Only a Batched batch can be posted.");

        Status = VoucherBatchStatus.Posted;

        AddDomainEvent(new VoucherBatchPostedEvent(Id, BatchNumber, CompanyId, FiscalPeriodId, PostingDate));
    }

    public VoucherBatch Reverse(string reversedReason)
    {
        if (Status != VoucherBatchStatus.Posted)
            throw new InvalidOperationException("Only a Posted batch can be reversed.");

        if (string.IsNullOrWhiteSpace(reversedReason))
            throw new ArgumentException("A reversal reason is required.", nameof(reversedReason));

        Status = VoucherBatchStatus.Reversed;

        var reversal = new VoucherBatch(
            CompanyId,
            $"REV-{BatchNumber}",
            $"Reversal: {Description} - {reversedReason}",
            DateTimeOffset.UtcNow,
            FiscalPeriodId);

        foreach (var voucher in _vouchers)
        {
            reversal.AddVoucher(
                voucher.VendorId,
                VoucherType.DebitMemo,
                $"REV-{voucher.InvoiceNumber}",
                DateTimeOffset.UtcNow,
                voucher.DueDate,
                voucher.TotalAmount,
                0,
                $"Reversal of voucher {voucher.InvoiceNumber}",
                null);
        }

        return reversal;
    }

    public void UpdateDescription(string description)
    {
        if (Status != VoucherBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        Description = description ?? string.Empty;
    }
}

public enum VoucherBatchStatus
{
    Draft = 0,
    Batched = 1,
    Posted = 2,
    Reversed = 3,
}

public record VoucherBatchPostedEvent : DomainEvent
{
    public VoucherBatchPostedEvent(
        Guid batchId,
        string batchNumber,
        Guid companyId,
        Guid fiscalPeriodId,
        DateTimeOffset postingDate)
    {
        BatchId = batchId;
        BatchNumber = batchNumber;
        CompanyId = companyId;
        FiscalPeriodId = fiscalPeriodId;
        PostingDate = postingDate;
    }

    public Guid BatchId { get; }
    public string BatchNumber { get; }
    public Guid CompanyId { get; }
    public Guid FiscalPeriodId { get; }
    public DateTimeOffset PostingDate { get; }

    public override string EventType => "VoucherBatchPosted";
}
