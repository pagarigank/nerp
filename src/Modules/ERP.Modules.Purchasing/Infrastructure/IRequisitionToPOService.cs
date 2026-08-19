// <copyright file="IRequisitionToPOService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Purchasing.Infrastructure;

public interface IRequisitionToPOService
{
    Task<Guid> ConvertRequisitionToPOAsync(
        Guid requisitionId,
        Guid? preferredVendorId,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> ConsolidateRequisitionsToPOAsync(
        List<Guid> requisitionIds,
        Guid vendorId,
        CancellationToken cancellationToken = default);
}
