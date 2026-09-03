// <copyright file="LhdnCodeTableSyncJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Hangfire background job that syncs LHDN reference code tables.
/// Refreshes classification codes, UOM, ID types, payment modes, and tax types
/// from the LHDN API daily. Falls back to embedded defaults when the API is unavailable.
/// </summary>
public class LhdnCodeTableSyncJob
{
    private readonly ILhdnCodeTableCache _codeTableCache;
    private readonly ILogger<LhdnCodeTableSyncJob> _logger;

    public LhdnCodeTableSyncJob(
        ILhdnCodeTableCache codeTableCache,
        ILogger<LhdnCodeTableSyncJob> logger)
    {
        _codeTableCache = codeTableCache;
        _logger = logger;
    }

    /// <summary>
    /// Refreshes all LHDN code tables from the API or embedded defaults.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("LHDN code table sync job started.");

        try
        {
            await _codeTableCache.RefreshAsync(ct);
            _logger.LogInformation(
                "LHDN code table sync completed. Last sync: {LastSyncTime}",
                _codeTableCache.LastSyncTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LHDN code table sync failed. Embedded defaults will be used.");
        }
    }
}
