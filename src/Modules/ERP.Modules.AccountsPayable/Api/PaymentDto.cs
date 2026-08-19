// <copyright file="PaymentDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Api;

public record PaymentDto(
    Guid Id,
    Guid CompanyId,
    Guid VendorId,
    string PaymentReference,
    DateTimeOffset PaymentDate,
    PaymentMethod PaymentMethod,
    string CurrencyCode,
    Guid? BankAccountId,
    PaymentStatus Status,
    decimal TotalAmount,
    IReadOnlyList<PaymentLineDto> Lines,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record PaymentLineDto(
    Guid Id,
    Guid PaymentId,
    Guid VoucherId,
    decimal AppliedAmount);

public record CreatePaymentRequest(
    Guid CompanyId,
    Guid VendorId,
    string PaymentReference,
    DateTimeOffset PaymentDate,
    PaymentMethod PaymentMethod,
    string CurrencyCode,
    Guid? BankAccountId);

public record SelectVouchersForPaymentRequest(
    IReadOnlyList<Guid> VoucherIds);

public record IssuePaymentRequest(
    string? PerformedBy);

public record VoidPaymentRequest(
    string Reason);