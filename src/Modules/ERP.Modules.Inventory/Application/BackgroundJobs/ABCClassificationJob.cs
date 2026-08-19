// <copyright file="ABCClassificationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Application.BackgroundJobs;

public class ABCClassificationJob
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ABCClassificationJob> _logger;

    public ABCClassificationJob(InventoryDbContext context, ILogger<ABCClassificationJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ABCClassificationJob");

            var companies = await _context.Items
                .Select(i => i.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var companyId in companies)
            {
                await ProcessCompanyABCClassificationAsync(companyId, cancellationToken);
            }

            _logger.LogInformation("ABCClassificationJob completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ABCClassificationJob");
            throw;
        }
    }

    private async Task ProcessCompanyABCClassificationAsync(Guid companyId, CancellationToken cancellationToken)
    {
        // Get all items with their annual usage value (quantity * cost)
        var items = await _context.Items
            .Where(i => i.CompanyId == companyId && i.Status == ItemStatus.Active)
            .ToListAsync(cancellationToken);

        var itemValues = new List<(Guid ItemId, decimal AnnualUsageValue)>();

        foreach (var item in items)
        {
            var totalQty = await _context.InventoryTransactions
                .Where(t => t.CompanyId == companyId
                         && t.ItemId == item.Id
                         && t.TransactionType == TransactionType.Issue
                         && t.TransactionDate >= DateTime.UtcNow.AddYears(-1))
                .SumAsync(t => Math.Abs(t.Quantity), cancellationToken);

            var avgCost = await _context.ItemCostLayers
                .Where(cl => cl.CompanyId == companyId
                          && cl.ItemId == item.Id
                          && cl.RemainingQuantity > 0)
                .AverageAsync(cl => (decimal?)cl.UnitCost, cancellationToken) ?? 0;

            var annualUsageValue = totalQty * avgCost;

            if (annualUsageValue > 0)
            {
                itemValues.Add((item.Id, annualUsageValue));
            }
        }

        if (!itemValues.Any())
        {
            _logger.LogInformation("No items with usage for company {CompanyId}", companyId);
            return;
        }

        // Sort by annual usage value descending
        itemValues.Sort((a, b) => b.AnnualUsageValue.CompareTo(a.AnnualUsageValue));

        // Calculate cumulative percentages
        decimal totalValue = itemValues.Sum(v => v.AnnualUsageValue);
        decimal cumulativeValue = 0;
        int itemCount = itemValues.Count;
        int aCount = 0;
        int bCount = 0;

        foreach (var (itemId, value) in itemValues)
        {
            cumulativeValue += value;
            decimal percentage = cumulativeValue / totalValue * 100;

            var abcClass = percentage switch
            {
                <= 80 => "A",
                <= 95 => "B",
                _ => "C",
            };

            var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
            if (item != null)
            {
                item.UpdateABCClass(abcClass);
                if (abcClass == "A") aCount++;
                else if (abcClass == "B") bCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        int cCount = itemCount - aCount - bCount;
        _logger.LogInformation("ABC Classification for company {CompanyId}: A={ACount}, B={BCount}, C={CCount}",
            companyId, aCount, bCount, cCount);
    }
}