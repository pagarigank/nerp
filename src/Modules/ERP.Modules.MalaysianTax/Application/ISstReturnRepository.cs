// <copyright file="ISstReturnRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Repository for SST return persistence.
/// </summary>
public interface ISstReturnRepository
{
    Task<SstReturn?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<SstReturn>> GetAllByCompanyAsync(
        Guid companyId, CancellationToken ct = default);

    Task<SstReturn?> GetByPeriodAsync(
        Guid companyId, DateTime periodStart, string returnType, CancellationToken ct = default);

    Task<SstReturn> CreateAsync(SstReturn entity, CancellationToken ct = default);

    Task<SstReturn> UpdateAsync(SstReturn entity, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
