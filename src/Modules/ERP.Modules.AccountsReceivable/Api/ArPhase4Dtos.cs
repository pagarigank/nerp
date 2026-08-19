// <copyright file="ArPhase4Dtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;

namespace ERP.Modules.AccountsReceivable.Api;

// Collection Notes
public record CollectionNoteDto(
    Guid Id,
    Guid CompanyId,
    Guid CustomerId,
    string Note,
    string Author,
    CollectionNoteType Type,
    CollectionNoteStatus Status,
    Guid? AssignedTo,
    DateTimeOffset? FollowUpDate,
    DateTimeOffset? PromiseToPayDate,
    string? RelatedDocumentNumber);

public record CreateCollectionNoteRequest(
    Guid CompanyId,
    Guid CustomerId,
    string Note,
    string Author,
    CollectionNoteType Type,
    Guid? AssignedTo,
    DateTimeOffset? FollowUpDate,
    DateTimeOffset? PromiseToPayDate,
    string? RelatedDocumentNumber);

public record AddCollectionNoteActivityRequest(
    string Author,
    string Description,
    CollectionNoteActivityType ActivityType,
    DateTimeOffset? PromiseToPayDate);

public record AssignCollectionNoteRequest(Guid? AssignedTo);

public record CloseCollectionNoteRequest(string Author);

// Collections Dashboard
public record CollectionsDashboardDto(
    Guid CompanyId,
    int OpenNotes,
    int PromisesToPay,
    IReadOnlyList<CollectionsQueueItemDto> FollowUpQueue,
    decimal TotalOutstanding);

public record CollectionsQueueItemDto(
    Guid NoteId,
    Guid CustomerId,
    DateTimeOffset FollowUpDate,
    DateTimeOffset? PromiseToPayDate,
    CollectionNoteType Type,
    Guid? AssignedTo);

// Dunning
public record DunningTemplateDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Subject,
    string Body,
    int Sequence,
    DunningAgingBucket Bucket,
    int MinDaysOverdue,
    int MaxDaysOverdue,
    bool SendEmail,
    bool SendPdf,
    bool IsActive);

public record CreateDunningTemplateRequest(
    Guid CompanyId,
    string Name,
    string Subject,
    string Body,
    int Sequence,
    DunningAgingBucket Bucket,
    int MinDaysOverdue,
    int MaxDaysOverdue,
    bool SendEmail,
    bool SendPdf);

public record UpdateDunningTemplateRequest(
    string Name,
    string Subject,
    string Body,
    int Sequence,
    DunningAgingBucket Bucket,
    int MinDaysOverdue,
    int MaxDaysOverdue,
    bool SendEmail,
    bool SendPdf,
    bool IsActive);

public record RunDunningRequest(Guid CompanyId, DateTimeOffset? AsOfDate);

public record DunningRunResultDto(
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    int LettersGenerated,
    IReadOnlyList<DunningLetterDto> Letters);

public record DunningLetterDto(
    Guid InvoiceId,
    Guid CustomerId,
    string CustomerName,
    string InvoiceNumber,
    int Sequence,
    DunningAgingBucket Bucket,
    string Subject,
    string Body,
    decimal BalanceDue,
    int DaysOverdue);

// Allowance
public record AllowanceRunDto(
    Guid Id,
    Guid CompanyId,
    DateTimeOffset AsOfDate,
    Guid ReserveAccountId,
    string Name,
    AllowanceMethod Method,
    string? Notes,
    AllowanceRunStatus Status,
    decimal TotalEstimatedAllowance,
    string? PostedBy,
    DateTimeOffset? PostedOn,
    IReadOnlyList<AllowanceBucketDto> Buckets);

public record AllowanceBucketDto(
    DunningAgingBucket Bucket,
    decimal OutstandingBalance,
    decimal ReserveRate,
    decimal EstimatedAmount);

public record CreateAllowanceRunRequest(
    Guid CompanyId,
    string Name,
    DateTimeOffset AsOfDate,
    AllowanceMethod Method,
    decimal PercentageOfReceivables,
    decimal AgingRateCurrent,
    decimal AgingRate1To30,
    decimal AgingRate31To60,
    decimal AgingRate61To90,
    decimal AgingRateOver90,
    decimal SpecificAmount);

public record PostAllowanceRunRequest(string PostedBy);

public record AgingBucketBreakdownDto(DunningAgingBucket Bucket)
{
    public decimal Outstanding { get; set; }
}

public record ArAgingByBasisReportDto(
    Guid CompanyId,
    string Basis,
    DateTimeOffset AsOfDate,
    IReadOnlyList<AgingBucketBreakdownDto> AgingBreakdown,
    decimal TotalOutstanding,
    DateTimeOffset GeneratedOn);

// Resale Certificates
public record ResaleCertificateDto(
    Guid Id,
    Guid CompanyId,
    Guid CustomerId,
    string CertificateNumber,
    string IssuedState,
    DateTimeOffset IssueDate,
    DateTimeOffset ExpiryDate,
    string? DocumentReference,
    bool IsActive,
    bool IsExpired);

public record CreateResaleCertificateRequest(
    Guid CompanyId,
    Guid CustomerId,
    string CertificateNumber,
    string IssuedState,
    DateTimeOffset IssueDate,
    DateTimeOffset ExpiryDate,
    string? DocumentReference);

public record UpdateResaleCertificateRequest(
    string CertificateNumber,
    string IssuedState,
    DateTimeOffset IssueDate,
    DateTimeOffset ExpiryDate,
    string? DocumentReference,
    bool IsActive);

// Credit Memo Application
public record ApplyCreditMemoRequest(IReadOnlyList<Guid>? TargetInvoiceIds);

public record CreditMemoApplyResultDto(
    Guid CreditMemoId,
    decimal TotalAmount,
    IReadOnlyList<Guid> AppliedInvoiceIds);

// Cash Receipt matching by reference
public record CashReceiptReferenceMatchDto(
    Guid ReceiptId,
    string ReferenceNumber,
    int MatchedInvoices,
    decimal AppliedAmount,
    IReadOnlyList<Guid> AppliedInvoiceIds);
