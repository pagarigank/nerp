// <copyright file="IConsolidationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public interface IConsolidationService
{
    Task<ConsolidationRun> CreateConsolidationRunAsync(
        Guid parentCompanyId,
        DateTimeOffset consolidationDate,
        int fiscalYear,
        int fiscalPeriod,
        string description,
        CancellationToken cancellationToken = default);

    Task<ConsolidationRun> ExecuteConsolidationAsync(
        Guid consolidationRunId,
        CancellationToken cancellationToken = default);

    Task<ConsolidationRun?> GetConsolidationRunAsync(
        Guid consolidationRunId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsolidationRun>> GetConsolidationRunsAsync(
        Guid parentCompanyId,
        CancellationToken cancellationToken = default);

    Task<IntercompanyMapping> CreateIntercompanyMappingAsync(
        Guid fromCompanyId,
        Guid toCompanyId,
        string fromAccountNumber,
        string toAccountNumber,
        string description,
        CancellationToken cancellationToken = default);

    Task<IntercompanyMapping> UpdateIntercompanyMappingAsync(
        Guid mappingId,
        string fromAccountNumber,
        string toAccountNumber,
        string description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntercompanyMapping>> GetIntercompanyMappingsAsync(
        Guid? fromCompanyId = null,
        Guid? toCompanyId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task DeleteIntercompanyMappingAsync(Guid mappingId, CancellationToken cancellationToken = default);
}