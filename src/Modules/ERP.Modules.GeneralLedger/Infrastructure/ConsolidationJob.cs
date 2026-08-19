// <copyright file="ConsolidationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Hangfire;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class ConsolidationJob
{
    private readonly IConsolidationService _consolidationService;
    private readonly ILogger<ConsolidationJob> _logger;

    public ConsolidationJob(IConsolidationService consolidationService, ILogger<ConsolidationJob> logger)
    {
        _consolidationService = consolidationService ?? throw new ArgumentNullException(nameof(consolidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [300, 900])]
    public async Task ExecuteScheduledConsolidationAsync(
        Guid parentCompanyId,
        int fiscalYear,
        int fiscalPeriod,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled consolidation for company {CompanyId}, period {Year}-{Period}", parentCompanyId, fiscalYear, fiscalPeriod);

        try
        {
            var description = $"Scheduled consolidation for {fiscalYear}-{fiscalPeriod:D2}";
            var run = await _consolidationService.CreateConsolidationRunAsync(parentCompanyId, DateTimeOffset.UtcNow, fiscalYear, fiscalPeriod, description, cancellationToken);

            await _consolidationService.ExecuteConsolidationAsync(run.Id, cancellationToken);

            _logger.LogInformation("Scheduled consolidation completed for run {RunId}, company {CompanyId}", run.Id, parentCompanyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled consolidation failed for company {CompanyId}, period {Year}-{Period}", parentCompanyId, fiscalYear, fiscalPeriod);
            throw;
        }
    }
}
