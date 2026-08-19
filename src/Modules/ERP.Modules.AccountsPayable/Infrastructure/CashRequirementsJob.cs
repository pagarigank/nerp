// <copyright file="CashRequirementsJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class CashRequirementsJob
{
    private readonly ApDbContext _context;
    private readonly ILogger<CashRequirementsJob> _logger;

    public CashRequirementsJob(ApDbContext context, ILogger<CashRequirementsJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task GenerateCashRequirementsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cash requirements generation");

        try
        {
            var companyIds = await _context.VoucherBatches
                .Where(b => !b.DeletedOn.HasValue && b.Status == VoucherBatchStatus.Posted)
                .Select(b => b.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companyIds)
            {
                var unpaidVouchers = await _context.Vouchers
                    .Where(v => !v.SelectedForPayment
                        && v.VoucherBatchId != Guid.Empty
                        && v.VoucherBatch != null
                        && v.VoucherBatch.Status == VoucherBatchStatus.Posted)
                    .Include(v => v.VoucherBatch)
                    .ToListAsync(cancellationToken);

                var totalDue = unpaidVouchers
                    .Where(v => v.DueDate <= DateTimeOffset.UtcNow.AddDays(30))
                    .Sum(v => v.TotalAmount - v.DiscountAmount);

                var dueThisWeek = unpaidVouchers
                    .Where(v => v.DueDate <= DateTimeOffset.UtcNow.AddDays(7))
                    .Sum(v => v.TotalAmount - v.DiscountAmount);

                var dueToday = unpaidVouchers
                    .Where(v => v.DueDate <= DateTimeOffset.UtcNow)
                    .Sum(v => v.TotalAmount - v.DiscountAmount);

                _logger.LogInformation(
                    "Company {CompanyId}: Due today={DueToday:C2}, Due this week={DueThisWeek:C2}, Due within 30 days={TotalDue:C2}",
                    companyId,
                    dueToday,
                    dueThisWeek,
                    totalDue);
            }

            _logger.LogInformation("Cash requirements generation completed for {Count} companies", companyIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cash requirements generation failed");
            throw;
        }
    }
}
