// <copyright file="StatementGenerationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Hangfire;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class StatementGenerationJob
{
    private readonly IStatementGenerationService _statementService;
    private readonly ILogger<StatementGenerationJob> _logger;

    public StatementGenerationJob(IStatementGenerationService statementService, ILogger<StatementGenerationJob> logger)
    {
        _statementService = statementService ?? throw new ArgumentNullException(nameof(statementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300])]
    public async Task GenerateStatementsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled statement generation for company {CompanyId}", companyId);

        try
        {
            var asOfDate = DateTimeOffset.UtcNow;
            var statements = await _statementService.GenerateStatementsAsync(companyId, asOfDate, cancellationToken);

            _logger.LogInformation(
                "Statement generation completed for company {CompanyId}. Generated {Count} statements.",
                companyId,
                statements.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Statement generation failed for company {CompanyId}", companyId);
            throw;
        }
    }
}
