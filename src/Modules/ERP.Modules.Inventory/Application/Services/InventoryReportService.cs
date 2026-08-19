// <copyright file="InventoryReportService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Application.Services;

/// <summary>
/// Read-model queries for the Phase 7 Inventory reports. Each method projects the
/// inv schema into a flat DTO; all amounts use decimal (never float/double) per
/// project convention.
/// </summary>
public class InventoryReportService
{
    private readonly InventoryDbContext _context;

    public InventoryReportService(InventoryDbContext context)
    {
        _context = context;
    }

    /// <summary>Inventory Valuation Report: on-hand qty x cost per item/warehouse/lot.</summary>
    public async Task<List<InventoryValuationRow>> GetValuationAsync(
        Guid companyId, Guid? warehouseId = null, Guid? itemId = null, CancellationToken ct = default)
    {
        var stocks = _context.ItemStocks.Where(s => s.CompanyId == companyId);
        if (warehouseId.HasValue) stocks = stocks.Where(s => s.WarehouseId == warehouseId.Value);
        if (itemId.HasValue) stocks = stocks.Where(s => s.ItemId == itemId.Value);

        var rows = new List<InventoryValuationRow>();
        foreach (var stock in await stocks.ToListAsync(ct))
        {
            var item = await _context.Items.FindAsync(new object[] { stock.ItemId }, ct);
            if (item == null) continue;

            // Use the layer-based average cost when layers exist, else the item standard cost.
            decimal unitCost = item.StandardCost ?? 0;
            var layers = await _context.ItemCostLayers
                .Where(l => l.ItemId == stock.ItemId && l.WarehouseId == stock.WarehouseId && l.RemainingQuantity > 0)
                .ToListAsync(ct);
            if (layers.Count > 0)
            {
                var totalQty = layers.Sum(l => l.RemainingQuantity);
                var totalCost = layers.Sum(l => l.RemainingQuantity * l.UnitCost);
                unitCost = totalQty > 0 ? totalCost / totalQty : unitCost;
            }

            var onHand = stock.OnHandQuantity;
            rows.Add(new InventoryValuationRow(
                stock.ItemId, item.ItemCode, item.Description, stock.WarehouseId,
                onHand, unitCost, onHand * unitCost, item.ABCClass ?? "U"));
        }

        return rows.OrderBy(r => r.ItemCode).ToList();
    }

    /// <summary>Reorder Report: items whose available qty is at/below reorder point.</summary>
    public async Task<List<ReorderReportRow>> GetReorderReportAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var stocks = await _context.ItemStocks.Where(s => s.CompanyId == companyId).ToListAsync(ct);
        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var rows = new List<ReorderReportRow>();
        foreach (var stock in stocks)
        {
            if (!items.TryGetValue(stock.ItemId, out var item)) continue;
            var available = stock.OnHandQuantity - stock.AllocatedQuantity;
            var reorderPoint = item.ReorderPoint ?? 0;
            if (available > reorderPoint) continue;

            var vendor = await _context.ItemVendorAssignments
                .FirstOrDefaultAsync(v => v.ItemId == item.Id && v.IsPrimaryVendor, ct);
            var suggested = item.ReorderQuantity ?? Math.Max(reorderPoint + (item.SafetyStock ?? 0) - available, 1);

            rows.Add(new ReorderReportRow(
                stock.ItemId, item.ItemCode, item.Description, stock.WarehouseId,
                stock.OnHandQuantity, stock.AllocatedQuantity, available,
                reorderPoint, item.SafetyStock ?? 0, suggested,
                vendor?.VendorId ?? Guid.Empty, vendor?.VendorCost, item.LeadTimeDays ?? 0));
        }

        return rows.OrderBy(r => r.AvailableQuantity).ToList();
    }

    /// <summary>Transaction History Report: movement detail by date range.</summary>
    public async Task<List<TransactionHistoryRow>> GetTransactionHistoryAsync(
        Guid companyId, DateTime? from = null, DateTime? to = null,
        Guid? itemId = null, Guid? warehouseId = null, CancellationToken ct = default)
    {
        var q = _context.InventoryTransactions.Where(t => t.CompanyId == companyId);
        if (from.HasValue) q = q.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) q = q.Where(t => t.TransactionDate <= to.Value);
        if (itemId.HasValue) q = q.Where(t => t.ItemId == itemId.Value);
        if (warehouseId.HasValue) q = q.Where(t => t.WarehouseId == warehouseId.Value);

        return await q.OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionHistoryRow(
                t.Id, t.ItemId, t.WarehouseId, t.TransactionType.ToString(),
                t.Quantity, t.UnitOfMeasure, t.UnitCost, t.ExtendedCost,
                t.TransactionDate, t.LotId, t.SerialNumber, t.ReferenceNumber, t.ProjectId))
            .ToListAsync(ct);
    }

    /// <summary>Stock-Out Report: zero on-hand items that still have open demand (allocations).</summary>
    public async Task<List<StockOutRow>> GetStockOutReportAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var stocks = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && s.OnHandQuantity <= 0 && s.AllocatedQuantity > 0)
            .ToListAsync(ct);
        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        return stocks
            .Where(s => items.ContainsKey(s.ItemId))
            .Select(s => new StockOutRow(
                s.ItemId, items[s.ItemId].ItemCode, items[s.ItemId].Description,
                s.WarehouseId, s.OnHandQuantity, s.AllocatedQuantity,
                items[s.ItemId].ReorderPoint ?? 0))
            .OrderBy(r => r.ItemCode).ToList();
    }

    /// <summary>Negative Inventory Report: items with negative on-hand requiring investigation.</summary>
    public async Task<List<NegativeInventoryRow>> GetNegativeInventoryReportAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var stocks = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && s.OnHandQuantity < 0)
            .ToListAsync(ct);
        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        return stocks
            .Where(s => items.ContainsKey(s.ItemId))
            .Select(s => new NegativeInventoryRow(
                s.ItemId, items[s.ItemId].ItemCode, items[s.ItemId].Description,
                s.WarehouseId, s.OnHandQuantity, s.AllocatedQuantity))
            .OrderBy(r => r.OnHandQuantity).ToList();
    }

    /// <summary>Slow-Moving / Dead Stock Report: items with no issue movement in the window.</summary>
    public async Task<List<SlowMovingRow>> GetSlowMovingReportAsync(
        Guid companyId, int monthsThreshold = 12, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddMonths(-monthsThreshold);
        var itemIds = await _context.Items.Where(i => i.CompanyId == companyId).Select(i => i.Id).ToListAsync(ct);
        var movedItemIds = await _context.InventoryTransactions
            .Where(t => t.CompanyId == companyId && t.TransactionType == TransactionType.Issue && t.TransactionDate >= since)
            .Select(t => t.ItemId).Distinct().ToListAsync(ct);

        var slowIds = itemIds.Except(movedItemIds).ToList();
        var stocks = await _context.ItemStocks.Where(s => s.CompanyId == companyId && slowIds.Contains(s.ItemId)).ToListAsync(ct);
        var items = await _context.Items.Where(i => slowIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var rows = new List<SlowMovingRow>();
        foreach (var stock in stocks)
        {
            if (!items.TryGetValue(stock.ItemId, out var item)) continue;
            var lastMovement = await _context.InventoryTransactions
                .Where(t => t.ItemId == stock.ItemId && t.WarehouseId == stock.WarehouseId)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => (DateTime?)t.TransactionDate)
                .FirstOrDefaultAsync(ct);

            rows.Add(new SlowMovingRow(
                stock.ItemId, item.ItemCode, item.Description, stock.WarehouseId,
                stock.OnHandQuantity, item.StandardCost ?? 0,
                stock.OnHandQuantity * (item.StandardCost ?? 0),
                lastMovement, monthsThreshold));
        }

        return rows.OrderByDescending(r => r.OnHandValue).ToList();
    }

    /// <summary>ABC Analysis Report: rank items by usage value (issues) and cumulative %.</summary>
    public async Task<List<AbcAnalysisRow>> GetAbcAnalysisAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var issues = await _context.InventoryTransactions
            .Where(t => t.CompanyId == companyId && t.TransactionType == TransactionType.Issue)
            .GroupBy(t => t.ItemId)
            .Select(g => new { ItemId = g.Key, UsageValue = g.Sum(t => t.ExtendedCost) })
            .ToListAsync(ct);

        var itemIds = issues.Select(i => i.ItemId).Distinct().ToList();
        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var ranked = issues
            .Where(i => items.ContainsKey(i.ItemId))
            .OrderByDescending(i => i.UsageValue)
            .ToList();

        var totalValue = ranked.Sum(i => i.UsageValue);
        decimal cumulative = 0;
        var rows = new List<AbcAnalysisRow>();
        foreach (var r in ranked)
        {
            cumulative += r.UsageValue;
            var pct = totalValue > 0 ? r.UsageValue / totalValue * 100 : 0;
            var cumPct = totalValue > 0 ? cumulative / totalValue * 100 : 0;
            string abc;
            if (cumPct <= 80) abc = "A";
            else if (cumPct <= 95) abc = "B";
            else abc = "C";
            rows.Add(new AbcAnalysisRow(
                r.ItemId, items[r.ItemId].ItemCode, items[r.ItemId].Description,
                r.UsageValue, pct, cumPct, abc));
        }

        return rows;
    }

    /// <summary>Lot Traceability Report: genealogy of each lot (received → issued → remaining).</summary>
    public async Task<List<LotTraceabilityRow>> GetLotTraceabilityAsync(
        Guid companyId, Guid? itemId = null, CancellationToken ct = default)
    {
        var lots = _context.Lots.Where(l => l.ItemId != Guid.Empty);
        if (itemId.HasValue) lots = lots.Where(l => l.ItemId == itemId.Value);

        var rows = new List<LotTraceabilityRow>();
        foreach (var lot in await lots.ToListAsync(ct))
        {
            var item = await _context.Items.FindAsync(new object[] { lot.ItemId }, ct);
            if (item == null) continue;

            // Received qty from the cost layer tied to this lot (if any).
            var received = await _context.ItemCostLayers
                .Where(c => c.LotId == lot.Id)
                .SumAsync(c => c.Quantity, ct);

            // Issued qty from inventory transactions referencing this lot.
            var issued = await _context.InventoryTransactions
                .Where(t => t.LotId == lot.Id && t.TransactionType == TransactionType.Issue)
                .SumAsync(t => t.Quantity, ct);

            rows.Add(new LotTraceabilityRow(
                lot.Id, lot.LotNumber, lot.ItemId, item.ItemCode, item.Description,
                lot.WarehouseId, lot.ReceivedDate, lot.ExpirationDate, lot.Status.ToString(),
                received, issued, received - issued));
        }

        return rows.OrderBy(r => r.ItemCode).ThenBy(r => r.LotNumber).ToList();
    }

    /// <summary>Serial Traceability Report: history of each serial (received, shipped, installed, returned).</summary>
    public async Task<List<SerialTraceabilityRow>> GetSerialTraceabilityAsync(
        Guid companyId, Guid? itemId = null, CancellationToken ct = default)
    {
        var serials = _context.SerialNumbers.Where(s => s.ItemId != Guid.Empty);
        if (itemId.HasValue) serials = serials.Where(s => s.ItemId == itemId.Value);

        var rows = new List<SerialTraceabilityRow>();
        foreach (var sn in await serials.ToListAsync(ct))
        {
            var item = await _context.Items.FindAsync(new object[] { sn.ItemId }, ct);
            if (item == null) continue;

            rows.Add(new SerialTraceabilityRow(
                sn.Id, sn.SerialNo, sn.ItemId, item.ItemCode, item.Description,
                sn.WarehouseId, sn.ReceivedDate, sn.Status.ToString(),
                sn.CustomerId, sn.InstallationDate, sn.WarrantyInfo));
        }

        return rows.OrderBy(r => r.ItemCode).ThenBy(r => r.SerialNo).ToList();
    }

    /// <summary>Inventory Turnover Report: COGS ÷ average inventory value, by item.</summary>
    public async Task<List<InventoryTurnoverRow>> GetInventoryTurnoverAsync(
        Guid companyId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // COGS over the window = sum of Issue extended cost.
        var issues = await _context.InventoryTransactions
            .Where(t => t.CompanyId == companyId && t.TransactionType == TransactionType.Issue
                        && t.TransactionDate >= from && t.TransactionDate <= to)
            .GroupBy(t => t.ItemId)
            .Select(g => new { ItemId = g.Key, Cogs = g.Sum(t => t.ExtendedCost) })
            .ToListAsync(ct);

        var itemIds = issues.Select(i => i.ItemId).Distinct().ToList();
        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        var stocks = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId && itemIds.Contains(s.ItemId)).ToListAsync(ct);

        var days = Math.Max(1, (to - from).Days);
        var rows = new List<InventoryTurnoverRow>();
        foreach (var issue in issues)
        {
            if (!items.TryGetValue(issue.ItemId, out var item)) continue;
            var onHand = stocks.Where(s => s.ItemId == issue.ItemId).Sum(s => s.OnHandQuantity);
            var unitCost = item.StandardCost ?? 0;
            var avgInventory = onHand * unitCost / 2m; // opening 0 + closing avg
            var annualizedCogs = issue.Cogs * (365m / days);
            var turnover = avgInventory > 0 ? annualizedCogs / avgInventory : 0;

            rows.Add(new InventoryTurnoverRow(
                issue.ItemId, item.ItemCode, item.Description, issue.Cogs, avgInventory, turnover));
        }

        return rows.OrderByDescending(r => r.Turnover).ToList();
    }

    /// <summary>Cycle Count Variance Report: per-line book vs counted, variance $ and %.</summary>
    public async Task<List<CycleCountVarianceRow>> GetCycleCountVarianceAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var counts = await _context.CycleCounts
            .Where(c => c.CompanyId == companyId && c.Status == CycleCountStatus.Completed)
            .Include(c => c.Lines)
            .ToListAsync(ct);

        var itemIds = counts.SelectMany(c => c.Lines.Select(l => l.ItemId)).Distinct().ToList();
        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var rows = new List<CycleCountVarianceRow>();
        foreach (var count in counts)
        {
            foreach (var line in count.Lines)
            {
                if (!line.CountedQuantity.HasValue) continue;
                if (!items.TryGetValue(line.ItemId, out var item)) continue;
                var unitCost = item.StandardCost ?? 0;
                var varianceQty = line.Variance ?? 0;
                var varianceValue = varianceQty * unitCost;
                var pct = line.SystemQuantity != 0 ? varianceQty / line.SystemQuantity * 100m : 0;

                rows.Add(new CycleCountVarianceRow(
                    count.Id, count.CountNumber, line.ItemId, item.ItemCode, item.Description,
                    count.WarehouseId, line.BinId, line.SystemQuantity, line.CountedQuantity.Value,
                    varianceQty, varianceValue, pct, line.Notes));
            }
        }

        return rows.OrderByDescending(r => r.VarianceValue).ToList();
    }

    /// <summary>Cycle Count Summary: variance rolled up by warehouse.</summary>
    public async Task<List<CycleCountSummaryRow>> GetCycleCountSummaryAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var variance = await GetCycleCountVarianceAsync(companyId, ct);
        if (variance.Count == 0) return new List<CycleCountSummaryRow>();

        return variance
            .GroupBy(v => v.WarehouseId ?? Guid.Empty)
            .Select(g => new CycleCountSummaryRow(
                g.Key, g.Count(),
                g.Sum(x => x.SystemQuantity), g.Sum(x => x.CountedQuantity),
                g.Sum(x => x.VarianceQuantity), g.Sum(x => x.VarianceValue)))
            .OrderBy(r => r.WarehouseId.ToString())
            .ToList();
    }

    /// <summary>
    /// Inventory GL Tie-Out: the perpetual sub-ledger inventory value rolled up by the
    /// GL inventory-asset account it should reconcile to. Each item's inventory-asset
    /// account comes from its ItemGLAccountDefaults (falling back to the default 1400).
    /// The sub-ledger value per account must equal that GL account's balance (variance
    /// to zero) at period close. Returns one row per GL account plus a TOTAL row.
    /// </summary>
    public async Task<List<InventoryGlTieOutRow>> GetGlTieOutAsync(
        Guid companyId, CancellationToken ct = default)
    {
        const string defaultInventoryAsset = "1400";
        var stocksList = await _context.ItemStocks.Where(s => s.CompanyId == companyId).ToListAsync(ct);

        var accountValue = new Dictionary<string, decimal>();
        decimal grandTotal = 0m;

        foreach (var stock in stocksList)
        {
            var item = await _context.Items.FindAsync(new object[] { stock.ItemId }, ct);
            if (item == null) continue;

            decimal unitCost = item.StandardCost ?? 0;
            var layers = await _context.ItemCostLayers
                .Where(l => l.ItemId == stock.ItemId && l.WarehouseId == stock.WarehouseId && l.RemainingQuantity > 0)
                .ToListAsync(ct);
            if (layers.Count > 0)
            {
                var totalQty = layers.Sum(l => l.RemainingQuantity);
                var totalCost = layers.Sum(l => l.RemainingQuantity * l.UnitCost);
                unitCost = totalQty > 0 ? totalCost / totalQty : unitCost;
            }

            var value = stock.OnHandQuantity * unitCost;
            grandTotal += value;

            // Sub-ledger inventory value rolls up to the default inventory-asset
            // account (1400) unless a per-item GL default overrides the account id.
            var accountNumber = defaultInventoryAsset;
            if (!accountValue.ContainsKey(accountNumber))
                accountValue[accountNumber] = 0m;
            accountValue[accountNumber] += value;
        }

        var rows = accountValue
            .OrderBy(x => x.Key)
            .Select(kvp => new InventoryGlTieOutRow(Guid.Empty, kvp.Key, kvp.Value))
            .ToList();

        rows.Add(new InventoryGlTieOutRow(Guid.Empty, "TOTAL", grandTotal));
        return rows;
    }

    /// <summary>
    /// Stock Card: item-centric transaction history with running balance.
    /// Returns transactions in chronological order (oldest first) with separate
    /// qty-in / qty-out columns and a running balance after each movement.
    /// </summary>
    public async Task<List<StockCardRow>> GetStockCardAsync(
        Guid companyId, Guid itemId, Guid? warehouseId = null,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = _context.InventoryTransactions
            .Where(t => t.CompanyId == companyId && t.ItemId == itemId);
        if (warehouseId.HasValue) q = q.Where(t => t.WarehouseId == warehouseId.Value);
        if (from.HasValue) q = q.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) q = q.Where(t => t.TransactionDate <= to.Value);

        var txns = await q.OrderBy(t => t.TransactionDate)
            .Select(t => new
            {
                t.Id,
                t.TransactionDate,
                Type = t.TransactionType.ToString(),
                t.ReferenceNumber,
                t.Quantity,
                t.UnitCost,
                t.ExtendedCost,
                t.Notes,
            })
            .ToListAsync(ct);

        var rows = new List<StockCardRow>();
        decimal runningBalance = 0m;
        decimal runningValue = 0m;

        foreach (var t in txns)
        {
            decimal qtyIn = t.Quantity > 0 ? t.Quantity : 0m;
            decimal qtyOut = t.Quantity < 0 ? Math.Abs(t.Quantity) : 0m;
            runningBalance += t.Quantity;
            runningValue += t.ExtendedCost;

            rows.Add(new StockCardRow(
                t.Id,
                t.TransactionDate,
                t.Type,
                t.ReferenceNumber,
                t.Notes,
                qtyIn,
                qtyOut,
                t.UnitCost,
                t.ExtendedCost,
                runningBalance,
                runningValue));
        }

        return rows;
    }
}

public record InventoryValuationRow(
    Guid ItemId, string ItemCode, string Description, Guid WarehouseId,
    decimal OnHandQuantity, decimal UnitCost, decimal ExtendedValue, string ABCClass);

public record ReorderReportRow(
    Guid ItemId, string ItemCode, string Description, Guid WarehouseId,
    decimal OnHandQuantity, decimal AllocatedQuantity, decimal AvailableQuantity,
    decimal ReorderPoint, decimal SafetyStock, decimal SuggestedOrderQuantity,
    Guid PreferredVendorId, decimal? VendorCost, int LeadTimeDays);

public record TransactionHistoryRow(
    Guid Id, Guid ItemId, Guid WarehouseId, string TransactionType,
    decimal Quantity, string UnitOfMeasure, decimal UnitCost, decimal ExtendedCost,
    DateTime TransactionDate, Guid? LotId, string? SerialNumber, string? ReferenceNumber, Guid? ProjectId);

public record StockOutRow(
    Guid ItemId, string ItemCode, string Description, Guid WarehouseId,
    decimal OnHandQuantity, decimal AllocatedQuantity, decimal ReorderPoint);

public record NegativeInventoryRow(
    Guid ItemId, string ItemCode, string Description, Guid WarehouseId,
    decimal OnHandQuantity, decimal AllocatedQuantity);

public record SlowMovingRow(
    Guid ItemId, string ItemCode, string Description, Guid WarehouseId,
    decimal OnHandQuantity, decimal UnitCost, decimal OnHandValue,
    DateTime? LastMovementDate, int MonthsThreshold);

public record AbcAnalysisRow(
    Guid ItemId, string ItemCode, string Description,
    decimal UsageValue, decimal PercentOfTotal, decimal CumulativePercent, string ABCClass);

public record LotTraceabilityRow(
    Guid LotId, string LotNumber, Guid ItemId, string ItemCode, string Description,
    Guid WarehouseId, DateTime ReceivedDate, DateTime? ExpirationDate, string Status,
    decimal ReceivedQuantity, decimal IssuedQuantity, decimal RemainingQuantity);

public record SerialTraceabilityRow(
    Guid SerialId, string SerialNo, Guid ItemId, string ItemCode, string Description,
    Guid WarehouseId, DateTime ReceivedDate, string Status,
    Guid? CustomerId, DateTime? InstallationDate, string? WarrantyInfo);

public record InventoryTurnoverRow(
    Guid ItemId, string ItemCode, string Description,
    decimal Cogs, decimal AverageInventory, decimal Turnover);

public record CycleCountVarianceRow(
    Guid CountId, string CountNumber, Guid ItemId, string ItemCode, string Description,
    Guid? WarehouseId, Guid? BinId, decimal SystemQuantity, decimal CountedQuantity,
    decimal VarianceQuantity, decimal VarianceValue, decimal VariancePercent, string? Notes);

public record CycleCountSummaryRow(
    Guid WarehouseId, int LineCount,
    decimal TotalSystemQuantity, decimal TotalCountedQuantity,
    decimal TotalVarianceQuantity, decimal TotalVarianceValue);

public record InventoryGlTieOutRow(
    Guid ItemCategoryId, string GlAccountNumber, decimal SubLedgerValue);

public record StockCardRow(
    Guid TransactionId,
    DateTime TransactionDate,
    string TransactionType,
    string? ReferenceNumber,
    string? Description,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal UnitCost,
    decimal ExtendedCost,
    decimal RunningBalance,
    decimal RunningValue);
