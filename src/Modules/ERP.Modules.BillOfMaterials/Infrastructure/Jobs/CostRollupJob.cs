// <copyright file="CostRollupJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

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

/// <summary>
/// Recomputes EstimatedMaterialCost bottom-up across multi-level active BOMs,
/// persisting only changed costs, and reports the largest deltas.
/// </summary>
public partial class CostRollupJob : ICostRollupJob
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

        var memoizedCosts = new Dictionary<Guid, decimal>();
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

        var deltas = new List<CostRollupDelta>();
        var updatedCount = 0;
        var unchangedCount = 0;

        foreach (var header in headers)
        {
            var newCost = Math.Round(CostOf(header.ParentItemId), 4);

            if (header.EstimatedMaterialCost == newCost)
            {
                unchangedCount++;
                continue;
            }

            var previousCost = header.EstimatedMaterialCost ?? 0m;
            header.UpdateEstimatedCosts(newCost, header.EstimatedLaborCost ?? 0m, header.EstimatedOverheadCost ?? 0m);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting weekly BOM standard-cost roll-up")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "BOM cost roll-up completed: {Total} checked, {Updated} updated, {Unchanged} unchanged")]
    private partial void LogCompleted(int total, int updated, int unchanged);
}
