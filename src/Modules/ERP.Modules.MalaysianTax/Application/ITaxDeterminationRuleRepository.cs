// <copyright file="ITaxDeterminationRuleRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Repository interface for TaxDeterminationRule entity.
/// </summary>
public interface ITaxDeterminationRuleRepository
{
    Task<TaxDeterminationRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<TaxDeterminationRule>> GetAllActiveAsync(Guid companyId, CancellationToken ct = default);
    Task<IEnumerable<TaxDeterminationRule>> FindMatchingRulesAsync(
        Guid companyId,
        string itemCategory,
        string customerType,
        string placeOfSupply,
        DateTime asOfDate,
        CancellationToken ct = default);
    Task AddAsync(TaxDeterminationRule entity, CancellationToken ct = default);
    Task UpdateAsync(TaxDeterminationRule entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
