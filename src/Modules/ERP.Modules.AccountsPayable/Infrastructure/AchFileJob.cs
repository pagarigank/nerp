// <copyright file="AchFileJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class AchFileJob
{
    private readonly ApDbContext _context;
    private readonly IAchFileService _achFileService;
    private readonly ILogger<AchFileJob> _logger;

    public AchFileJob(ApDbContext context, IAchFileService achFileService, ILogger<AchFileJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _achFileService = achFileService ?? throw new ArgumentNullException(nameof(achFileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [300, 900])]
    public async Task GenerateAndTransmitAchFilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ACH file generation and transmission job");

        try
        {
            var issuedPayments = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Issued)
                .Include(p => p.Lines)
                .ToListAsync(cancellationToken);

            if (issuedPayments.Count == 0)
            {
                _logger.LogInformation("No issued payments found for ACH generation");
                return;
            }

            var paymentIds = issuedPayments.Select(p => p.Id).ToList();
            var result = await _achFileService.GenerateBatchAchFileAsync(paymentIds, cancellationToken);

            _logger.LogInformation(
                "ACH file {FileName} generated with {RecordCount} records totaling {TotalAmount:C2}",
                result.FileName,
                result.RecordCount,
                result.TotalAmount);

            foreach (var payment in issuedPayments)
            {
                payment.Clear();
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("ACH transmission completed for {Count} payments", issuedPayments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACH file generation job failed");
            throw;
        }
    }
}
