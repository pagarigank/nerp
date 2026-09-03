// <copyright file="LhdnCodeTableCache.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Caches LHDN code tables (classification codes, UOM, ID types, payment modes)
/// used in MyInvois e-Invoice documents. Synced daily from the LHDN reference API.
/// Falls back to embedded defaults when the API is unavailable.
/// </summary>
public interface ILhdnCodeTableCache
{
    /// <summary>
    /// Gets all codes for a specific table.
    /// </summary>
    IReadOnlyList<LhdnCodeEntry> GetCodes(string tableName);

    /// <summary>
    /// Gets a specific code entry by table and code value.
    /// </summary>
    LhdnCodeEntry? GetCode(string tableName, string code);

    /// <summary>
    /// Gets all table names that have cached codes.
    /// </summary>
    IReadOnlyList<string> GetTableNames();

    /// <summary>
    /// Refreshes the cache from LHDN API or embedded defaults.
    /// </summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the last sync timestamp.
    /// </summary>
    DateTime? LastSyncTime { get; }
}

public class LhdnCodeTableCache : ILhdnCodeTableCache
{
    /// <summary>Cache key prefix for code table entries.</summary>
    private const string CacheKeyPrefix = "lhdn:codetable:";

    /// <summary>Cache key for the last sync timestamp.</summary>
    private const string SyncTimeKey = "lhdn:codetable:sync_time";

    /// <summary>Cache duration for code tables (12 hours).</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    /// <summary>Embedded default code tables — used when the LHDN API is unavailable.</summary>
    private static readonly ConcurrentDictionary<string, List<LhdnCodeEntry>> EmbeddedDefaults = new()
    {
        ["classification"] = new List<LhdnCodeEntry>
        {
            new("022", "Goods", "Goods"),
            new("031", "Services", "Services"),
            new("032", "Construction services", "Services"),
            new("033", "Utility services", "Services"),
            new("034", "Telecommunications services", "Services"),
            new("035", "Transportation services", "Services"),
            new("036", "Real estate services", "Services"),
            new("037", "Professional services", "Services"),
            new("038", "Government services", "Services"),
            new("039", "Other services", "Services"),
        },
        ["uom"] = new List<LhdnCodeEntry>
        {
            new("C62", "Each (unit)", "Count"),
            new("KGM", "Kilogram", "Mass"),
            new("GRM", "Gram", "Mass"),
            new("LTR", "Litre", "Volume"),
            new("MLT", "Millilitre", "Volume"),
            new("MTR", "Metre", "Length"),
            new("MTK", "Square metre", "Area"),
            new("MTQ", "Cubic metre", "Volume"),
            new("HUR", "Hour", "Time"),
            new("DAY", "Day", "Time"),
            new("MON", "Month", "Time"),
            new("SET", "Set", "Count"),
            new("BOX", "Box", "Count"),
            new("PAK", "Package", "Count"),
            new("BAG", "Bag", "Count"),
            new("NPR", "Pair", "Count"),
            new("RO", "Roll", "Count"),
            new("DZN", "Dozen", "Count"),
            new("BDL", "Bundle", "Count"),
            new("CT", "Carton", "Count"),
        },
        ["idType"] = new List<LhdnCodeEntry>
        {
            new("NRIC", "New IC / MyKad", "Individual"),
            new("PASS", "Passport", "Individual"),
            new("ARMY", "Army", "Individual"),
            new("BRN", "Business Registration Number", "Business"),
            new("SSM", "SSM Registration No.", "Business"),
            new("OLD_IC", "Old IC", "Individual"),
            new("TRL", "Tax Reference Number", "Tax"),
        },
        ["paymentMode"] = new List<LhdnCodeEntry>
        {
            new("01", "Cash", "Payment"),
            new("02", "Cheque", "Payment"),
            new("03", "Credit Card", "Payment"),
            new("04", "Bank Transfer", "Payment"),
            new("05", "QR Code", "Payment"),
            new("06", "Online Payment", "Payment"),
            new("07", "Debit Card", "Payment"),
            new("08", "E-Wallet", "Payment"),
            new("09", "Others", "Payment"),
        },
        ["taxType"] = new List<LhdnCodeEntry>
        {
            new("01", "Sales Tax (Manufacturing)", "Sales Tax"),
            new("02", "Sales Tax (Import)", "Sales Tax"),
            new("03", "Sales Tax (Services)", "Sales Tax"),
            new("04", "Service Tax", "Service Tax"),
            new("E", "Exempt", "Exempt"),
            new("ZRL", "Zero Rated", "Zero Rate"),
            new("DS", "Deemed Supply", "Special"),
        },
    };

    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LhdnCodeTableCache> _logger;

    public LhdnCodeTableCache(
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<LhdnCodeTableCache> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        LastSyncTime = null;
    }

    /// <inheritdoc />
    public DateTime? LastSyncTime { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<LhdnCodeEntry> GetCodes(string tableName)
    {
        var key = CacheKeyPrefix + tableName;
        if (_cache.TryGetValue<List<LhdnCodeEntry>>(key, out var codes) && codes != null)
        {
            return codes.AsReadOnly();
        }

        if (EmbeddedDefaults.TryGetValue(tableName, out var defaults))
        {
            return defaults.AsReadOnly();
        }

        return Array.Empty<LhdnCodeEntry>();
    }

    /// <inheritdoc />
    public LhdnCodeEntry? GetCode(string tableName, string code)
    {
        var codes = GetCodes(tableName);
        return codes.FirstOrDefault(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTableNames()
    {
        return EmbeddedDefaults.Keys.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("LHDN code table sync started.");

        var tablesToSync = EmbeddedDefaults.Keys.ToList();
        var synced = 0;

        foreach (var tableName in tablesToSync)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var codes = await FetchCodesFromApiAsync(tableName, ct);
                if (codes != null && codes.Count > 0)
                {
                    StoreInCache(tableName, codes);
                    synced++;
                }
                else if (EmbeddedDefaults.TryGetValue(tableName, out var defaults))
                {
                    StoreInCache(tableName, defaults);
                    synced++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync code table '{TableName}', using defaults", tableName);

                if (EmbeddedDefaults.TryGetValue(tableName, out var defaults))
                {
                    StoreInCache(tableName, defaults);
                    synced++;
                }
            }
        }

        LastSyncTime = DateTime.UtcNow;
        _cache.Set(SyncTimeKey, LastSyncTime, CacheDuration);

        _logger.LogInformation(
            "LHDN code table sync completed. {Synced}/{Total} tables synced.",
            synced,
            tablesToSync.Count);
    }

    private void StoreInCache(string tableName, List<LhdnCodeEntry> codes)
    {
        var key = CacheKeyPrefix + tableName;
        _cache.Set(key, codes, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1,
        });
    }

    private async Task<List<LhdnCodeEntry>?> FetchCodesFromApiAsync(
        string tableName,
        CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("lhdn-reference");
            var response = await client.GetAsync($"/api/v1.0/codetables/{tableName}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "LHDN code table API returned {StatusCode} for table '{TableName}'",
                    response.StatusCode,
                    tableName);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var entries = JsonSerializer.Deserialize<List<LhdnCodeEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch code table '{TableName}' from LHDN API", tableName);
            return null;
        }
    }
}

/// <summary>
/// Represents a single entry in an LHDN code table.
/// </summary>
public record LhdnCodeEntry(string Code, string Description, string Category);
