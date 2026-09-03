// <copyright file="IEInvoiceCustomerExtensionRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain.Entities;

namespace ERP.Modules.EInvoice.Application;

/// <summary>
/// Repository interface for EInvoiceCustomerExtension entity.
/// </summary>
public interface IEInvoiceCustomerExtensionRepository
{
    Task<EInvoiceCustomerExtension?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EInvoiceCustomerExtension?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<EInvoiceCustomerExtension>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(EInvoiceCustomerExtension entity, CancellationToken ct = default);
    Task UpdateAsync(EInvoiceCustomerExtension entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
