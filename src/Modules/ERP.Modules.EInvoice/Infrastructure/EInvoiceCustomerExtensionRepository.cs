// <copyright file="EInvoiceCustomerExtensionRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Repository implementation for EInvoiceCustomerExtension entity.
/// </summary>
public class EInvoiceCustomerExtensionRepository : IEInvoiceCustomerExtensionRepository
{
    private readonly EInvoiceDbContext _context;
    private readonly ILogger<EInvoiceCustomerExtensionRepository> _logger;

    public EInvoiceCustomerExtensionRepository(EInvoiceDbContext context, ILogger<EInvoiceCustomerExtensionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EInvoiceCustomerExtension?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.EInvoiceCustomerExtensions.FindAsync([id], ct);
    }

    public async Task<EInvoiceCustomerExtension?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _context.EInvoiceCustomerExtensions
            .FirstOrDefaultAsync(e => e.CustomerId == customerId && !e.IsDeleted, ct);
    }

    public async Task<IEnumerable<EInvoiceCustomerExtension>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.EInvoiceCustomerExtensions
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.CustomerId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<EInvoiceCustomerExtension>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _context.EInvoiceCustomerExtensions
            .Where(e => !e.IsDeleted && e.CompanyId == companyId)
            .OrderBy(e => e.CustomerId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(EInvoiceCustomerExtension entity, CancellationToken ct = default)
    {
        // Upsert: if one already exists for this customer, update it instead
        var existing = await _context.EInvoiceCustomerExtensions
            .FirstOrDefaultAsync(e => e.CustomerId == entity.CustomerId && !e.IsDeleted, ct);

        if (existing != null)
        {
            _logger.LogInformation("EInvoiceCustomerExtension already exists for CustomerId {CustomerId} (ID: {Id}). Updating.", entity.CustomerId, existing.Id);
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
            existing.MarkModified("system");
            await _context.SaveChangesAsync(ct);
            return;
        }

        entity.CreatedBy = "system";
        await _context.EInvoiceCustomerExtensions.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Created EInvoiceCustomerExtension for CustomerId {CustomerId}", entity.CustomerId);
    }

    public async Task UpdateAsync(EInvoiceCustomerExtension entity, CancellationToken ct = default)
    {
        entity.MarkModified("system");
        _context.EInvoiceCustomerExtensions.Update(entity);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated EInvoiceCustomerExtension {Id} for CustomerId {CustomerId}", entity.Id, entity.CustomerId);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.EInvoiceCustomerExtensions.FindAsync([id], ct);
        if (entity is null) return;

        entity.MarkDeleted("system");
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Soft-deleted EInvoiceCustomerExtension {Id} for CustomerId {CustomerId}", id, entity.CustomerId);
    }
}
