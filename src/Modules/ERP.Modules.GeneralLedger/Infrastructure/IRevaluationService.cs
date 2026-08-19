// <copyright file="IRevaluationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public interface IRevaluationService
{
    Task<RevaluationResult> RevalueAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        DateTimeOffset revaluationDate,
        string revaluationReason,
        CancellationToken cancellationToken = default);

    Task<RevaluationPreviewDto> PreviewRevaluationAsync(
        Guid companyId,
        Guid fiscalPeriodId,
        DateTimeOffset revaluationDate,
        CancellationToken cancellationToken = default);
}

public record RevaluationResult(
    JournalBatch RevaluationBatch,
    int LinesRevalued,
    decimal TotalGainLoss);

public record RevaluationPreviewDto(
    int LinesToRevalue,
    decimal EstimatedGainLoss,
    IReadOnlyList<RevaluationLinePreview> Lines);

public record RevaluationLinePreview(
    Guid AccountId,
    string AccountNumber,
    decimal OriginalDebit,
    decimal OriginalCredit,
    decimal RevaluedDebit,
    decimal RevaluedCredit,
    decimal GainLoss);