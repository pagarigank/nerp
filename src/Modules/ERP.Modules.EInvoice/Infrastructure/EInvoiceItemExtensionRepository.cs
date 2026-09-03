// <copyright file="EInvoiceItemExtensionRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.EInvoice.Infrastructure;

public class EInvoiceItemExtensionRepository : IEInvoiceItemExtensionRepository
{
    private readonly EInvoiceDbContext _context;

    public EInvoiceItemExtensionRepository(EInvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<List<EInvoiceItemExtension>> GetAllAsync()
    {
        return await _context.Set<EInvoiceItemExtension>()
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.ItemId)
            .ToListAsync();
    }

    public async Task<List<EInvoiceItemExtension>> GetAllByCompanyIdAsync(Guid companyId)
    {
        return await _context.Set<EInvoiceItemExtension>()
            .Where(e => !e.IsDeleted && e.CompanyId == companyId)
            .OrderBy(e => e.ItemId)
            .ToListAsync();
    }

    public async Task<EInvoiceItemExtension?> GetByIdAsync(Guid id)
    {
        return await _context.Set<EInvoiceItemExtension>()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<EInvoiceItemExtension?> GetByItemIdAsync(Guid itemId)
    {
        return await _context.Set<EInvoiceItemExtension>()
            .FirstOrDefaultAsync(e => e.ItemId == itemId && !e.IsDeleted);
    }

    public async Task AddAsync(EInvoiceItemExtension extension)
    {
        _context.Set<EInvoiceItemExtension>().Add(extension);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EInvoiceItemExtension extension)
    {
        _context.Set<EInvoiceItemExtension>().Update(extension);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<EInvoiceItemExtension>().FindAsync(id);
        if (entity != null)
        {
            entity.MarkDeleted("system");
            await _context.SaveChangesAsync();
        }
    }
}
