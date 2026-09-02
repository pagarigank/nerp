// <copyright file="IVendorReadService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Shared.Kernel.Documents;

/// <summary>
/// Shared contract for resolving vendor display names used by report PDFs.
/// Lives in ERP.Shared so Order Management can render vendor-facing documents
/// without a compile-time dependency on the Accounts Payable module.
/// </summary>
public interface IVendorReadService
{
    /// <summary>Bulk-resolves vendor IDs to display names for report PDFs.</summary>
    /// <param name="vendorIds">The vendor IDs to resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping vendor IDs to their display names.</returns>
    Task<IReadOnlyDictionary<Guid, string>> GetVendorNamesAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default);
}
