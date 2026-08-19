// <copyright file="JournalBatchDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Api;

public record JournalBatchDto(
    Guid Id,
    Guid CompanyId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    Guid FiscalPeriodId,
    JournalBatchStatus Status,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn,
    IReadOnlyList<JournalEntryLineDto> Lines);

public record JournalEntryLineDto(
    Guid Id,
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string? Reference,
    string? SegmentsJson);

public record CreateJournalBatchRequest(
    Guid CompanyId,
    string BatchNumber,
    string Description,
    DateTimeOffset PostingDate,
    Guid FiscalPeriodId,
    IReadOnlyList<CreateJournalEntryLineRequest> Lines);

public record CreateJournalEntryLineRequest(
    Guid AccountId,
    decimal? Debit,
    decimal? Credit,
    string? Reference,
    string? SegmentsJson);

public record AddLineToBatchRequest(
    Guid AccountId,
    decimal? Debit,
    decimal? Credit,
    string? Reference,
    string? SegmentsJson);

public record PostBatchRequest(
    string? PerformedBy);

public record ReverseBatchRequest(
    string Reason);

public record GenerateFromRecurringRequest(
    string BatchNumber,
    Guid FiscalPeriodId,
    DateTimeOffset PostingDate);
