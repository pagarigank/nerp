// <copyright file="IEInvoiceItemExtensionRepository.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain.Entities;

namespace ERP.Modules.EInvoice.Application;

public interface IEInvoiceItemExtensionRepository
{
    Task<List<EInvoiceItemExtension>> GetAllAsync();
    Task<EInvoiceItemExtension?> GetByIdAsync(Guid id);
    Task<EInvoiceItemExtension?> GetByItemIdAsync(Guid itemId);
    Task AddAsync(EInvoiceItemExtension extension);
    Task UpdateAsync(EInvoiceItemExtension extension);
    Task DeleteAsync(Guid id);
}
