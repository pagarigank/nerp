// <copyright file="IExemptSupplyCategoryRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Repository interface for ExemptSupplyCategory entity.
/// </summary>
public interface IExemptSupplyCategoryRepository
{
    Task<ExemptSupplyCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ExemptSupplyCategory>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<ExemptSupplyCategory?> GetByCategoryCodeAsync(Guid companyId, string categoryCode, CancellationToken ct = default);
    Task<IEnumerable<ExemptSupplyCategory>> GetEffectiveOnDateAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default);
    Task AddAsync(ExemptSupplyCategory entity, CancellationToken ct = default);
    Task UpdateAsync(ExemptSupplyCategory entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
