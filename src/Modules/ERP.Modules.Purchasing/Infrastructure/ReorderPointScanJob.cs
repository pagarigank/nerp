// <copyright file="ReorderPointScanJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.Purchasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Infrastructure;

public interface IReorderPointScanJob
{
    Task<ReorderScanReport> RunAsync(CancellationToken ct = default);
}

/// <summary>
/// Outcome of a reorder-point scan run.
/// </summary>
/// <param name="ItemsBelowReorderPoint">Items whose effective need (on-hand minus open PO quantity) is below the reorder point.</param>
/// <param name="RequisitionsCreated">Draft requisitions created during the scan.</param>
/// <param name="Skipped">Items that needed replenishment but were skipped, with reasons.</param>
public record ReorderScanReport(int ItemsBelowReorderPoint, int RequisitionsCreated, IReadOnlyList<string> Skipped);

public class ReorderPointScanJob : IReorderPointScanJob
{
    public const int DuplicateLookbackDays = 7;

    private static readonly PurchaseOrderStatus[] OpenPoStatuses =
    [
        PurchaseOrderStatus.Draft,
        PurchaseOrderStatus.PendingApproval,
        PurchaseOrderStatus.Approved,
    ];

    private readonly PurchasingDbContext _context;
    private readonly IRepository<Requisition> _requisitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReorderSource _reorderSource;

    public ReorderPointScanJob(
        PurchasingDbContext context,
        IRepository<Requisition> requisitionRepository,
        IUnitOfWork unitOfWork,
        IInventoryReorderSource reorderSource)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _requisitionRepository = requisitionRepository ?? throw new ArgumentNullException(nameof(requisitionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reorderSource = reorderSource ?? throw new ArgumentNullException(nameof(reorderSource));
    }

    public async Task<ReorderScanReport> RunAsync(CancellationToken ct = default)
    {
        var candidates = await _reorderSource.GetBelowReorderPointAsync(ct);
        if (candidates.Count == 0)
            return new ReorderScanReport(0, 0, []);

        var itemKeys = candidates.Select(c => c.ItemId.ToString()).ToList();

        var openPoQuantities = await _context.PurchaseOrderLines
            .Where(l => l.ItemId != null
                && itemKeys.Contains(l.ItemId)
                && !l.IsCancelled
                && l.QuantityReceived < l.Quantity
                && _context.PurchaseOrders.Any(
                    p => p.Id == l.PurchaseOrderId
                        && OpenPoStatuses.Contains(p.Status)))
            .GroupBy(l => l.ItemId)
            .Select(g => new { ItemId = g.Key!, OpenQuantity = g.Sum(x => x.Quantity - x.QuantityReceived) })
            .ToListAsync(ct);

        var openPoQtyByItem = openPoQuantities.ToDictionary(x => x.ItemId, x => x.OpenQuantity);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-DuplicateLookbackDays);
        var recentItemIds = await _context.RequisitionLines
            .Where(l => l.ItemId != null
                && _context.Requisitions.Any(
                    r => r.Id == l.RequisitionId
                        && r.CreatedOn >= cutoff
                        && r.Status != RequisitionStatus.Cancelled))
            .Select(l => l.ItemId!)
            .Distinct()
            .ToListAsync(ct);

        var recentItemKeySet = recentItemIds.ToHashSet(StringComparer.Ordinal);

        var skipped = new List<string>();
        int itemsBelowReorderPoint = 0;
        var plans = new Dictionary<(Guid CompanyId, Guid VendorId), List<(string ItemKey, string ItemCode, decimal Quantity)>>();

        foreach (var candidate in candidates)
        {
            var itemKey = candidate.ItemId.ToString();
            var effectiveNeed = candidate.OnHand - openPoQtyByItem.GetValueOrDefault(itemKey);

            if (effectiveNeed >= candidate.ReorderPoint)
                continue;

            itemsBelowReorderPoint++;

            if (!candidate.PreferredVendorId.HasValue)
            {
                skipped.Add($"{candidate.ItemCode}: no preferred vendor assigned.");
                continue;
            }

            if (recentItemKeySet.Contains(itemKey))
            {
                skipped.Add($"{candidate.ItemCode}: requisition created within the last {DuplicateLookbackDays} days.");
                continue;
            }

            var quantityToOrder = Math.Max(candidate.ReorderQuantity, candidate.ReorderPoint - effectiveNeed);
            var planKey = (candidate.CompanyId, candidate.PreferredVendorId.Value);
            if (!plans.TryGetValue(planKey, out var planLines))
            {
                planLines = [];
                plans[planKey] = planLines;
            }

            planLines.Add((itemKey, candidate.ItemCode, quantityToOrder));
        }

        var requisitionsCreated = 0;
        foreach (var plan in plans)
        {
            var requisition = new Requisition(
                BuildAutoRequisitionNumber(),
                plan.Key.CompanyId,
                Guid.Empty,
                DateTime.UtcNow,
                null,
                $"Auto-generated by reorder point scan ({plan.Value.Count} item(s))");

            var lineNumber = 1;
            foreach (var planLine in plan.Value)
            {
                requisition.AddLine(new RequisitionLine(
                    requisition.Id,
                    lineNumber++,
                    planLine.ItemKey,
                    planLine.ItemCode,
                    planLine.Quantity,
                    "EA",
                    0m,
                    null,
                    plan.Key.VendorId,
                    null,
                    null,
                    null));
            }

            await _requisitionRepository.AddAsync(requisition, ct);
            requisitionsCreated++;
        }

        if (requisitionsCreated > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return new ReorderScanReport(itemsBelowReorderPoint, requisitionsCreated, skipped);
    }

    private static string BuildAutoRequisitionNumber()
    {
        return $"REQ-AUTO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
