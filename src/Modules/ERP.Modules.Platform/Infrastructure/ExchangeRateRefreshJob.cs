// <copyright file="ExchangeRateRefreshJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Platform.Infrastructure;

public class ExchangeRateRefreshJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly ILogger<ExchangeRateRefreshJob> _logger;

    public ExchangeRateRefreshJob(IServiceProvider serviceProvider, IExchangeRateProvider exchangeRateProvider, ILogger<ExchangeRateRefreshJob> logger)
    {
        _serviceProvider = serviceProvider;
        _exchangeRateProvider = exchangeRateProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task RefreshRatesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        _logger.LogInformation("Starting scheduled exchange rate refresh");

        try
        {
            // Get all active currencies
            var activeCurrencies = await context.Currencies
                .Where(c => c.IsActive)
                .Select(c => c.Code)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (activeCurrencies.Count < 2)
            {
                _logger.LogInformation("Not enough active currencies for exchange rate refresh");
                return;
            }

            var baseCurrency = activeCurrencies[0];
            var targetCurrencies = activeCurrencies.Where(c => c != baseCurrency).ToList();

            var rates = await _exchangeRateProvider.GetRatesAsync(baseCurrency, targetCurrencies, cancellationToken);

            foreach (var ratePair in rates)
            {
                var targetCurrency = ratePair.Key;
                var rate = ratePair.Value;

                var firstCompany = await context.Companies
                    .Where(c => !c.DeletedOn.HasValue)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (firstCompany == Guid.Empty)
                {
                    _logger.LogWarning("No company found for exchange rate");
                    continue;
                }

                var existingRate = await context.ExchangeRates
                    .FirstOrDefaultAsync(r => r.FromCurrency == baseCurrency
                        && r.ToCurrency == targetCurrency, cancellationToken);

                if (existingRate != null)
                {
                    existingRate.Update(rate, DateTimeOffset.UtcNow);
                }
                else
                {
                    var newRate = new ExchangeRate(firstCompany, baseCurrency, targetCurrency, rate, DateTimeOffset.UtcNow);
                    context.ExchangeRates.Add(newRate);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Exchange rate refresh completed successfully. Updated {Count} rates.", rates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exchange rate refresh failed");
            throw;
        }
    }
}