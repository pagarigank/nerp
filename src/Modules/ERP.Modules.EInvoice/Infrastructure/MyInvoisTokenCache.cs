// <copyright file="MyInvoisTokenCache.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Caches MyInvois OAuth access tokens to avoid re-authenticating per API call.
/// Tokens are cached for ~55 minutes (token lifetime is ~60 minutes).
/// Thread-safe via IMemoryCache.
/// </summary>
public interface IMyInvoisTokenCache
{
    Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string environment, CancellationToken ct = default);
}

public class MyInvoisTokenCache : IMyInvoisTokenCache
{
    private const string CacheKeyPrefix = "myinvois:token:";
    private const int CacheExpiryMinutes = 55; // Token lifetime is ~60 min; refresh at 55

    private readonly IMyInvoisApiClient _apiClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MyInvoisTokenCache> _logger;

    public MyInvoisTokenCache(IMyInvoisApiClient apiClient, IMemoryCache cache, ILogger<MyInvoisTokenCache> logger)
    {
        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string environment, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{environment}:{clientId}";

        if (_cache.TryGetValue<string>(cacheKey, out var cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            _logger.LogDebug("Returning cached MyInvois token for environment {Environment}", environment);
            return cachedToken;
        }

        _logger.LogInformation("Requesting new MyInvois token for environment {Environment}", environment);

        var authRequest = new AuthenticationRequest(clientId, clientSecret, environment);
        var authResponse = await _apiClient.AuthenticateAsync(authRequest, ct);

        // Cache for 55 minutes (token lifetime is ~60 min)
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpiryMinutes),
            Priority = CacheItemPriority.Normal,
        };

        _cache.Set(cacheKey, authResponse.AccessToken, cacheOptions);

        _logger.LogInformation("Cached new MyInvois token for environment {Environment}, expires in {Minutes} minutes", environment, CacheExpiryMinutes);

        return authResponse.AccessToken;
    }
}
