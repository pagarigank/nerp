// <copyright file="CompanySstRegistrationRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Repository implementation for CompanySstRegistration entity.
/// </summary>
public class CompanySstRegistrationRepository : ICompanySstRegistrationRepository
{
    private readonly MalaysianTaxDbContext _context;
    private readonly ILogger<CompanySstRegistrationRepository> _logger;

    public CompanySstRegistrationRepository(MalaysianTaxDbContext context, ILogger<CompanySstRegistrationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CompanySstRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.CompanySstRegistrations.FindAsync([id], ct);
    }

    public async Task<CompanySstRegistration?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _context.CompanySstRegistrations
            .FirstOrDefaultAsync(r => r.CompanyId == companyId && r.IsActive, ct);
    }

    public async Task<IEnumerable<CompanySstRegistration>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.CompanySstRegistrations
            .Where(r => r.IsActive)
            .OrderBy(r => r.CompanyId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CompanySstRegistration entity, CancellationToken ct = default)
    {
        // Only one active registration per company
        var existing = await _context.CompanySstRegistrations
            .FirstOrDefaultAsync(r => r.CompanyId == entity.CompanyId && r.IsActive, ct);
        if (existing != null)
        {
            _logger.LogWarning("Active SST registration already exists for CompanyId {CompanyId} (existing: {Id}). Updating instead.", entity.CompanyId, existing.Id);
            existing.UpdateProfile(
                entity.IsRegisteredForSalesTax,
                entity.SalesTaxRegistrationNumber,
                entity.SalesTaxRegistrationDate,
                entity.IsRegisteredForServiceTax,
                entity.ServiceTaxRegistrationNumber,
                entity.ServiceTaxRegistrationDate,
                entity.IsRegisteredForTourismTax,
                entity.TourismTaxRegistrationNumber,
                entity.RmcdPortalReference,
                entity.Notes);
            await _context.SaveChangesAsync(ct);
            return;
        }

        await _context.CompanySstRegistrations.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Added CompanySstRegistration for CompanyId {CompanyId}, SalesTax={SalesTax}, ServiceTax={ServiceTax}",
            entity.CompanyId, entity.IsRegisteredForSalesTax, entity.IsRegisteredForServiceTax);
    }

    public async Task UpdateAsync(CompanySstRegistration entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CompanySstRegistrations.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated CompanySstRegistration {Id} for CompanyId {CompanyId}", entity.Id, entity.CompanyId);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.CompanySstRegistrations.FindAsync([id], ct);
        if (entity is null) return;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted CompanySstRegistration {Id} for CompanyId {CompanyId}", id, entity.CompanyId);
    }
}
