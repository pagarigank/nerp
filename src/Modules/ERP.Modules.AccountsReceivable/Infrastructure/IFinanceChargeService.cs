// <copyright file="IFinanceChargeService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public interface IFinanceChargeService
{
    Task<IReadOnlyList<FinanceCharge>> CalculateChargesAsync(Guid companyId, decimal annualRate, DateTimeOffset asOfDate, CancellationToken cancellationToken = default);
}
