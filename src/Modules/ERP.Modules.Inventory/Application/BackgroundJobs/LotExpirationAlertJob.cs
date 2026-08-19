// <copyright file="LotExpirationAlertJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Application.BackgroundJobs;

public class LotExpirationAlertJob
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<LotExpirationAlertJob> _logger;

    public LotExpirationAlertJob(InventoryDbContext context, ILogger<LotExpirationAlertJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting LotExpirationAlertJob");

            var companies = await _context.Items
                .Select(i => i.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companies)
            {
                await ProcessCompanyLotExpirationAlertsAsync(companyId, cancellationToken);
            }

            _logger.LogInformation("LotExpirationAlertJob completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LotExpirationAlertJob");
            throw;
        }
    }

    private async Task ProcessCompanyLotExpirationAlertsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var warningDate = now.AddDays(30);
        var criticalDate = now.AddDays(7);

        var lots = await _context.Lots
            .Where(l => l.ExpirationDate.HasValue
                     && l.Status == LotStatus.Active
                     && l.ExpirationDate <= warningDate)
            .Include(l => l.Item)
            .Include(l => l.Warehouse)
            .ToListAsync(cancellationToken);

        foreach (var lot in lots)
        {
            // Null check for Item (should not be null due to Include, but compiler needs it)
            if (lot.Item == null || !lot.ExpirationDate.HasValue)
            {
                continue;
            }

            // Get available quantity in this lot
            var availableQty = await _context.InventoryTransactions
                .Where(t => t.CompanyId == companyId
                         && t.ItemId == lot.ItemId
                         && t.WarehouseId == lot.WarehouseId
                         && t.LotId == lot.Id)
                .ToListAsync(cancellationToken);

            decimal qty = availableQty
                .Where(t => t.Quantity > 0).Sum(t => t.Quantity)
                - availableQty.Where(t => t.Quantity < 0).Sum(t => Math.Abs(t.Quantity));

            if (qty <= 0)
                continue;

            // Check if alert already exists
            var existingAlert = await _context.LotExpirationAlerts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId
                                     && a.LotId == lot.Id
                                     && !a.IsAcknowledged
                                     && a.AlertDate >= now.Date, cancellationToken);

            if (existingAlert != null)
                continue;

            var expirationDate = lot.ExpirationDate.Value;

            LotExpirationAlertType alertType;
            string message;

            if (expirationDate <= now)
            {
                alertType = LotExpirationAlertType.Expired;
                message = $"Lot {lot.LotNumber} for item {lot.Item.ItemCode} has expired on {expirationDate:yyyy-MM-dd}. Available: {qty} {lot.Item.BaseUnitOfMeasure}";
            }
            else if (expirationDate <= criticalDate)
            {
                alertType = LotExpirationAlertType.Critical;
                message = $"Lot {lot.LotNumber} for item {lot.Item.ItemCode} expires in {(expirationDate - now).Days} days on {expirationDate:yyyy-MM-dd}. Available: {qty} {lot.Item.BaseUnitOfMeasure}";
            }
            else
            {
                alertType = LotExpirationAlertType.Warning;
                message = $"Lot {lot.LotNumber} for item {lot.Item.ItemCode} expires in {(expirationDate - now).Days} days on {expirationDate:yyyy-MM-dd}. Available: {qty} {lot.Item.BaseUnitOfMeasure}";
            }

            var alert = new LotExpirationAlert(
                companyId,
                lot.Id,
                lot.ItemId,
                lot.WarehouseId,
                alertType,
                now.Date,
                qty,
                expirationDate,
                message);

            _context.LotExpirationAlerts.Add(alert);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processed lot expiration alerts for company {CompanyId}: {Count} lots checked",
            companyId, lots.Count);
    }
}