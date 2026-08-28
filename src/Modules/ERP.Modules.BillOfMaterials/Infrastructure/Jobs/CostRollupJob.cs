// <copyright file="CostRollupJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable CA1848
#pragma warning disable SA1601

using ERP.Core.Common;
using ERP.Modules.BillOfMaterials.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.BillOfMaterials.Infrastructure.Jobs;

public record CostRollupDelta(Guid BomHeaderId, string ParentItemCode, decimal PreviousCost, decimal NewCost, decimal ChangeAmount);

public record CostRollupReport(int TotalBomsChecked, int UpdatedCount, int UnchangedCount, IReadOnlyList<CostRollupDelta> BiggestDeltas);

public interface ICostRollupJob
{
    Task<CostRollupReport> RunAsync(CancellationToken cancellationToken = default);
}

public class CostRollupJob : ICostRollupJob
{
    private const int MaxReportedDeltas = 5;

    private readonly BomDbContext _context;
    private readonly IInventoryItemLookup _itemLookup;
    private readonly ILogger<CostRollupJob> _logger;

    public CostRollupJob(BomDbContext context, IInventoryItemLookup itemLookup, ILogger<CostRollupJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _itemLookup = itemLookup ?? throw new ArgumentNullException(nameof(itemLookup));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CostRollupReport> RunAsync(CancellationToken cancellationToken = default)
    {
        LogStarting();

        var headers = await _context.BomHeaders
            .Include(h => h.Components)
            .Where(h => h.Status == BomStatus.Active)
            .ToListAsync(cancellationToken);

        var itemIds = headers.Select(h => h.ParentItemId)
            .Concat(headers.SelectMany(h => h.Components.Select(c => c.ComponentItemId)))
            .Distinct()
            .ToList();

        var items = (await _itemLookup.GetItemsAsync(itemIds, cancellationToken))
            .ToDictionary(i => i.ItemId);

        var bomByParentItem = headers
            .GroupBy(h => h.ParentItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.CreatedOn).First());

        var workCenters = await _context.WorkCenters
            .Where(w => w.IsActive)
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var routingOps = await _context.RoutingOperations
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var memoizedCosts = new Dictionary<Guid, decimal>();
        var memoizedLabor = new Dictionary<Guid, decimal>();
        var inProgress = new HashSet<Guid>();

        decimal CostOf(Guid itemId)
        {
            if (memoizedCosts.TryGetValue(itemId, out var cached))
            {
                return cached;
            }

            if (!inProgress.Add(itemId))
            {
                return LeafCost(items, itemId);
            }

            decimal cost;

            if (bomByParentItem.TryGetValue(itemId, out var bom))
            {
                var total = bom.Components.Sum(line => line.EffectiveQuantity * CostOf(line.ComponentItemId));

                if (bom.YieldPercentage > 0 && bom.YieldPercentage != 100)
                {
                    total /= bom.YieldPercentage / 100m;
                }

                cost = total;
            }
            else
            {
                cost = LeafCost(items, itemId);
            }

            inProgress.Remove(itemId);
            memoizedCosts[itemId] = cost;

            return cost;
        }

        decimal LaborOf(Guid itemId, Guid companyId)
        {
            if (memoizedLabor.TryGetValue(itemId, out var cached))
            {
                return cached;
            }

            decimal labor = 0;

            if (bomByParentItem.TryGetValue(itemId, out var bom))
            {
                var bomOps = routingOps.Where(o => o.CompanyId == companyId).ToList();

                foreach (var op in bomOps)
                {
                    if (!op.WorkCenterId.HasValue)
                    {
                        continue;
                    }

                    if (!workCenters.TryGetValue(op.WorkCenterId.Value, out var wc))
                    {
                        continue;
                    }

                    var hours = (op.StandardSetupTimeMinutes + op.StandardRunTimeMinutesPerUnit) / 60m;
                    labor += hours * wc.CostRatePerHour;
                }

                foreach (var comp in bom.Components)
                {
                    labor += LaborOf(comp.ComponentItemId, companyId) * comp.EffectiveQuantity;
                }

                if (bom.YieldPercentage > 0 && bom.YieldPercentage != 100)
                {
                    labor /= bom.YieldPercentage / 100m;
                }
            }

            memoizedLabor[itemId] = labor;

            return labor;
        }

        var deltas = new List<CostRollupDelta>();
        var updatedCount = 0;
        var unchangedCount = 0;

        foreach (var header in headers)
        {
            var newCost = Math.Round(CostOf(header.ParentItemId), 4);
            var newLabor = Math.Round(LaborOf(header.ParentItemId, header.CompanyId), 4);
            var newOverhead = Math.Round(newLabor * 0.25m, 4);

            if (header.EstimatedMaterialCost == newCost && header.EstimatedLaborCost == newLabor && header.EstimatedOverheadCost == newOverhead)
            {
                unchangedCount++;
                continue;
            }

            var previousCost = header.EstimatedMaterialCost ?? 0m;
            header.UpdateEstimatedCosts(newCost, newLabor, newOverhead);
            updatedCount++;
            deltas.Add(new CostRollupDelta(header.Id, Label(items, header), previousCost, newCost, newCost - previousCost));
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var biggestDeltas = deltas
            .OrderByDescending(d => Math.Abs(d.ChangeAmount))
            .Take(MaxReportedDeltas)
            .ToList();

        LogCompleted(headers.Count, updatedCount, unchangedCount);

        return new CostRollupReport(headers.Count, updatedCount, unchangedCount, biggestDeltas);
    }

    private static decimal LeafCost(Dictionary<Guid, InventoryItemInfo> items, Guid itemId) =>
        items.TryGetValue(itemId, out var info) ? info.UnitCost ?? info.StandardCost ?? 0m : 0m;

    private static string Label(Dictionary<Guid, InventoryItemInfo> items, BomHeader header) =>
        items.TryGetValue(header.ParentItemId, out var parent) ? parent.ItemCode : header.ParentItemId.ToString();

    private void LogStarting() => _logger.LogInformation("Starting weekly BOM standard-cost roll-up");

    private void LogCompleted(int total, int updated, int unchanged) => _logger.LogInformation("BOM cost roll-up completed: {Total} checked, {Updated} updated, {Unchanged} unchanged", total, updated, unchanged);
}
