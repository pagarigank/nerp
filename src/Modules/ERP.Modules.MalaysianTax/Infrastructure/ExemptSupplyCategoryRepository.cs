// <copyright file="ExemptSupplyCategoryRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Repository implementation for ExemptSupplyCategory entity.
/// </summary>
public class ExemptSupplyCategoryRepository : IExemptSupplyCategoryRepository
{
    private readonly MalaysianTaxDbContext _context;
    private readonly ILogger<ExemptSupplyCategoryRepository> _logger;

    public ExemptSupplyCategoryRepository(MalaysianTaxDbContext context, ILogger<ExemptSupplyCategoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExemptSupplyCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ExemptSupplyCategories.FindAsync([id], ct);
    }

    public async Task<IEnumerable<ExemptSupplyCategory>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _context.ExemptSupplyCategories
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .OrderBy(e => e.CategoryCode)
            .ToListAsync(ct);
    }

    public async Task<ExemptSupplyCategory?> GetByCategoryCodeAsync(Guid companyId, string categoryCode, CancellationToken ct = default)
    {
        return await _context.ExemptSupplyCategories
            .FirstOrDefaultAsync(e => e.CompanyId == companyId
                && e.CategoryCode == categoryCode
                && e.IsActive,
                ct);
    }

    public async Task<IEnumerable<ExemptSupplyCategory>> GetEffectiveOnDateAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default)
    {
        return await _context.ExemptSupplyCategories
            .Where(e => e.CompanyId == companyId
                && e.IsActive
                && (!e.EffectiveFrom.HasValue || e.EffectiveFrom <= asOfDate)
                && (!e.EffectiveTo.HasValue || e.EffectiveTo >= asOfDate))
            .OrderBy(e => e.CategoryCode)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ExemptSupplyCategory entity, CancellationToken ct = default)
    {
        await _context.ExemptSupplyCategories.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Added ExemptSupplyCategory: Category={Category}, SupplyType={SupplyType}, TaxCodeId={TaxCodeId}",
            entity.CategoryCode, entity.SupplyType, entity.TaxCodeId);
    }

    public async Task UpdateAsync(ExemptSupplyCategory entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.ExemptSupplyCategories.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated ExemptSupplyCategory {Id}", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.ExemptSupplyCategories.FindAsync([id], ct);
        if (entity is null) return;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted ExemptSupplyCategory {Id}", id);
    }
}
