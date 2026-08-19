// <copyright file="IExchangeRateProvider.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Infrastructure;

public interface IExchangeRateProvider
{
    Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency, IEnumerable<string> targetCurrencies, CancellationToken cancellationToken = default);

    Task RefreshRatesAsync(CancellationToken cancellationToken = default);
}