// <copyright file="IVoucherService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IVoucherService
{
    Task<VoucherBatch> CreateVoucherBatchAsync(
        Guid companyId,
        string batchNumber,
        string description,
        DateTimeOffset postingDate,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken = default);

    Task<Voucher> AddVoucherToBatchAsync(
        Guid batchId,
        Guid vendorId,
        VoucherType voucherType,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset dueDate,
        decimal totalAmount,
        decimal discountAmount,
        string? description,
        Guid? paymentTermId,
        Guid? purchaseOrderId,
        Guid? receiptLineId,
        decimal form1099Amount,
        decimal backupWithholdingAmount,
        IReadOnlyList<VoucherDistributionDto> distributions,
        CancellationToken cancellationToken = default);

    Task<VoucherBatch> ReleaseBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<VoucherBatch> PostBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<VoucherBatch> ReverseBatchAsync(Guid batchId, string reason, CancellationToken cancellationToken = default);

    Task<Payment> CreatePaymentAsync(
        Guid companyId,
        Guid vendorId,
        string paymentReference,
        DateTimeOffset paymentDate,
        PaymentMethod paymentMethod,
        string currencyCode,
        Guid? bankAccountId,
        CancellationToken cancellationToken = default);

    Task<Payment> SelectVouchersForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> voucherIds,
        CancellationToken cancellationToken = default);

    Task<Payment> IssuePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<Payment> VoidPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default);
}

public record VoucherDistributionDto(
    Guid AccountId,
    decimal? Debit,
    decimal? Credit,
    Guid? ProjectId,
    Guid? TaskId);