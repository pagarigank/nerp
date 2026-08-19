// <copyright file="VoucherDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Api;

public record VoucherBatchDto(
    Guid Id,
    Guid CompanyId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    Guid FiscalPeriodId,
    VoucherBatchStatus Status,
    IReadOnlyList<VoucherDto> Vouchers,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record VoucherDto(
    Guid Id,
    Guid VoucherBatchId,
    Guid VendorId,
    VoucherType VoucherType,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    decimal TotalAmount,
    decimal DiscountAmount,
    string Description,
    Guid? PaymentTermId,
    Guid? PurchaseOrderId,
    Guid? ReceiptLineId,
    decimal Form1099Amount,
    decimal BackupWithholdingAmount,
    bool Is1099Reportable,
    bool SelectedForPayment,
    IReadOnlyList<VoucherDistributionDto> Distributions);

public record VoucherDistributionDto(
    Guid Id,
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    Guid? ProjectId,
    Guid? TaskId);

public record CreateVoucherBatchRequest(
    Guid CompanyId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    Guid FiscalPeriodId);

public record AddVoucherToBatchRequest(
    Guid VendorId,
    VoucherType VoucherType,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    decimal TotalAmount,
    decimal DiscountAmount,
    string? Description,
    Guid? PaymentTermId,
    Guid? PurchaseOrderId,
    Guid? ReceiptLineId,
    decimal Form1099Amount,
    decimal BackupWithholdingAmount,
    IReadOnlyList<CreateVoucherDistributionRequest> Distributions);

public record CreateVoucherDistributionRequest(
    Guid AccountId,
    decimal? Debit,
    decimal? Credit,
    Guid? ProjectId,
    Guid? TaskId);

public record ReleaseBatchRequest(
    string? PerformedBy);

public record ReverseBatchRequest(
    string Reason);