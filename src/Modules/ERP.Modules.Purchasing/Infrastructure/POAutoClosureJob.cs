// <copyright file="POAutoClosureJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace ERP.Modules.Purchasing.Infrastructure;

public class POAutoClosureJob
{
    private readonly IPurchaseOrderService _poService;
    private readonly ILogger<POAutoClosureJob> _logger;

    public POAutoClosureJob(
        IPurchaseOrderService poService,
        ILogger<POAutoClosureJob> logger)
    {
        _poService = poService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting PO auto-closure job...");

        try
        {
            await _poService.AutoClosePurchaseOrdersAsync(90, cancellationToken);
            _logger.LogInformation("PO auto-closure job completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PO auto-closure job execution.");
            throw;
        }
    }
}
