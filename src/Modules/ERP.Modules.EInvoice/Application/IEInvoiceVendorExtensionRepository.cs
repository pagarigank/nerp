// <copyright file="IEInvoiceVendorExtensionRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain.Entities;

namespace ERP.Modules.EInvoice.Application;

/// <summary>
/// Repository interface for EInvoiceVendorExtension entity.
/// </summary>
public interface IEInvoiceVendorExtensionRepository
{
    Task<EInvoiceVendorExtension?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EInvoiceVendorExtension?> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default);
    Task<IEnumerable<EInvoiceVendorExtension>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(EInvoiceVendorExtension entity, CancellationToken ct = default);
    Task UpdateAsync(EInvoiceVendorExtension entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
