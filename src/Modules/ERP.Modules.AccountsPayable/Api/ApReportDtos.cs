// <copyright file="ApReportDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsPayable.Api;

public record ApVendorAgingLineDto(
    Guid VendorId,
    string VendorCode,
    string VendorName,
    decimal CurrentBalance,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90Days,
    decimal TotalDue);

public record ApAgingReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    IReadOnlyList<ApVendorAgingLineDto> Lines,
    decimal TotalCurrent,
    decimal TotalDue,
    DateTimeOffset GeneratedOn);

public record VendorTrialBalanceLineDto(
    Guid VendorId,
    string VendorCode,
    string VendorName,
    decimal BeginningBalance,
    decimal Debits,
    decimal Credits,
    decimal EndingBalance);

public record VendorTrialBalanceReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    IReadOnlyList<VendorTrialBalanceLineDto> Lines,
    decimal TotalBeginningBalance,
    decimal TotalEndingBalance,
    DateTimeOffset GeneratedOn);

public record ApBatchRegisterLineDto(
    Guid BatchId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    string Status,
    int VoucherCount,
    decimal TotalAmount,
    decimal TotalDiscount);

public record ApBatchRegisterReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    IReadOnlyList<ApBatchRegisterLineDto> Lines,
    int TotalBatches,
    decimal GrandTotal,
    DateTimeOffset GeneratedOn);

public record CashRequirementLineDto(
    Guid VendorId,
    string VendorCode,
    string VendorName,
    Guid VoucherId,
    string InvoiceNumber,
    DateTimeOffset DueDate,
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal NetDue,
    bool PastDue);

public record CashRequirementsReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    int DaysAhead,
    IReadOnlyList<CashRequirementLineDto> Lines,
    decimal TotalDue,
    decimal TotalPastDue,
    decimal GrandTotal,
    DateTimeOffset GeneratedOn);

public record Form1099ReportLineDto(
    Guid VendorId,
    string VendorCode,
    string VendorName,
    string? TaxId,
    string Category,
    decimal TotalPayments,
    decimal BackupWithholding);

public record Form1099ReportDto(
    Guid CompanyId,
    int TaxYear,
    IReadOnlyList<Form1099ReportLineDto> Lines,
    decimal TotalPayments,
    decimal TotalBackupWithholding,
    DateTimeOffset GeneratedOn);

public record CheckRegisterLineDto(
    Guid PaymentId,
    string PaymentReference,
    Guid VendorId,
    string VendorName,
    DateTimeOffset PaymentDate,
    string PaymentMethod,
    decimal Amount,
    string Status);

public record CheckRegisterReportDto(
    Guid CompanyId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    IReadOnlyList<CheckRegisterLineDto> Lines,
    decimal TotalAmount,
    int TotalChecks,
    DateTimeOffset GeneratedOn);

public record ApAccountDistributionLineDto(
    Guid AccountId,
    string AccountNumber,
    string AccountDescription,
    decimal Debit,
    decimal Credit,
    int TransactionCount);

public record ApAccountDistributionReportDto(
    Guid CompanyId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    IReadOnlyList<ApAccountDistributionLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    DateTimeOffset GeneratedOn);
