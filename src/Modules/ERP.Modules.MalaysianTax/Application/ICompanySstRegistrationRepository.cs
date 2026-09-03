// <copyright file="ICompanySstRegistrationRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Repository interface for CompanySstRegistration entity.
/// </summary>
public interface ICompanySstRegistrationRepository
{
    Task<CompanySstRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CompanySstRegistration?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<IEnumerable<CompanySstRegistration>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(CompanySstRegistration entity, CancellationToken ct = default);
    Task UpdateAsync(CompanySstRegistration entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
