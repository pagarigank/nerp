// <copyright file="FinanceChargeJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Hangfire;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class FinanceChargeJob
{
    private readonly IFinanceChargeService _financeChargeService;
    private readonly ILogger<FinanceChargeJob> _logger;

    public FinanceChargeJob(IFinanceChargeService financeChargeService, ILogger<FinanceChargeJob> logger)
    {
        _financeChargeService = financeChargeService ?? throw new ArgumentNullException(nameof(financeChargeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300])]
    public async Task CalculateFinanceChargesAsync(Guid companyId, decimal annualRate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled finance charge calculation for company {CompanyId}", companyId);

        try
        {
            var asOfDate = DateTimeOffset.UtcNow;
            var charges = await _financeChargeService.CalculateChargesAsync(companyId, annualRate, asOfDate, cancellationToken);

            _logger.LogInformation(
                "Finance charge calculation completed for company {CompanyId}. Generated {Count} charges.",
                companyId,
                charges.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Finance charge calculation failed for company {CompanyId}", companyId);
            throw;
        }
    }
}
