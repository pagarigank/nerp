// <copyright file="TaxCodeRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Repository implementation for TaxCode entity.
/// </summary>
public class TaxCodeRepository : ITaxCodeRepository
{
    private readonly MalaysianTaxDbContext _context;
    private readonly ILogger<TaxCodeRepository> _logger;

    public TaxCodeRepository(MalaysianTaxDbContext context, ILogger<TaxCodeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaxCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TaxCodes.FindAsync([id], ct);
    }

    public async Task<TaxCode?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.TaxCodes
            .FirstOrDefaultAsync(tc => tc.Code == code && tc.IsActive, ct);
    }

    public async Task<IEnumerable<TaxCode>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.TaxCodes
            .Where(tc => tc.IsActive)
            .ToListAsync(ct);
    }

    public async Task AddAsync(TaxCode entity, CancellationToken ct = default)
    {
        await _context.TaxCodes.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Added TaxCode {Code} with ID {Id}", entity.Code, entity.Id);
    }

    public async Task UpdateAsync(TaxCode entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TaxCodes.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated TaxCode {Code}", entity.Code);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.TaxCodes.FindAsync([id], ct);
        if (entity is null) return;

        entity.IsActive = false;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted TaxCode {Id}", id);
    }
}