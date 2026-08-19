// <copyright file="RequisitionSuggester.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.BillOfMaterials.Domain.Services;

/// <summary>
/// Suggests purchasing requisitions for BOM component shortages on a planned build
/// (gap features §676 / §708). For each component, computes the quantity required
/// for the planned build, compares it to on-hand inventory, and — when short —
/// creates a draft Purchasing requisition line for the shortfall, reusing the
/// Phase 6 (Purchasing) requisition aggregate directly (one-way BOM -> Purchasing).
/// </summary>
public class RequisitionSuggester
{
    private readonly BomDbContext _bomContext;
    private readonly InventoryDbContext _invContext;
    private readonly PurchasingDbContext _purContext;

    public RequisitionSuggester(
        BomDbContext bomContext,
        InventoryDbContext invContext,
        PurchasingDbContext purContext)
    {
        _bomContext = bomContext;
        _invContext = invContext;
        _purContext = purContext;
    }

    public async Task<RequisitionSuggestionResult> SuggestAsync(
        Guid bomHeaderId,
        decimal plannedQuantity,
        Guid companyId,
        Guid requestorId,
        CancellationToken cancellationToken)
    {
        var bom = await _bomContext.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == bomHeaderId, cancellationToken);

        if (bom is null)
            throw new InvalidOperationException("BOM header not found.");

        var lines = new List<RequisitionSuggestionLine>();
        var shortages = new List<(int LineNo, decimal Qty, Item? Item, BomComponentLine Comp)>();
        var lineNo = 1;

        foreach (var comp in bom.Components)
        {
            var required = plannedQuantity * comp.EffectiveQuantity;
            if (required <= 0)
                continue;

            var onHand = await GetOnHandAsync(comp.ComponentItemId, cancellationToken);
            var shortfall = required - onHand;

            lines.Add(new RequisitionSuggestionLine
            {
                ComponentItemId = comp.ComponentItemId,
                RequiredQuantity = required,
                OnHandQuantity = onHand,
                Shortfall = shortfall,
            });

            if (shortfall > 0)
            {
                var item = await _invContext.Items.FindAsync(new object[] { comp.ComponentItemId }, cancellationToken);
                shortages.Add((lineNo, shortfall, item, comp));
                lineNo++;
            }
        }

        Guid? requisitionId = null;
        if (shortages.Count > 0)
        {
            var requisition = new Requisition(
                $"BOM-{DateTime.UtcNow:yyyyMMddHHmmss}",
                companyId,
                requestorId,
                DateTime.UtcNow,
                null,
                $"Auto-suggested from BOM {bom.Revision} for planned qty {plannedQuantity}");

            foreach (var (ln, qty, item, component) in shortages)
            {
                var description = item?.Description ?? $"Component {component.ComponentItemId}";
                var unitCost = component.EstimatedUnitCost ?? item?.StandardCost ?? 0m;

                var line = new RequisitionLine(
                    requisition.Id,
                    ln,
                    component.ComponentItemId.ToString(),
                    description,
                    Math.Round(qty, 4),
                    component.UnitOfMeasure,
                    unitCost,
                    null,
                    null,
                    null,
                    null,
                    null);
                requisition.AddLine(line);
            }

            _purContext.Requisitions.Add(requisition);
            await _purContext.SaveChangesAsync(cancellationToken);
            requisitionId = requisition.Id;
        }

        return new RequisitionSuggestionResult
        {
            BomHeaderId = bomHeaderId,
            PlannedQuantity = plannedQuantity,
            RequisitionId = requisitionId,
            Suggestions = lines,
            RequisitionCreated = requisitionId.HasValue,
        };
    }

    private async Task<decimal> GetOnHandAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var movements = await _invContext.InventoryTransactions
            .Where(t => t.ItemId == itemId)
            .Select(t => t.Quantity)
            .ToListAsync(cancellationToken);

        return movements.Sum();
    }
}

#pragma warning disable CA1002, CA2227
public class RequisitionSuggestionResult
{
    public Guid BomHeaderId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public Guid? RequisitionId { get; set; }
    public bool RequisitionCreated { get; set; }
    public List<RequisitionSuggestionLine> Suggestions { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class RequisitionSuggestionLine
{
    public Guid ComponentItemId { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal Shortfall { get; set; }
}
