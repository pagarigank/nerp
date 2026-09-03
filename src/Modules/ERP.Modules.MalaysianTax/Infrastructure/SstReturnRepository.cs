// <copyright file="SstReturnRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Repository for SST return persistence.
/// </summary>
public class SstReturnRepository : ISstReturnRepository
{
    private readonly MalaysianTaxDbContext _dbContext;

    public SstReturnRepository(MalaysianTaxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<SstReturn?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.SstReturns
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SstReturn>> GetAllByCompanyAsync(
        Guid companyId, CancellationToken ct = default)
    {
        return await _dbContext.SstReturns
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SstReturn?> GetByPeriodAsync(
        Guid companyId, DateTime periodStart, string returnType, CancellationToken ct = default)
    {
        return await _dbContext.SstReturns
            .FirstOrDefaultAsync(
                r => r.CompanyId == companyId
                    && r.PeriodStart == periodStart
                    && r.ReturnType == returnType,
                ct);
    }

    /// <inheritdoc />
    public async Task<SstReturn> CreateAsync(SstReturn entity, CancellationToken ct = default)
    {
        _dbContext.SstReturns.Add(entity);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    /// <inheritdoc />
    public async Task<SstReturn> UpdateAsync(SstReturn entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.SstReturns.Update(entity);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }
}
