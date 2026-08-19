// <copyright file="Phase5Dtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;

namespace ERP.Modules.CashManagement.Api;

public record BankGlMappingDto(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    string? BankAccountName,
    Guid GlAccountId,
    bool IsDefault);

public record CreateBankGlMappingRequest(
    Guid CompanyId,
    Guid BankAccountId,
    Guid GlAccountId,
    bool IsDefault);

public record UpdateBankGlMappingRequest(
    Guid GlAccountId,
    bool IsDefault);

public record LockboxBatchDto(
    Guid Id,
    Guid CompanyId,
    string BatchNumber,
    string FileName,
    string Format,
    DateTimeOffset ImportedOn,
    string Status,
    int TotalItems,
    decimal TotalAmount,
    IReadOnlyList<LockboxItemDto> Items);

public record LockboxItemDto(
    Guid Id,
    string ReferenceNumber,
    Guid? CustomerId,
    string CustomerName,
    decimal Amount,
    DateTimeOffset? RemittanceDate,
    string? InvoiceNumber,
    bool ReceiptCreated);

public record CreateLockboxBatchRequest(
    Guid CompanyId,
    string BatchNumber,
    string FileName,
    string Format,
    IReadOnlyList<CreateLockboxItemRequest> Items);

public record CreateLockboxItemRequest(
    string ReferenceNumber,
    Guid? CustomerId,
    string CustomerName,
    decimal Amount,
    DateTimeOffset? RemittanceDate,
    string? InvoiceNumber);

public record StaleCheckEscheatmentDto(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    Guid? CheckId,
    string CheckNumber,
    decimal Amount,
    DateTimeOffset IssueDate,
    string Payee,
    string State,
    string Status,
    DateTimeOffset? EscheatedOn,
    DateTimeOffset? ReissuedOn);

public record CreateStaleCheckEscheatmentRequest(
    Guid CompanyId,
    Guid BankAccountId,
    Guid? CheckId,
    string CheckNumber,
    decimal Amount,
    DateTimeOffset IssueDate,
    string Payee,
    string State);

public record PositivePayExceptionDto(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    string CheckNumber,
    decimal Amount,
    DateTimeOffset IssueDate,
    string Decision,
    string DecisionReason,
    DateTimeOffset ReceivedOn,
    DateTimeOffset? DecidedOn);

public record CreatePositivePayExceptionRequest(
    Guid CompanyId,
    Guid BankAccountId,
    string CheckNumber,
    decimal Amount,
    DateTimeOffset IssueDate,
    string DecisionReason);

public record DecidePositivePayRequest(
    string Decision,
    string DecisionReason);

public record BankDuplicateLineDto(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    string CheckNumber,
    decimal Amount,
    DateTimeOffset TransactionDate,
    Guid StatementLineId,
    Guid StatementId,
    DateTimeOffset DetectedOn,
    bool Resolved);

public record BankFeeAnalysisDto(
    Guid Id,
    Guid CompanyId,
    int Year,
    int Month,
    DateTimeOffset GeneratedOn,
    decimal TotalFees,
    IReadOnlyList<BankFeeAnalysisLineDto> Lines);

public record BankFeeAnalysisLineDto(
    Guid Id,
    string FeeType,
    Guid? BankAccountId,
    decimal Amount,
    int Count);

public record CashForecastHorizonResponse(
    decimal TodayCash,
    decimal Next7DayCash,
    decimal Next30DayCash,
    decimal OpenPayablesNext7,
    decimal OpenReceivablesNext7,
    decimal OpenPayablesNext30,
    decimal OpenReceivablesNext30);

public record OutstandingDepositDto(
    Guid BankAccountId,
    string AccountName,
    decimal OutstandingDepositAmount,
    int DepositCount);

public record OutstandingDepositsResponse(
    IReadOnlyList<OutstandingDepositDto> Accounts,
    decimal TotalOutstandingDeposits);
