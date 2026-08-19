// <copyright file="ExchangeRateProvider.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Platform.Infrastructure;

public class ExchangeRateProvider : IExchangeRateProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateProvider> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly PlatformDbContext _dbContext;

    public ExchangeRateProvider(HttpClient httpClient, ILogger<ExchangeRateProvider> logger, IConfiguration configuration, PlatformDbContext dbContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["ExchangeRateApi:ApiKey"] ?? string.Empty;
        _baseUrl = configuration["ExchangeRateApi:BaseUrl"] ?? "https://api.exchangerate-api.com/v4/latest";
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency, IEnumerable<string> targetCurrencies, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Exchange rate API key not configured, skipping rate refresh");
            return new Dictionary<string, decimal>();
        }

        try
        {
            var targetList = string.Join(",", targetCurrencies);
            var url = $"{_baseUrl}/{baseCurrency}?api_key={_apiKey}&symbols={targetList}";

            var response = await _httpClient.GetAsync(new Uri(url), cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ExchangeRateApiResponse>(json, JsonOptions);

            return result?.Rates ?? new Dictionary<string, decimal>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rates for base currency {BaseCurrency}", baseCurrency);
            return new Dictionary<string, decimal>();
        }
    }

    public async Task RefreshRatesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Exchange rate API key not configured, skipping rate refresh");
            return;
        }

        try
        {
            // Get all active currencies from the database
            var currencies = await _dbContext.Currencies
                .Where(c => c.IsActive)
                .Select(c => c.Code)
                .ToListAsync(cancellationToken);

            if (currencies.Count == 0)
            {
                _logger.LogWarning("No active currencies found for rate refresh");
                return;
            }

            // Use USD as base currency (common practice)
            var baseCurrency = "USD";

            var rates = await GetRatesAsync(baseCurrency, currencies, cancellationToken);

            foreach (var rateEntry in rates)
            {
                var targetCurrency = rateEntry.Key;
                var rate = rateEntry.Value;

                if (targetCurrency == baseCurrency)
                    continue;

                // Find the first company to use for the exchange rate
                var firstCompany = await _dbContext.Companies
                    .Where(c => !c.DeletedOn.HasValue)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (firstCompany == Guid.Empty)
                {
                    _logger.LogWarning("No company found for exchange rate");
                    continue;
                }

                var exchangeRate = await _dbContext.ExchangeRates
                    .FirstOrDefaultAsync(er => er.FromCurrency == baseCurrency
                        && er.ToCurrency == targetCurrency, cancellationToken);

                if (exchangeRate != null)
                {
                    exchangeRate.Update(rate, DateTimeOffset.UtcNow);
                }
                else
                {
                    exchangeRate = new ExchangeRate(firstCompany, baseCurrency, targetCurrency, rate, DateTimeOffset.UtcNow);
                    _dbContext.ExchangeRates.Add(exchangeRate);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refreshed {Count} exchange rates for base currency {BaseCurrency}", rates.Count, baseCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh exchange rates");
        }
    }

    private sealed class ExchangeRateApiResponse
    {
        public string Base { get; set; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}