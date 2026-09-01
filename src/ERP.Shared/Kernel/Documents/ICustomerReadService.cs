// <copyright file="ICustomerReadService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Shared.Kernel.Documents;

/// <summary>
/// Shared contract for resolving the document header (company + customer) used by
/// the PDF document renderers. Lives in ERP.Shared (which owns
/// <see cref="CompanyDocumentHeader"/>) so Order Management can render customer-facing
/// documents without a compile-time dependency on the AR module — AR, which owns the
/// Customer master, implements this interface.
/// </summary>
public interface ICustomerReadService
{
    Task<CompanyDocumentHeader> GetDocumentHeaderAsync(
        Guid companyId,
        Guid? customerId,
        string documentTitle,
        string documentNumber,
        DateTimeOffset? documentDate,
        CancellationToken cancellationToken = default);
}
