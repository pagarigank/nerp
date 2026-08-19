// <copyright file="OpenPOAgingJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Purchasing.Infrastructure;

public class OpenPOAgingJob
{
    private readonly PurchasingDbContext _context;
    private readonly ILogger<OpenPOAgingJob> _logger;

    public OpenPOAgingJob(
        PurchasingDbContext context,
        ILogger<OpenPOAgingJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting open PO aging analysis job...");

        try
        {
            var today = DateTime.UtcNow.Date;

            var openPOs = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .Where(p => p.Status == PurchaseOrderStatus.Approved)
                .Select(p => new
                {
                    p.Id,
                    p.PONumber,
                    p.CompanyId,
                    p.VendorId,
                    p.OrderDate,
                    DaysOpen = (today - p.OrderDate.Date).Days,
                    TotalAmount = p.Lines.Sum(l => l.Quantity * l.UnitPrice),
                    RemainingAmount = p.Lines.Where(l => !l.IsCancelled).Sum(l => (l.Quantity - l.QuantityReceived) * l.UnitPrice),
                })
                .Where(p => p.DaysOpen > 30)
                .ToListAsync(cancellationToken);

            var aging30 = openPOs.Count(p => p.DaysOpen >= 30 && p.DaysOpen < 60);
            var aging60 = openPOs.Count(p => p.DaysOpen >= 60 && p.DaysOpen < 90);
            var aging90 = openPOs.Count(p => p.DaysOpen >= 90);

            _logger.LogInformation(
                "Open PO Aging Summary: 30-60 days: {Aging30}, 60-90 days: {Aging60}, 90+ days: {Aging90}",
                aging30,
                aging60,
                aging90);

            if (aging90 > 0)
            {
                var oldest = openPOs.Where(p => p.DaysOpen >= 90).OrderByDescending(p => p.DaysOpen).Take(5);
                foreach (var po in oldest)
                {
                    _logger.LogWarning(
                        "Aged PO: {PONumber}, {DaysOpen} days open, Remaining: {RemainingAmount:C}",
                        po.PONumber,
                        po.DaysOpen,
                        po.RemainingAmount);
                }
            }

            _logger.LogInformation("Open PO aging analysis job completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during open PO aging analysis job execution.");
            throw;
        }
    }
}
