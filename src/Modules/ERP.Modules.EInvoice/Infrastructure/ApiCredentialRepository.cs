// <copyright file="ApiCredentialRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Repository implementation for ApiCredential entity.
/// </summary>
public class ApiCredentialRepository : IApiCredentialRepository
{
    private readonly EInvoiceDbContext _context;
    private readonly ILogger<ApiCredentialRepository> _logger;

    public ApiCredentialRepository(EInvoiceDbContext context, ILogger<ApiCredentialRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiCredential?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ApiCredentials.FindAsync([id], ct);
    }

    public async Task<ApiCredential?> GetActiveByCompanyAndEnvironmentAsync(Guid companyId, string environment, CancellationToken ct = default)
    {
        return await _context.ApiCredentials
            .FirstOrDefaultAsync(c => c.CompanyId == companyId
                && c.Environment == environment
                && c.IsActive, ct);
    }

    public async Task<IEnumerable<ApiCredential>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _context.ApiCredentials
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Environment)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ApiCredential entity, CancellationToken ct = default)
    {
        // Only one active credential per company per environment
        var existing = await _context.ApiCredentials
            .FirstOrDefaultAsync(c => c.CompanyId == entity.CompanyId
                && c.Environment == entity.Environment
                && c.IsActive, ct);

        if (existing != null)
        {
            _logger.LogWarning("Active credential already exists for CompanyId {CompanyId}, Env {Env}. Deactivating.", entity.CompanyId, entity.Environment);
            existing.Deactivate();
        }

        await _context.ApiCredentials.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Added ApiCredential for CompanyId {CompanyId}, Env {Env}", entity.CompanyId, entity.Environment);
    }

    public async Task UpdateAsync(ApiCredential entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.ApiCredentials.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated ApiCredential {Id}", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.ApiCredentials.FindAsync([id], ct);
        if (entity is null) return;

        entity.Deactivate();
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Deactivated ApiCredential {Id}", id);
    }
}
