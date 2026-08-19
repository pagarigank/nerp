// <copyright file="CostingService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Application.Services;

public class CostingService
{
    private readonly InventoryDbContext _context;

    public CostingService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CalculateIssueCostAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        CostingMethod costingMethod,
        Guid? binId = null,
        string? lotNumber = null,
        string? serialNumber = null,
        CancellationToken cancellationToken = default)
    {
        return costingMethod switch
        {
            CostingMethod.FIFO => await CalculateFifoCostAsync(itemId, warehouseId, quantity, binId, lotNumber, serialNumber, cancellationToken),
            CostingMethod.LIFO => await CalculateLifoCostAsync(itemId, warehouseId, quantity, binId, lotNumber, serialNumber, cancellationToken),
            CostingMethod.Average => await CalculateAverageCostAsync(itemId, warehouseId, cancellationToken),
            CostingMethod.Standard => await GetStandardCostAsync(itemId, cancellationToken),
            CostingMethod.LotSpecific => await CalculateLotSpecificCostAsync(itemId, warehouseId, lotNumber!, cancellationToken),
            _ => throw new ArgumentException($"Unsupported costing method: {costingMethod}", nameof(costingMethod)),
        };
    }

    public async Task<List<CostLayerConsumption>> GetFifoConsumptionAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        Guid? binId = null,
        string? lotNumber = null,
        string? serialNumber = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemCostLayers
            .Where(l => l.CompanyId == GetCompanyId(itemId)
                     && l.ItemId == itemId
                     && l.WarehouseId == warehouseId
                     && l.RemainingQuantity > 0);

        if (binId.HasValue)
        {
            // Note: ItemCostLayer doesn't have BinId, would need to track via transactions
        }

        if (!string.IsNullOrEmpty(lotNumber))
        {
            var lot = await _context.Lots.FirstOrDefaultAsync(l => l.ItemId == itemId && l.LotNumber == lotNumber, cancellationToken);
            if (lot != null)
            {
                query = query.Where(l => l.LotId == lot.Id);
            }
        }

        var layers = await query
            .OrderBy(l => l.ReceivedDate)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        var consumption = new List<CostLayerConsumption>();
        decimal remainingToConsume = quantity;

        foreach (var layer in layers)
        {
            if (remainingToConsume <= 0)
            {
                break;
            }

            var consumeQty = Math.Min(layer.RemainingQuantity, remainingToConsume);
            consumption.Add(new CostLayerConsumption
            {
                CostLayerId = layer.Id,
                Quantity = consumeQty,
                UnitCost = layer.UnitCost,
            });

            remainingToConsume -= consumeQty;
        }

        if (remainingToConsume > 0.0001m)
        {
            throw new InvalidOperationException($"Insufficient inventory layers for item {itemId}. Requested: {quantity}, Available: {quantity - remainingToConsume}");
        }

        return consumption;
    }

    public async Task<List<CostLayerConsumption>> GetLifoConsumptionAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        Guid? binId = null,
        string? lotNumber = null,
        string? serialNumber = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemCostLayers
            .Where(l => l.CompanyId == GetCompanyId(itemId)
                     && l.ItemId == itemId
                     && l.WarehouseId == warehouseId
                     && l.RemainingQuantity > 0);

        if (!string.IsNullOrEmpty(lotNumber))
        {
            var lot = await _context.Lots.FirstOrDefaultAsync(l => l.ItemId == itemId && l.LotNumber == lotNumber, cancellationToken);
            if (lot != null)
            {
                query = query.Where(l => l.LotId == lot.Id);
            }
        }

        var layers = await query
            .OrderByDescending(l => l.ReceivedDate)
            .ThenByDescending(l => l.Id)
            .ToListAsync(cancellationToken);

        var consumption = new List<CostLayerConsumption>();
        decimal remainingToConsume = quantity;

        foreach (var layer in layers)
        {
            if (remainingToConsume <= 0)
            {
                break;
            }

            var consumeQty = Math.Min(layer.RemainingQuantity, remainingToConsume);
            consumption.Add(new CostLayerConsumption
            {
                CostLayerId = layer.Id,
                Quantity = consumeQty,
                UnitCost = layer.UnitCost,
            });

            remainingToConsume -= consumeQty;
        }

        if (remainingToConsume > 0.0001m)
        {
            throw new InvalidOperationException($"Insufficient inventory layers for item {itemId}. Requested: {quantity}, Available: {quantity - remainingToConsume}");
        }

        return consumption;
    }

    private async Task<decimal> CalculateFifoCostAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        Guid? binId,
        string? lotNumber,
        string? serialNumber,
        CancellationToken cancellationToken)
    {
        var consumption = await GetFifoConsumptionAsync(itemId, warehouseId, quantity, binId, lotNumber, serialNumber, cancellationToken);
        return consumption.Sum(c => c.Quantity * c.UnitCost) / quantity;
    }

    private async Task<decimal> CalculateLifoCostAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        Guid? binId,
        string? lotNumber,
        string? serialNumber,
        CancellationToken cancellationToken)
    {
        var consumption = await GetLifoConsumptionAsync(itemId, warehouseId, quantity, binId, lotNumber, serialNumber, cancellationToken);
        return consumption.Sum(c => c.Quantity * c.UnitCost) / quantity;
    }

    public async Task<decimal> CalculateAverageCostAsync(
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var receipts = await _context.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId && t.Quantity > 0)
            .Select(t => new { t.Quantity, t.ExtendedCost })
            .ToListAsync(cancellationToken);

        if (receipts.Count == 0)
        {
            var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
            return item?.StandardCost ?? 0;
        }

        var totalQty = receipts.Sum(r => r.Quantity);
        var totalCost = receipts.Sum(r => r.ExtendedCost);

        return totalQty > 0 ? totalCost / totalQty : 0;
    }

    private async Task<decimal> GetStandardCostAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        return item?.StandardCost ?? 0;
    }

    private async Task<decimal> CalculateLotSpecificCostAsync(
        Guid itemId,
        Guid warehouseId,
        string lotNumber,
        CancellationToken cancellationToken)
    {
        var lot = await _context.Lots.FirstOrDefaultAsync(l => l.ItemId == itemId && l.LotNumber == lotNumber, cancellationToken);
        if (lot == null)
        {
            return await CalculateAverageCostAsync(itemId, warehouseId, cancellationToken);
        }

        // Get cost layers for this specific lot
        var layers = await _context.ItemCostLayers
            .Where(l => l.ItemId == itemId && l.WarehouseId == warehouseId && l.LotId == lot.Id && l.RemainingQuantity > 0)
            .OrderBy(l => l.ReceivedDate)
            .ToListAsync(cancellationToken);

        if (layers.Count == 0)
        {
            return await CalculateAverageCostAsync(itemId, warehouseId, cancellationToken);
        }

        var totalQty = layers.Sum(l => l.RemainingQuantity);
        var totalCost = layers.Sum(l => l.RemainingQuantity * l.UnitCost);

        return totalQty > 0 ? totalCost / totalQty : 0;
    }

    public async Task<decimal> RecalculateAverageCostAsync(
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var newAverageCost = await CalculateAverageCostAsync(itemId, warehouseId, cancellationToken);

        var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        if (item != null && item.CostingMethod == CostingMethod.Average)
        {
            item.UpdateStandardCost(newAverageCost);
            _context.Items.Update(item);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return newAverageCost;
    }

    public async Task CreateCostLayerAsync(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        decimal unitCost,
        DateTime receivedDate,
        Guid? lotId = null,
        string? referenceNumber = null,
        CancellationToken cancellationToken = default)
    {
        var costLayer = new ItemCostLayer(
            companyId,
            itemId,
            warehouseId,
            quantity,
            unitCost,
            receivedDate,
            lotId,
            referenceNumber);

        _context.ItemCostLayers.Add(costLayer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Guid GetCompanyId(Guid itemId)
    {
        var item = _context.Items.Find(itemId);
        return item?.CompanyId ?? Guid.Empty;
    }
}

public class CostLayerConsumption
{
    public Guid CostLayerId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ExtendedCost => Quantity * UnitCost;
}