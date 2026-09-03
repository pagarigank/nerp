// <copyright file="ISstReturnService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Service for computing and managing SST-02/SST-02A returns.
/// </summary>
public interface ISstReturnService
{
    /// <summary>
    /// Compute or recompute a return for a given period by aggregating
    /// posted transactions from the ERP GL/AR/AP modules.
    /// </summary>
    Task<SstReturn> ComputeReturnAsync(
        Guid companyId,
        DateTime periodStart,
        string returnType,
        CancellationToken ct = default);

    /// <summary>
    /// Get all returns for a company.
    /// </summary>
    Task<IReadOnlyList<SstReturn>> GetReturnsAsync(
        Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Get a single return by ID.
    /// </summary>
    Task<SstReturn?> GetReturnByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Mark a return as filed with LHDN reference.
    /// </summary>
    Task<SstReturn> FileReturnAsync(
        Guid returnId, string lhdnReference, CancellationToken ct = default);

    /// <summary>
    /// Amend a previously filed return.
    /// </summary>
    Task<SstReturn> AmendReturnAsync(
        Guid returnId, CancellationToken ct = default);
}
