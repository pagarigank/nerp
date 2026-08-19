// <copyright file="GlReportDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Api;

public record TrialBalanceLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    AccountType AccountType,
    NormalBalance NormalBalance,
    decimal BeginningBalance,
    decimal Debit,
    decimal Credit,
    decimal EndingBalance);

public record TrialBalanceReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid? FiscalPeriodId,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<TrialBalanceLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record GeneralLedgerDetailLineDto(
    Guid BatchId,
    string BatchNumber,
    DateTimeOffset PostingDate,
    string? Reference,
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    decimal Debit,
    decimal Credit,
    string? SegmentsJson);

public record GeneralLedgerDetailReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid? FiscalPeriodId,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<GeneralLedgerDetailLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record UnpostedTransactionDto(
    Guid BatchId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    string Status,
    int LineCount,
    decimal TotalDebits,
    decimal TotalCredits,
    DateTimeOffset CreatedOn);

public record UnpostedTransactionsReportDto(
    Guid CompanyId,
    string CompanyName,
    IReadOnlyList<UnpostedTransactionDto> Batches,
    DateTimeOffset GeneratedOn);

public record FinancialStatementLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    decimal Balance);

public record FinancialStatementReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid? FiscalPeriodId,
    string StatementType,
    decimal TotalAmount,
    IReadOnlyList<FinancialStatementLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record CashFlowLineDto(
    string Category,
    Guid? AccountId,
    string AccountNumber,
    string AccountDescription,
    decimal Amount);

public record CashFlowReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid? FiscalPeriodId,
    decimal NetCashOperating,
    decimal NetCashInvesting,
    decimal NetCashFinancing,
    decimal NetCashChange,
    IReadOnlyList<CashFlowLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record BudgetVsActualLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    decimal BudgetAmount,
    decimal ActualAmount,
    decimal Variance,
    decimal VariancePercent);

public record BudgetVsActualReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid BudgetId,
    string BudgetName,
    Guid? FiscalPeriodId,
    decimal TotalBudget,
    decimal TotalActual,
    decimal TotalVariance,
    IReadOnlyList<BudgetVsActualLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record AccountDistributionLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    AccountType AccountType,
    decimal Debit,
    decimal Credit,
    decimal NetChange,
    int TransactionCount);

public record AccountDistributionReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid? FiscalPeriodId,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<AccountDistributionLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record ConsolidatedTrialBalanceLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    AccountType AccountType,
    NormalBalance NormalBalance,
    decimal BeginningBalance,
    decimal Debit,
    decimal Credit,
    decimal EndingBalance,
    Guid CompanyId,
    string CompanyName);

public record ConsolidatedTrialBalanceReportDto(
    Guid ParentCompanyId,
    string ParentCompanyName,
    Guid? FiscalPeriodId,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<ConsolidatedTrialBalanceLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record IntercompanyBalanceLineDto(
    Guid FromCompanyId,
    string FromCompanyName,
    Guid ToCompanyId,
    string ToCompanyName,
    string FromAccountNumber,
    string ToAccountNumber,
    decimal Balance);

public record IntercompanyBalanceReportDto(
    Guid ParentCompanyId,
    string ParentCompanyName,
    Guid? FiscalPeriodId,
    IReadOnlyList<IntercompanyBalanceLineDto> Lines,
    DateTimeOffset GeneratedOn);

public record MultiCurrencyRevaluationLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    string Currency,
    decimal OriginalBalance,
    decimal RevaluedBalance,
    decimal GainLoss);

public record MultiCurrencyRevaluationReportDto(
    Guid CompanyId,
    string CompanyName,
    Guid FiscalPeriodId,
    DateTimeOffset RevaluationDate,
    decimal TotalGainLoss,
    IReadOnlyList<MultiCurrencyRevaluationLineDto> Lines,
    DateTimeOffset GeneratedOn);
