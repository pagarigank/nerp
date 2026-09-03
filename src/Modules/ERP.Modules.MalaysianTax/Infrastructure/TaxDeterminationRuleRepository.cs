// <copyright file="TaxDeterminationRuleRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Repository implementation for TaxDeterminationRule entity.
/// </summary>
public class TaxDeterminationRuleRepository : ITaxDeterminationRuleRepository
{
    private readonly MalaysianTaxDbContext _context;
    private readonly ILogger<TaxDeterminationRuleRepository> _logger;

    public TaxDeterminationRuleRepository(MalaysianTaxDbContext context, ILogger<TaxDeterminationRuleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaxDeterminationRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TaxDeterminationRules.FindAsync([id], ct);
    }

    public async Task<IEnumerable<TaxDeterminationRule>> GetAllActiveAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _context.TaxDeterminationRules
            .Where(r => r.IsActive && (r.CompanyId == companyId || r.CompanyId == Guid.Empty))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.SpecificityScore())
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TaxDeterminationRule>> FindMatchingRulesAsync(
        Guid companyId,
        string itemCategory,
        string customerType,
        string placeOfSupply,
        DateTime asOfDate,
        CancellationToken ct = default)
    {
        // Fetch all active rules for this company (or system-wide) that are effective on the given date
        var candidates = await _context.TaxDeterminationRules
            .Where(r => r.IsActive
                && (r.CompanyId == companyId || r.CompanyId == Guid.Empty)
                && (!r.EffectiveFrom.HasValue || r.EffectiveFrom.Value <= asOfDate)
                && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= asOfDate))
            .ToListAsync(ct);

        // Filter to those that actually match the context fields
        var matching = candidates
            .Where(r => r.Matches(itemCategory, customerType, placeOfSupply))
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.SpecificityScore())
            .ToList();

        if (matching.Count > 0)
        {
            _logger.LogDebug(
                "Found {Count} matching rules for CompanyId={CompanyId}, Item={Item}, Customer={Customer}, Place={Place}",
                matching.Count, companyId, itemCategory, customerType, placeOfSupply);
        }

        return matching;
    }

    public async Task AddAsync(TaxDeterminationRule entity, CancellationToken ct = default)
    {
        await _context.TaxDeterminationRules.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Added TaxDeterminationRule: Item={Item}, Customer={Customer}, Place={Place} → TaxCodeId={TaxCodeId}",
            entity.ItemCategory, entity.CustomerType, entity.PlaceOfSupply, entity.TaxCodeId);
    }

    public async Task UpdateAsync(TaxDeterminationRule entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TaxDeterminationRules.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated TaxDeterminationRule {Id}", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.TaxDeterminationRules.FindAsync([id], ct);
        if (entity is null) return;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted TaxDeterminationRule {Id}", id);
    }
}
