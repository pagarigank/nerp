// <copyright file="LotSerialTrackingService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Application.Services;

public class LotSerialTrackingService
{
    private readonly InventoryDbContext _context;

    public LotSerialTrackingService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task ValidateLotTrackingAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        string? lotNumber,
        TransactionType transactionType,
        bool allowExpiredLotOverride = false,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new ArgumentException($"Item {itemId} not found.");
        }

        if (!item.IsLotControlled)
        {
            return; // No lot tracking required
        }

        if (transactionType == TransactionType.Receipt || transactionType == TransactionType.ProductionReceipt)
        {
            // On receipt, lot number is required
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                throw new InvalidOperationException($"Item {item.ItemCode} is lot-controlled. Lot number is required for receipt.");
            }
        }
        else if (transactionType == TransactionType.Issue ||
                 transactionType == TransactionType.Shipment ||
                 transactionType == TransactionType.TransferOut)
        {
            // On issue, lot number is required to track which lot is being consumed
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                throw new InvalidOperationException($"Item {item.ItemCode} is lot-controlled. Lot number is required for issue.");
            }

            // Verify lot exists and has sufficient quantity
            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.ItemId == itemId
                                      && l.WarehouseId == warehouseId
                                      && l.LotNumber == lotNumber, cancellationToken);

            if (lot == null)
                throw new InvalidOperationException($"Lot {lotNumber} not found for item {item.ItemCode} in warehouse.");

            // Check available quantity in this lot
            var availableInLot = await GetAvailableQuantityInLotAsync(itemId, warehouseId, lotNumber, cancellationToken);
            if (availableInLot < quantity)
            {
                throw new InvalidOperationException($"Insufficient quantity in lot {lotNumber}. Available: {availableInLot}, Requested: {quantity}");
            }

            // Check if lot is expired (Phase 7 gap: allow override for scrap/write-off)
            if (lot.IsExpired() && !allowExpiredLotOverride)
            {
                throw new InvalidOperationException($"Lot {lotNumber} is expired and cannot be issued. Use allowExpiredLotOverride=true for scrap/write-off.");
            }

            // Check if lot is quarantined
            if (lot.Status == LotStatus.Quarantine)
            {
                throw new InvalidOperationException($"Lot {lotNumber} is in quarantine and cannot be issued.");
            }
        }
        else if (transactionType == TransactionType.Adjustment || transactionType == TransactionType.TransferIn)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
                return;

            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.ItemId == itemId
                                      && l.WarehouseId == warehouseId
                                      && l.LotNumber == lotNumber, cancellationToken);

            if (lot == null)
                throw new InvalidOperationException($"Lot {lotNumber} not found for item {item.ItemCode} in warehouse.");
        }
    }

    public async Task ValidateSerialTrackingAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        string? serialNumber,
        TransactionType transactionType,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new ArgumentException($"Item {itemId} not found.");
        }

        if (!item.IsSerialControlled)
        {
            return; // No serial tracking required
        }

        // Serial-controlled items must have quantity of 1 per serial number
        if (quantity != 1)
        {
            throw new InvalidOperationException($"Serial-controlled items must be transacted one at a time (quantity = 1).");
        }

        if (transactionType == TransactionType.Receipt || transactionType == TransactionType.ProductionReceipt)
        {
            // On receipt, serial number is required
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                throw new InvalidOperationException($"Item {item.ItemCode} is serial-controlled. Serial number is required for receipt.");
            }

            // Check if serial number already exists
            var existingSerial = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.ItemId == itemId && s.SerialNo == serialNumber, cancellationToken);

            if (existingSerial != null)
            {
                throw new InvalidOperationException($"Serial number {serialNumber} already exists in the system.");
            }
        }
        else if (transactionType == TransactionType.Issue ||
                 transactionType == TransactionType.Shipment ||
                 transactionType == TransactionType.TransferOut)
        {
            // On issue, serial number is required
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                throw new InvalidOperationException($"Item {item.ItemCode} is serial-controlled. Serial number is required for issue.");
            }

            // Verify serial exists and is available
            var serial = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.ItemId == itemId
                                      && s.WarehouseId == warehouseId
                                      && s.SerialNo == serialNumber, cancellationToken);

            if (serial == null)
            {
                throw new InvalidOperationException($"Serial number {serialNumber} not found for item {item.ItemCode} in warehouse.");
            }

            if (serial.Status != SerialStatus.InStock)
            {
                throw new InvalidOperationException($"Serial number {serialNumber} is not available (Status: {serial.Status}).");
            }
        }
        else if (transactionType == TransactionType.Adjustment || transactionType == TransactionType.TransferIn)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return;

            var serial = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.ItemId == itemId
                                      && s.WarehouseId == warehouseId
                                      && s.SerialNo == serialNumber, cancellationToken);

            if (serial == null)
                throw new InvalidOperationException($"Serial number {serialNumber} not found for item {item.ItemCode} in warehouse.");
        }
    }

    public async Task<Lot> GetOrCreateLotAsync(
        Guid itemId,
        Guid warehouseId,
        string lotNumber,
        DateTime receivedDate,
        DateTime? expirationDate,
        string? vendorLotNumber,
        CancellationToken cancellationToken = default)
    {
        var lot = await _context.Lots
            .FirstOrDefaultAsync(l => l.ItemId == itemId
                                  && l.WarehouseId == warehouseId
                                  && l.LotNumber == lotNumber, cancellationToken);

        if (lot == null)
        {
            lot = new Lot(lotNumber, itemId, warehouseId, receivedDate, expirationDate, vendorLotNumber);
            _context.Lots.Add(lot);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return lot;
    }

    public async Task<SerialNumber> CreateSerialNumberAsync(
        Guid itemId,
        Guid warehouseId,
        string serialNumber,
        DateTime receivedDate,
        string? warrantyInfo,
        DateTime? installationDate,
        Guid? customerId,
        CancellationToken cancellationToken = default)
    {
        var serial = new SerialNumber(serialNumber, itemId, warehouseId, receivedDate, warrantyInfo, installationDate, customerId);
        _context.SerialNumbers.Add(serial);
        await _context.SaveChangesAsync(cancellationToken);
        return serial;
    }

    public async Task ReleaseSerialNumberAsync(
        Guid itemId,
        Guid warehouseId,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        var serial = await _context.SerialNumbers
            .FirstOrDefaultAsync(s => s.ItemId == itemId
                                  && s.WarehouseId == warehouseId
                                  && s.SerialNo == serialNumber, cancellationToken);

        if (serial != null && serial.Status == SerialStatus.InStock)
        {
            // This is called when the item is issued/shipped
            serial.Ship(Guid.Empty); // In real implementation, pass customer ID
            _context.SerialNumbers.Update(serial);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<decimal> GetAvailableQuantityInLotAsync(
        Guid itemId,
        Guid warehouseId,
        string lotNumber,
        CancellationToken cancellationToken = default)
    {
        var lot = await _context.Lots
            .FirstOrDefaultAsync(l => l.ItemId == itemId
                                  && l.WarehouseId == warehouseId
                                  && l.LotNumber == lotNumber, cancellationToken);

        if (lot == null)
        {
            return 0;
        }

        var transactions = await _context.InventoryTransactions
            .Where(t => t.ItemId == itemId
                     && t.WarehouseId == warehouseId
                     && t.LotId == lot.Id)
            .ToListAsync(cancellationToken);

        decimal received = transactions.Where(t => t.Quantity > 0).Sum(t => t.Quantity);
        decimal issued = transactions.Where(t => t.Quantity < 0).Sum(t => Math.Abs(t.Quantity));

        return received - issued;
    }

    public async Task<List<LotAvailabilityDto>> GetLotAvailabilityAsync(
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var lots = await _context.Lots
            .Where(l => l.ItemId == itemId && l.WarehouseId == warehouseId && l.Status == LotStatus.Active)
            .ToListAsync(cancellationToken);

        var result = new List<LotAvailabilityDto>();

        foreach (var lot in lots)
        {
            var available = await GetAvailableQuantityInLotAsync(itemId, warehouseId, lot.LotNumber, cancellationToken);
            if (available > 0)
            {
                result.Add(new LotAvailabilityDto
                {
                    LotNumber = lot.LotNumber,
                    VendorLotNumber = lot.VendorLotNumber,
                    ExpirationDate = lot.ExpirationDate,
                    AvailableQuantity = available,
                    IsExpired = lot.IsExpired(),
                    Status = lot.Status.ToString(),
                });
            }
        }

        return result.OrderBy(l => l.ExpirationDate).ToList();
    }

    public async Task<List<SerialNumberDto>> GetAvailableSerialNumbersAsync(
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var serials = await _context.SerialNumbers
            .Where(s => s.ItemId == itemId && s.WarehouseId == warehouseId && s.Status == SerialStatus.InStock)
            .ToListAsync(cancellationToken);

        return serials.Select(s => new SerialNumberDto
        {
            SerialNumber = s.SerialNo,
            ReceivedDate = s.ReceivedDate,
            WarrantyInfo = s.WarrantyInfo,
            CustomerId = s.CustomerId,
        }).ToList();
    }
}

public class LotAvailabilityDto
{
    public string LotNumber { get; set; } = string.Empty;
    public string? VendorLotNumber { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal AvailableQuantity { get; set; }
    public bool IsExpired { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SerialNumberDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string? WarrantyInfo { get; set; }
    public Guid? CustomerId { get; set; }
}