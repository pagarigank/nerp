// <copyright file="BomValidationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.BillOfMaterials.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.BillOfMaterials.Infrastructure.Jobs;

public record BomValidationIssue(Guid BomHeaderId, string ParentItemCode, string IssueType, string Message);

public record BomValidationReport(int TotalBomsChecked, IReadOnlyList<BomValidationIssue> Issues);

public interface IBomValidationJob
{
    Task<BomValidationReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Nightly BOM data-quality checks: circular references, inactive/missing components,
/// cost anomalies, inactive work centers and duplicate operation sequences.
/// </summary>
public partial class BomValidationJob : IBomValidationJob
{
    private readonly BomDbContext _context;
    private readonly IInventoryItemLookup _itemLookup;
    private readonly ILogger<BomValidationJob> _logger;

    public BomValidationJob(BomDbContext context, IInventoryItemLookup itemLookup, ILogger<BomValidationJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _itemLookup = itemLookup ?? throw new ArgumentNullException(nameof(itemLookup));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BomValidationReport> RunAsync(CancellationToken cancellationToken = default)
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

        var workCenters = await _context.WorkCenters
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var componentItemsByParent = headers
            .GroupBy(h => h.ParentItemId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(h => h.Components.Select(c => c.ComponentItemId)).ToList());

        var issues = new List<BomValidationIssue>();

        foreach (var header in headers)
        {
            var label = Label(items, header);

            if (ReachesCycle(header.ParentItemId, componentItemsByParent, [], []))
            {
                issues.Add(new BomValidationIssue(
                    header.Id,
                    label,
                    "CircularReference",
                    "This BOM participates in a circular reference chain of parent/component items."));
            }

            foreach (var comp in header.Components)
            {
                items.TryGetValue(comp.ComponentItemId, out var info);

                if (info is null)
                {
                    issues.Add(new BomValidationIssue(
                        header.Id,
                        label,
                        "MissingComponent",
                        $"Component item {comp.ComponentItemId} does not exist in Inventory."));
                }
                else if (!info.IsActive)
                {
                    issues.Add(new BomValidationIssue(
                        header.Id,
                        label,
                        "InactiveComponent",
                        $"Component {info.ItemCode} is inactive."));
                }

                if (comp.EstimatedUnitCost is null)
                {
                    issues.Add(new BomValidationIssue(
                        header.Id,
                        label,
                        "MissingUnitCost",
                        $"Component {Name(info, comp)} has no estimated unit cost."));
                }
                else if (comp.EstimatedUnitCost.Value <= 0m)
                {
                    issues.Add(new BomValidationIssue(
                        header.Id,
                        label,
                        "InvalidUnitCost",
                        $"Component {Name(info, comp)} has a zero or negative estimated unit cost ({comp.EstimatedUnitCost.Value})."));
                }

                if (comp.ScrapFactor < 0m || comp.ScrapFactor > 100m)
                {
                    issues.Add(new BomValidationIssue(
                        header.Id,
                        label,
                        "InvalidScrapFactor",
                        $"Component {Name(info, comp)} has an out-of-range scrap factor ({comp.ScrapFactor})."));
                }

                if (comp.WorkCenterId.HasValue && workCenters.TryGetValue(comp.WorkCenterId.Value, out var wc) && !wc.IsActive)
                {
                    issues.Add(new BomValidationIssue(
                        header.Id,
                        label,
                        "InactiveWorkCenter",
                        $"Component {Name(info, comp)} references inactive work center {wc.Code}."));
                }
            }

            if (header.EstimatedMaterialCost is null)
            {
                issues.Add(new BomValidationIssue(
                    header.Id,
                    label,
                    "MissingMaterialCost",
                    "Estimated material cost has never been computed. Run the cost roll-up job."));
            }

            var duplicatedSequences = header.Components
                .GroupBy(c => c.OperationSequence)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            for (var i = 0; i < duplicatedSequences.Count; i++)
            {
                issues.Add(new BomValidationIssue(
                    header.Id,
                    label,
                    "DuplicateOperationSequence",
                    $"Operation sequence {duplicatedSequences[i]} appears on more than one component line."));
            }
        }

        LogCompleted(headers.Count, issues.Count);

        return new BomValidationReport(headers.Count, issues);
    }

    private static string Name(InventoryItemInfo? info, BomComponentLine comp) =>
        info?.ItemCode ?? comp.ComponentItemId.ToString();

    private static string Label(Dictionary<Guid, InventoryItemInfo> items, BomHeader header) =>
        items.TryGetValue(header.ParentItemId, out var parent) ? parent.ItemCode : header.ParentItemId.ToString();

    private static bool ReachesCycle(
        Guid node,
        Dictionary<Guid, List<Guid>> edges,
        HashSet<Guid> path,
        HashSet<Guid> cycleFreeNodes)
    {
        if (path.Contains(node))
        {
            return true;
        }

        if (cycleFreeNodes.Contains(node))
        {
            return false;
        }

        path.Add(node);

        if (edges.TryGetValue(node, out var nexts))
        {
            for (var i = 0; i < nexts.Count; i++)
            {
                if (ReachesCycle(nexts[i], edges, path, cycleFreeNodes))
                {
                    path.Remove(node);
                    return true;
                }
            }
        }

        path.Remove(node);
        cycleFreeNodes.Add(node);
        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting nightly BOM validation")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "BOM validation completed: {BomCount} BOMs checked, {IssueCount} issues found")]
    private partial void LogCompleted(int bomCount, int issueCount);
}
