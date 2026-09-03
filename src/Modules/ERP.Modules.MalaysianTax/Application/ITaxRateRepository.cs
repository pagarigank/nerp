// <copyright file="ITaxRateRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Repository interface for TaxRate entity.
/// </summary>
public interface ITaxRateRepository
{
    Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaxRate?> GetActiveRateByCodeAsync(Guid taxCodeId, DateTime? asOfDate = null, CancellationToken ct = default);
    Task<TaxRate?> GetRateForPeriodAsync(Guid taxCodeId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<TaxRate>> GetRatesByCodeAsync(Guid taxCodeId, CancellationToken ct = default);
    Task AddAsync(TaxRate entity, CancellationToken ct = default);
    Task UpdateAsync(TaxRate entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}