// <copyright file="VendorDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Api;

public record VendorDto(
    Guid Id,
    string VendorId,
    string Name,
    string? LegalName,
    string? TaxId,
    Vendor1099Category? Form1099Category,
    Guid? DefaultPaymentTermId,
    bool IsActive,
    bool BackupWithholdingFlag,
    decimal BackupWithholdingRate,
    IReadOnlyList<VendorBankAccountDto> BankAccounts,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record VendorBankAccountDto(
    Guid Id,
    string BankName,
    string AccountNumber,
    string? RoutingNumber,
    bool IsDefault);

public record CreateVendorRequest(
    string VendorId,
    string Name,
    string? LegalName,
    string? TaxId,
    Vendor1099Category? Form1099Category,
    Guid? DefaultPaymentTermId,
    bool IsActive,
    bool BackupWithholdingFlag,
    decimal BackupWithholdingRate,
    IReadOnlyList<CreateVendorBankAccountRequest> BankAccounts);

public record CreateVendorBankAccountRequest(
    string BankName,
    string AccountNumber,
    string? RoutingNumber,
    bool IsDefault);

public record UpdateVendorRequest(
    string Name,
    string? LegalName,
    string? TaxId,
    Vendor1099Category? Form1099Category,
    Guid? DefaultPaymentTermId,
    bool BackupWithholdingFlag,
    decimal BackupWithholdingRate);

public record PaymentTermDto(
    Guid Id,
    string Name,
    int DueDays,
    int DiscountDays,
    decimal DiscountPercent,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreatePaymentTermRequest(
    string Name,
    int DueDays,
    int DiscountDays,
    decimal DiscountPercent);

public record UpdatePaymentTermRequest(
    string Name,
    int DueDays,
    int DiscountDays,
    decimal DiscountPercent);