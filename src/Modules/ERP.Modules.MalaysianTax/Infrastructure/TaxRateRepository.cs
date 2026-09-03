// <copyright file="TaxRateRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Repository implementation for TaxRate entity.
/// </summary>
public class TaxRateRepository : ITaxRateRepository
{
    private readonly MalaysianTaxDbContext _context;
    private readonly ILogger<TaxRateRepository> _logger;

    public TaxRateRepository(MalaysianTaxDbContext context, ILogger<TaxRateRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TaxRates.FindAsync([id], ct);
    }

    public async Task<TaxRate?> GetActiveRateByCodeAsync(Guid taxCodeId, DateTime? asOfDate = null, CancellationToken ct = default)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        return await _context.TaxRates
            .Where(tr => tr.TaxCodeId == taxCodeId 
                      && tr.IsActive 
                      && tr.EffectiveFrom <= date 
                      && (tr.EffectiveTo == null || tr.EffectiveTo > date))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TaxRate?> GetRateForPeriodAsync(Guid taxCodeId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _context.TaxRates
            .Where(tr => tr.TaxCodeId == taxCodeId
                      && tr.IsActive
                      && tr.EffectiveFrom <= to
                      && (tr.EffectiveTo == null || tr.EffectiveTo >= from))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<TaxRate>> GetRatesByCodeAsync(Guid taxCodeId, CancellationToken ct = default)
    {
        return await _context.TaxRates
            .Where(tr => tr.TaxCodeId == taxCodeId)
            .OrderBy(tr => tr.EffectiveFrom)
            .ToListAsync(ct);
    }

    public async Task AddAsync(TaxRate entity, CancellationToken ct = default)
    {
        await _context.TaxRates.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Added TaxRate {Rate}% for TaxCode {TaxCodeId}", 
            entity.RatePercent, entity.TaxCodeId);
    }

    public async Task UpdateAsync(TaxRate entity, CancellationToken ct = default)
    {
        _context.TaxRates.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated TaxRate {Rate}%", entity.RatePercent);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.TaxRates.FindAsync([id], ct);
        if (entity is null) return;

        entity.IsActive = false;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted TaxRate {Id}", id);
    }
}