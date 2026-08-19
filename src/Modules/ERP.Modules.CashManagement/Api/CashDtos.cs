// <copyright file="CashDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.CashManagement.Api;

public record CreateBankAccountRequest(
    Guid CompanyId,
    string AccountCode,
    string AccountName,
    string AccountNumber,
    string? RoutingNumber,
    string? BankName,
    string? CurrencyCode,
    int AccountType,
    decimal OpeningBalance,
    Guid? GlAccountId);

public record UpdateBankAccountRequest(
    string AccountName,
    string AccountNumber,
    string? RoutingNumber,
    string? BankName,
    string? CurrencyCode,
    int AccountType,
    Guid? GlAccountId);

public record BankContactRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Title);

public record BankAccountResponse(
    Guid Id,
    Guid CompanyId,
    string AccountCode,
    string AccountName,
    string AccountNumber,
    string? RoutingNumber,
    string? BankName,
    string CurrencyCode,
    string AccountType,
    decimal OpeningBalance,
    decimal CurrentBalance,
    Guid? GlAccountId,
    string Status);

public record BankAccountDetailResponse(
    BankAccountResponse Account,
    IReadOnlyList<BankContactResponse> Contacts);

public record BankContactResponse(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Title);

public record CreateDepositRequest(
    Guid CompanyId,
    Guid BankAccountId,
    string DepositNumber,
    DateTimeOffset DepositDate,
    string? Reference,
    IReadOnlyList<DepositLineItem> Lines);

public record DepositLineItem(
    int Source,
    Guid? SourceReferenceId,
    decimal Amount,
    string? Description);

public record CreateDepositFromArRequest(
    Guid CompanyId,
    Guid BankAccountId,
    string DepositNumber,
    DateTimeOffset DepositDate,
    Guid CashReceiptId);

public record DepositResponse(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    string DepositNumber,
    DateTimeOffset DepositDate,
    string? Reference,
    string Status,
    decimal TotalAmount);

public record DepositDetailResponse(
    DepositResponse Deposit,
    IReadOnlyList<DepositLineDetailResponse> Lines);

public record DepositLineDetailResponse(
    Guid Id,
    string Source,
    Guid? SourceReferenceId,
    decimal Amount,
    string? Description);

public record ImportStatementRequest(
    Guid CompanyId,
    Guid BankAccountId,
    string StatementNumber,
    DateTimeOffset StatementDate,
    string FileContent,
    int? Format);

public record ImportStatementResponse(
    Guid BankStatementId,
    string StatementNumber,
    string Format,
    int LineCount,
    decimal? BeginningBalance,
    decimal? EndingBalance,
    IReadOnlyList<string> Warnings);

public record BankStatementResponse(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    string StatementNumber,
    DateTimeOffset StatementDate,
    decimal BeginningBalance,
    decimal EndingBalance,
    string Format,
    string Status,
    int LineCount);

public record BankStatementLineResponse(
    Guid Id,
    DateTimeOffset TransactionDate,
    decimal Amount,
    string Description,
    string? ReferenceNumber,
    string? CheckNumber,
    decimal Balance,
    string Status,
    Guid? MatchedTransactionId,
    string? MatchedSource);

public record BankStatementDetailResponse(
    BankStatementResponse Statement,
    IReadOnlyList<BankStatementLineResponse> Lines);

public record CreateReconciliationSessionRequest(
    string SessionNumber,
    string CreatedBy);

public record CreateReconciliationSessionResponse(
    Guid SessionId,
    string SessionNumber,
    decimal BeginningBalance,
    decimal EndingBalance);

public record MarkLineMatchedRequest(
    Guid StatementLineId,
    Guid TransactionId,
    int Source,
    string? ClearedBy);

public record MarkLineClearedRequest(
    Guid StatementLineId,
    string? ClearedBy);

public record MarkLineUnmatchedRequest(
    Guid StatementLineId,
    string? ClearedBy);

public record LockReconciliationRequest(
    Guid VarianceGlAccountId,
    decimal Tolerance,
    string? LockedBy);

public record ReconciliationSessionResponse(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    Guid BankStatementId,
    string SessionNumber,
    DateTimeOffset StatementDate,
    decimal BeginningBalance,
    decimal EndingBalance,
    decimal? Variance,
    Guid? GlJournalBatchId,
    string Status);

public record AutoMatchLineResponse(
    Guid StatementLineId,
    decimal StatementAmount,
    AutoMatchCandidateResponse? Candidate,
    int Score,
    string Confidence);

public record AutoMatchCandidateResponse(
    Guid Id,
    string Source,
    string Reference,
    decimal Amount,
    DateTimeOffset Date,
    string? CheckNumber,
    string? Description);

public record CreateBankTransferRequest(
    Guid CompanyId,
    Guid FromBankAccountId,
    Guid ToBankAccountId,
    string TransferNumber,
    decimal Amount,
    DateTimeOffset TransferDate,
    string? Reference);

public record BankTransferResponse(
    Guid Id,
    Guid CompanyId,
    Guid FromBankAccountId,
    Guid ToBankAccountId,
    string TransferNumber,
    decimal Amount,
    DateTimeOffset TransferDate,
    string? Reference,
    string Status);

public record ProcessNsfRequest(
    Guid CompanyId,
    Guid BankAccountId,
    Guid CashReceiptId,
    string NsfNumber,
    decimal Amount,
    DateTimeOffset ReturnedDate,
    string? BankReference,
    string? Reason,
    decimal? NsfFeeAmount,
    string? ProcessedBy);

public record NsfResponse(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    Guid? CashReceiptId,
    Guid? CustomerId,
    string NsfNumber,
    decimal Amount,
    DateTimeOffset ReturnedDate,
    string? BankReference,
    string? Reason,
    decimal? NsfFeeAmount,
    string Status);

public record RecordBankFeeRequest(
    Guid CompanyId,
    Guid BankAccountId,
    string FeeNumber,
    int FeeType,
    decimal Amount,
    DateTimeOffset FeeDate,
    string? Description,
    Guid ExpenseGlAccountId,
    string? PostedBy);

public record BankFeeResponse(
    Guid Id,
    Guid CompanyId,
    Guid BankAccountId,
    string FeeNumber,
    string FeeType,
    decimal Amount,
    DateTimeOffset FeeDate,
    string? Description,
    Guid? GlJournalBatchId,
    string Status);

public record CashPositionResponse(
    Guid BankAccountId,
    string AccountCode,
    string AccountName,
    string AccountNumber,
    decimal CurrentBalance,
    string CurrencyCode,
    int OutstandingChecks,
    int OutstandingDeposits);

public record OutstandingCheckBucketResponse(
    string Bucket,
    decimal Amount,
    int CheckCount);

public record OutstandingCheckAgingResponse(
    Guid BankAccountId,
    string AccountName,
    DateTimeOffset AsOfDate,
    IReadOnlyList<OutstandingCheckBucketResponse> Buckets);

public record ReconciliationSummaryResponse(
    Guid BankAccountId,
    string AccountName,
    DateTimeOffset StatementDate,
    decimal BeginningBalance,
    decimal EndingBalance,
    decimal ClearedDeposits,
    decimal ClearedWithdrawals,
    decimal OutstandingChecks,
    decimal OutstandingDeposits,
    decimal Variance,
    string Status);

public record ReconciliationDetailLineResponse(
    DateTimeOffset TransactionDate,
    decimal Amount,
    string Description,
    string? CheckNumber,
    string Status,
    Guid? MatchedTransactionId,
    string? MatchedSource);

public record ReconciliationDetailResponse(
    Guid BankAccountId,
    string AccountName,
    string StatementNumber,
    DateTimeOffset StatementDate,
    decimal BeginningBalance,
    decimal EndingBalance,
    decimal Variance,
    string Status,
    IReadOnlyList<ReconciliationDetailLineResponse> Lines);
