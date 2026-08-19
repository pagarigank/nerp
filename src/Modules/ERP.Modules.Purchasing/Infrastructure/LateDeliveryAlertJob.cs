// <copyright file="LateDeliveryAlertJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Purchasing.Infrastructure;

public class LateDeliveryAlertJob
{
    private readonly PurchasingDbContext _context;
    private readonly ILogger<LateDeliveryAlertJob> _logger;

    public LateDeliveryAlertJob(
        PurchasingDbContext context,
        ILogger<LateDeliveryAlertJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting late delivery alert job...");

        try
        {
            var today = DateTime.UtcNow.Date;

            var latePOs = await _context.PurchaseOrderLines
                .Include(l => l)
                .Where(l => l.NeedByDate.HasValue && l.NeedByDate.Value.Date < today)
                .Where(l => !l.IsCancelled)
                .Where(l => l.Quantity > l.QuantityReceived)
                .Select(l => new
                {
                    l.PurchaseOrderId,
                    l.LineNumber,
                    l.ItemId,
                    l.Description,
                    NeedByDate = l.NeedByDate!.Value,
                    l.Quantity,
                    l.QuantityReceived,
                    RemainingQuantity = l.Quantity - l.QuantityReceived,
                    DaysLate = (today - l.NeedByDate!.Value.Date).Days,
                })
                .ToListAsync(cancellationToken);

            if (latePOs.Count > 0)
            {
                _logger.LogWarning("Found {Count} late PO lines requiring attention", latePOs.Count);

                foreach (var lateLine in latePOs.Take(10))
                {
                    _logger.LogWarning(
                        "Late PO: {POId}, Line: {LineNumber}, Item: {ItemId}, {DaysLate} days overdue, {RemainingQty} units remaining",
                        lateLine.PurchaseOrderId,
                        lateLine.LineNumber,
                        lateLine.ItemId,
                        lateLine.DaysLate,
                        lateLine.RemainingQuantity);
                }
            }

            _logger.LogInformation("Late delivery alert job completed. Found {Count} late items.", latePOs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during late delivery alert job execution.");
            throw;
        }
    }
}
