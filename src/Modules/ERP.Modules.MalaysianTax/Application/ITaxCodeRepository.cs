// <copyright file="ITaxCodeRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Repository interface for TaxCode entity.
/// </summary>
public interface ITaxCodeRepository
{
    Task<TaxCode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaxCode?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<TaxCode>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(TaxCode entity, CancellationToken ct = default);
    Task UpdateAsync(TaxCode entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}