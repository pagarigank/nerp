// <copyright file="InvoiceBatch.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class InvoiceBatch : AuditableAggregateRoot
{
    private readonly List<Invoice> _invoices = [];
    private readonly List<CreditDebitMemo> _creditDebitMemos = [];

    protected InvoiceBatch() { }

    public InvoiceBatch(
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
        Status = InvoiceBatchStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public string BatchNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset PostingDate { get; private set; }

    public Guid FiscalPeriodId { get; private set; }

    public InvoiceBatchStatus Status { get; private set; }

    public IReadOnlyList<Invoice> Invoices => _invoices.AsReadOnly();

    public Invoice AddInvoice(
        Guid customerId,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset dueDate,
        string? description,
        Guid? paymentTermId,
        Guid? projectId,
        Guid? salesOrderId)
    {
        if (Status != InvoiceBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        var invoice = new Invoice(
            Id,
            customerId,
            invoiceNumber,
            invoiceDate,
            dueDate,
            description,
            paymentTermId,
            projectId,
            salesOrderId);

        _invoices.Add(invoice);
        return invoice;
    }

    public CreditDebitMemo AddCreditDebitMemo(
        Guid customerId,
        string referenceNumber,
        DateTimeOffset memoDate,
        Guid? invoiceId,
        string? description)
    {
        if (Status != InvoiceBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        var memo = new CreditDebitMemo(
            Id,
            customerId,
            referenceNumber,
            memoDate,
            invoiceId,
            description);

        _creditDebitMemos.Add(memo);
        return memo;
    }

    public bool IsBalanced()
    {
        return _invoices.Count > 0;
    }

    public void Release()
    {
        if (Status != InvoiceBatchStatus.Draft)
            throw new InvalidOperationException("Only a Draft batch can be released.");

        if (_invoices.Count == 0)
            throw new InvalidOperationException("Batch must have at least one invoice.");

        if (!IsBalanced())
            throw new InvalidOperationException("Batch must be balanced (debits = credits) before release.");

        Status = InvoiceBatchStatus.Batched;
    }

    public void Post()
    {
        if (Status != InvoiceBatchStatus.Batched)
            throw new InvalidOperationException("Only a Batched batch can be posted.");

        Status = InvoiceBatchStatus.Posted;

        AddDomainEvent(new InvoiceBatchPostedEvent(Id, BatchNumber, CompanyId, FiscalPeriodId, PostingDate));
    }

    public InvoiceBatch Reverse(string reversalReason)
    {
        if (Status != InvoiceBatchStatus.Posted)
            throw new InvalidOperationException("Only a Posted batch can be reversed.");

        if (string.IsNullOrWhiteSpace(reversalReason))
            throw new ArgumentException("A reversal reason is required.", nameof(reversalReason));

        Status = InvoiceBatchStatus.Reversed;

        var reversal = new InvoiceBatch(
            CompanyId,
            $"REV-{BatchNumber}",
            $"Reversal: {Description} - {reversalReason}",
            DateTimeOffset.UtcNow,
            FiscalPeriodId);

        foreach (var invoice in _invoices)
        {
            reversal.AddInvoice(invoice.CustomerId, $"REV-{invoice.InvoiceNumber}", DateTimeOffset.UtcNow, invoice.DueDate, $"Reversal of invoice {invoice.InvoiceNumber}", null, null, null);
        }

        return reversal;
    }
}

public enum InvoiceBatchStatus
{
    Draft = 0,
    Batched = 1,
    Posted = 2,
    Reversed = 3,
}

public record InvoiceBatchPostedEvent : DomainEvent
{
    public InvoiceBatchPostedEvent(
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

    public override string EventType => "InvoiceBatchPosted";
}
