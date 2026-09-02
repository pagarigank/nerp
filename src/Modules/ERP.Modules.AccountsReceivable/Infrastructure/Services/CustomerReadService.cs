// <copyright file="CustomerReadService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Documents;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure.Services;

/// <summary>
/// Implements <see cref="ICustomerReadService"/> against the AR + Platform
/// DbContexts so the Order Management module can render the shared company
/// document header without depending on AR.
/// </summary>
public sealed class CustomerReadService : ICustomerReadService
{
    private readonly ArDbContext _arContext;
    private readonly PlatformDbContext _platformContext;

    public CustomerReadService(ArDbContext arContext, PlatformDbContext platformContext)
    {
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
    }

    public async Task<CompanyDocumentHeader> GetDocumentHeaderAsync(
        Guid companyId,
        Guid? customerId,
        string documentTitle,
        string documentNumber,
        DateTimeOffset? documentDate,
        CancellationToken cancellationToken = default)
    {
        var company = await _platformContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        string? customerName = null, customerTaxId = null, customerAddress = null;
        if (customerId.HasValue)
        {
            var customer = await _arContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId.Value, cancellationToken);
            if (customer is not null)
            {
                customerName = customer.Name;
                customerTaxId = customer.TaxId;
                customerAddress = BuildAddress(
                    customer.ShippingAddress,
                    customer.ShippingCity,
                    customer.ShippingState,
                    customer.ShippingZipCode,
                    customer.ShippingCountry);
            }
        }

        return new CompanyDocumentHeader(
            CompanyName: company?.Name ?? string.Empty,
            CompanyLegalName: company?.LegalName,
            CompanyTaxId: company?.TaxId,
            CompanyAddress: company?.Address,
            CompanyCurrency: company?.BaseCurrency ?? "USD",
            CustomerName: customerName,
            CustomerTaxId: customerTaxId,
            CustomerAddress: customerAddress,
            DocumentTitle: documentTitle,
            DocumentNumber: documentNumber,
            DocumentDate: documentDate);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetCustomerNamesAsync(
        IEnumerable<Guid> customerIds,
        CancellationToken cancellationToken = default)
    {
        var idList = customerIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<Guid, string>();

        var customers = await _arContext.Customers
            .AsNoTracking()
            .Where(c => idList.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        return customers.ToDictionary(c => c.Id, c => c.Name);
    }

    private static string? BuildAddress(string? line1, string? city, string? state, string? zip, string? country)
    {
        var parts = new[] { line1, city, state, zip, country }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }
}
