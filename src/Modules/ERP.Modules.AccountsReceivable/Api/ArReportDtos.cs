// <copyright file="ArReportDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsReceivable.Api;

public record ArAgingLineDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    decimal CurrentBalance,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90Days,
    decimal TotalDue);

public record ArAgingReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    IReadOnlyList<ArAgingLineDto> Lines,
    decimal TotalCurrent,
    decimal TotalDue,
    DateTimeOffset GeneratedOn);

public record CustomerTrialBalanceLineDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    decimal BeginningBalance,
    decimal Debits,
    decimal Credits,
    decimal EndingBalance);

public record CustomerTrialBalanceReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    IReadOnlyList<CustomerTrialBalanceLineDto> Lines,
    decimal TotalBeginningBalance,
    decimal TotalEndingBalance,
    DateTimeOffset GeneratedOn);

public record CashReceiptsJournalLineDto(
    Guid ReceiptId,
    string ReceiptReference,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset ReceiptDate,
    decimal Amount,
    string PaymentMethod,
    string Status);

public record CashReceiptsJournalReportDto(
    Guid CompanyId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    IReadOnlyList<CashReceiptsJournalLineDto> Lines,
    decimal TotalAmount,
    int TotalReceipts,
    DateTimeOffset GeneratedOn);

public record SalesJournalLineDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset InvoiceDate,
    decimal Amount,
    string Status);

public record SalesJournalReportDto(
    Guid CompanyId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    IReadOnlyList<SalesJournalLineDto> Lines,
    decimal TotalAmount,
    int TotalInvoices,
    DateTimeOffset GeneratedOn);

public record FinanceChargeReportLineDto(
    Guid ChargeId,
    string ChargeNumber,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset ChargeDate,
    decimal Amount,
    decimal AnnualRate,
    string Status);

public record FinanceChargeReportDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    IReadOnlyList<FinanceChargeReportLineDto> Lines,
    decimal TotalCharges,
    DateTimeOffset GeneratedOn);
