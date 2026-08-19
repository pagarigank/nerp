// <copyright file="IAchFileService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IAchFileService
{
    Task<AchFileGenerationResult> GenerateAchFileAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<AchFileGenerationResult> GenerateBatchAchFileAsync(
        IReadOnlyList<Guid> paymentIds,
        CancellationToken cancellationToken = default);

    Task<string> GetAchFileContentAsync(string fileName, CancellationToken cancellationToken = default);
}

public record AchFileGenerationResult(
    string FileName,
    int RecordCount,
    decimal TotalAmount,
    string Content);
