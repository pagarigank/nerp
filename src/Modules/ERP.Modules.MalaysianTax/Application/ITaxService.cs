// <copyright file="ITaxService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.MalaysianTax.Application;

/// <summary>
/// Service interface for tax determination and operations.
/// </summary>
public interface ITaxService
{
    Task<TaxDeterminationResult> DetermineTaxAsync(Guid itemId, Guid customerId, decimal amount, Guid companyId, CancellationToken ct = default);
}