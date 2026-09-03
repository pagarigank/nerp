// <copyright file="EInvoiceVendorExtensionRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Repository implementation for EInvoiceVendorExtension entity.
/// </summary>
public class EInvoiceVendorExtensionRepository : IEInvoiceVendorExtensionRepository
{
    private readonly EInvoiceDbContext _context;
    private readonly ILogger<EInvoiceVendorExtensionRepository> _logger;

    public EInvoiceVendorExtensionRepository(EInvoiceDbContext context, ILogger<EInvoiceVendorExtensionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EInvoiceVendorExtension?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.EInvoiceVendorExtensions.FindAsync([id], ct);
    }

    public async Task<EInvoiceVendorExtension?> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default)
    {
        return await _context.EInvoiceVendorExtensions
            .FirstOrDefaultAsync(e => e.VendorId == vendorId && !e.IsDeleted, ct);
    }

    public async Task<IEnumerable<EInvoiceVendorExtension>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.EInvoiceVendorExtensions
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.VendorId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<EInvoiceVendorExtension>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _context.EInvoiceVendorExtensions
            .Where(e => !e.IsDeleted && e.CompanyId == companyId)
            .OrderBy(e => e.VendorId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(EInvoiceVendorExtension entity, CancellationToken ct = default)
    {
        // Upsert: if one already exists for this vendor, update it instead
        var existing = await _context.EInvoiceVendorExtensions
            .FirstOrDefaultAsync(e => e.VendorId == entity.VendorId && !e.IsDeleted, ct);

        if (existing != null)
        {
            _logger.LogInformation("EInvoiceVendorExtension already exists for VendorId {VendorId} (ID: {Id}). Updating.", entity.VendorId, existing.Id);
            existing.SetTaxIdentificationNumber(entity.TaxIdentificationNumber);
            existing.SetIdType(entity.IdType);
            existing.SetIdValue(entity.IdValue);
            existing.SetSstRegistrationNumber(entity.SstRegistrationNumber);
            existing.SetSstBillingAddress(
                entity.SstBillingAddress,
                entity.SstBillingCity,
                entity.SstBillingState,
                entity.SstBillingPostalCode,
                entity.SstBillingCountry);
            existing.SetEInvoiceStatus(entity.IsSubjectToEInvoice);
            existing.SetSelfBilledEligible(entity.CanIssueSelfBilled);
            existing.MarkModified("system");
            await _context.SaveChangesAsync(ct);
            return;
        }

        entity.CreatedBy = "system";
        await _context.EInvoiceVendorExtensions.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Created EInvoiceVendorExtension for VendorId {VendorId}", entity.VendorId);
    }

    public async Task UpdateAsync(EInvoiceVendorExtension entity, CancellationToken ct = default)
    {
        entity.MarkModified("system");
        _context.EInvoiceVendorExtensions.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated EInvoiceVendorExtension {Id} for VendorId {VendorId}", entity.Id, entity.VendorId);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.EInvoiceVendorExtensions.FindAsync([id], ct);
        if (entity is null) return;

        entity.MarkDeleted("system");
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted EInvoiceVendorExtension {Id} for VendorId {VendorId}", id, entity.VendorId);
    }
}
