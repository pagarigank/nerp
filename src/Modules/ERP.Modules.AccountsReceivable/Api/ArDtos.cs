// <copyright file="ArDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsReceivable.Api;

public record CreateCustomerRequest(
    string CustomerId,
    string Name,
    string? LegalName,
    string? TaxId,
    decimal CreditLimit,
    int CreditHoldDays,
    Guid? DefaultPaymentTermId,
    bool TaxExempt,
    string? TaxExemptCertificate,
    string? CurrencyCode,
    Guid? SalesRepId,
    Guid? TaxCodeId,
    Guid? TaxExemptionCertificateId);

public record UpdateCustomerRequest(
    string Name,
    string? LegalName,
    string? TaxId,
    decimal CreditLimit,
    int CreditHoldDays,
    Guid? DefaultPaymentTermId,
    bool TaxExempt,
    string? TaxExemptCertificate,
    string? CurrencyCode,
    Guid? SalesRepId,
    Guid? TaxCodeId,
    Guid? TaxExemptionCertificateId);

public record CustomerResponse(
    Guid Id,
    string CustomerId,
    string Name,
    string? LegalName,
    string? TaxId,
    decimal CreditLimit,
    int CreditHoldDays,
    Guid? DefaultPaymentTermId,
    bool TaxExempt,
    string? TaxExemptCertificate,
    string CurrencyCode,
    bool IsActive,
    Guid? SalesRepId,
    Guid? TaxCodeId,
    Guid? TaxExemptionCertificateId);

public record CreateInvoiceBatchRequest(
    Guid CompanyId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    Guid FiscalPeriodId);

public record InvoiceBatchLineItem(
    Guid CustomerId,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    string? Description,
    Guid? PaymentTermId,
    Guid? ProjectId,
    Guid? SalesOrderId,
    IReadOnlyList<InvoiceLineItem> Lines);

public record InvoiceLineItem(
    Guid AccountId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxAmount,
    decimal? DiscountAmount);

public record InvoiceBatchResponse(
    Guid Id,
    string BatchNumber,
    string Description,
    string Status,
    int InvoiceCount,
    decimal TotalAmount);

public record InvoiceResponse(
    Guid Id,
    Guid CustomerId,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    decimal TotalAmount,
    decimal BalanceDue,
    string Status);

public record InvoiceLineDetailResponse(
    Guid AccountId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount);

public record InvoiceDetailResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    string Description,
    string Status,
    decimal TotalAmount,
    decimal BalanceDue,
    IReadOnlyList<InvoiceLineDetailResponse> Lines);

public record InvoiceBatchDetailResponse(
    Guid Id,
    string BatchNumber,
    string Description,
    string Status,
    DateTimeOffset PostingDate,
    IReadOnlyList<InvoiceDetailResponse> Invoices);

public record CreateCashReceiptRequest(
    Guid CompanyId,
    Guid CustomerId,
    string ReceiptReference,
    decimal TotalAmount,
    DateTimeOffset ReceiptDate,
    string PaymentMethod,
    string? CurrencyCode,
    string? ReferenceNumber);

public record ApplyCashRequest(
    Guid InvoiceId,
    decimal Amount);

public record CashReceiptResponse(
    Guid Id,
    Guid CustomerId,
    string ReceiptReference,
    decimal TotalAmount,
    decimal AppliedAmount,
    decimal UnappliedAmount,
    string Status);

public record CashReceiptApplicationResponse(
    Guid Id,
    Guid CashReceiptId,
    Guid InvoiceId,
    decimal AppliedAmount);

public record WriteOffRequest(
    decimal Amount,
    string Reason,
    string? ApprovalToken);

public record CreateStandaloneInvoiceRequest(
    Guid CompanyId,
    Guid CustomerId,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    string? Description,
    Guid? PaymentTermId,
    Guid? ProjectId,
    Guid? SalesOrderId,
    IReadOnlyList<InvoiceLineItem> Lines);

public record CreateMemoRequest(
    Guid CompanyId,
    Guid CustomerId,
    string ReferenceNumber,
    DateTimeOffset MemoDate,
    int MemoType,
    Guid? InvoiceId,
    string? Description,
    IReadOnlyList<InvoiceLineItem> Lines);

public record UpdateMemoRequest(
    int MemoType,
    Guid? InvoiceId,
    string? Description);

public record MemoResponse(
    Guid Id,
    Guid CustomerId,
    string ReferenceNumber,
    DateTimeOffset MemoDate,
    string MemoType,
    string Status,
    decimal TotalAmount,
    string? Description);

public record StatementResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string StatementNumber,
    DateTimeOffset AsOfDate,
    string Status,
    decimal TotalDue);
