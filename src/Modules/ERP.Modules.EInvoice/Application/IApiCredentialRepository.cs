// <copyright file="IApiCredentialRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain.Entities;

namespace ERP.Modules.EInvoice.Application;

/// <summary>
/// Repository interface for ApiCredential entity.
/// </summary>
public interface IApiCredentialRepository
{
    Task<ApiCredential?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiCredential?> GetActiveByCompanyAndEnvironmentAsync(Guid companyId, string environment, CancellationToken ct = default);
    Task<IEnumerable<ApiCredential>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task AddAsync(ApiCredential entity, CancellationToken ct = default);
    Task UpdateAsync(ApiCredential entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
