// <copyright file="VendorReadService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Shared.Kernel.Documents;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IVendorReadService"/> against the AP DbContext so the
/// Order Management module can resolve vendor IDs to names in report PDFs
/// without a compile-time dependency on the Accounts Payable module.
/// </summary>
public sealed class VendorReadService : IVendorReadService
{
    private readonly ApDbContext _apContext;

    public VendorReadService(ApDbContext apContext)
    {
        _apContext = apContext ?? throw new System.ArgumentNullException(nameof(apContext));
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetVendorNamesAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default)
    {
        var idList = vendorIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var vendors = await _apContext.Vendors
            .AsNoTracking()
            .Where(v => idList.Contains(v.Id))
            .Select(v => new { v.Id, v.Name })
            .ToListAsync(cancellationToken);

        return vendors.ToDictionary(v => v.Id, v => v.Name);
    }
}
