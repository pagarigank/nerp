// <copyright file="Invoice.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class Invoice : Entity
{
    private readonly List<InvoiceLine> _lines = [];

    protected Invoice() { }

    internal Invoice(
        Guid invoiceBatchId,
        Guid customerId,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset dueDate,
        string? description,
        Guid? paymentTermId,
        Guid? projectId,
        Guid? salesOrderId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));

        InvoiceBatchId = invoiceBatchId;
        CustomerId = customerId;
        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        Description = description ?? string.Empty;
        PaymentTermId = paymentTermId;
        ProjectId = projectId;
        SalesOrderId = salesOrderId;
        Status = InvoiceStatus.Open;
    }

    public Guid InvoiceBatchId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string InvoiceNumber { get; private set; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; private set; }

    public DateTimeOffset DueDate { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? PaymentTermId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? SalesOrderId { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public decimal TotalAmount => _lines.Sum(l => l.TotalAmount);

    public decimal TotalPaid { get; private set; }

    public decimal BalanceDue => TotalAmount - TotalPaid;

    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();

    public InvoiceLine AddLine(
        Guid accountId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxAmount,
        decimal? discountAmount)
    {
        var line = new InvoiceLine(
            Id,
            null,
            accountId,
            description,
            quantity,
            unitPrice,
            taxAmount,
            discountAmount ?? 0);

        _lines.Add(line);
        return line;
    }

    public void Void()
    {
        if (Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("Invoice is already voided.");

        if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Cannot void an invoice that has cash applied. Unapply cash first.");

        Status = InvoiceStatus.Voided;
    }

    public void WriteOff(decimal amount, string reason)
    {
        if (Status != InvoiceStatus.Open)
            throw new InvalidOperationException("Only Open invoices can be written off.");

        if (amount <= 0)
            throw new ArgumentException("Write-off amount must be positive.", nameof(amount));

        if (amount > BalanceDue)
            throw new ArgumentException("Write-off amount cannot exceed balance due.", nameof(amount));

        Status = InvoiceStatus.WriteOff;
    }

    internal void ApplyPayment(decimal amount)
    {
        TotalPaid += amount;
        var remaining = BalanceDue;
        if (remaining <= 0)
            Status = InvoiceStatus.Paid;
        else if (TotalPaid > 0)
            Status = InvoiceStatus.PartiallyPaid;
        else
            Status = InvoiceStatus.Open;
    }
}

public enum InvoiceStatus
{
    Open = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Voided = 3,
    WriteOff = 4,
}

public record InvoicePostedEvent : DomainEvent
{
    public InvoicePostedEvent(Guid invoiceId, string invoiceNumber, Guid companyId, Guid customerId, decimal totalAmount)
    {
        InvoiceId = invoiceId;
        InvoiceNumber = invoiceNumber;
        CompanyId = companyId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }

    public Guid InvoiceId { get; }
    public string InvoiceNumber { get; }
    public Guid CompanyId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }

    public override string EventType => "InvoicePosted";
}
