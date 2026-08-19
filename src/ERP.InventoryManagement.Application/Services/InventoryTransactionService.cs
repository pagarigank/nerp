// <copyright file="InventoryTransactionService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.Services;

using ERP.InventoryManagement.Application.DTOs.Transactions;
using ERP.InventoryManagement.Domain.Entities;
using ERP.InventoryManagement.Domain.Repositories;
using ERP.Modules.Inventory.Application.Services;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Shared.Kernel.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for inventory transaction operations.
/// </summary>
public class InventoryTransactionService : IInventoryTransactionService
{
    private readonly IInventoryTransactionRepository transactionRepository;
    private readonly IItemRepository itemRepository;
    private readonly IWarehouseRepository warehouseRepository;
    private readonly ILogger<InventoryTransactionService> logger;
    private readonly LotSerialTrackingService lotSerialTrackingService;

    public InventoryTransactionService(
        IInventoryTransactionRepository transactionRepository,
        IItemRepository itemRepository,
        IWarehouseRepository warehouseRepository,
        ILogger<InventoryTransactionService> logger,
        LotSerialTrackingService lotSerialTrackingService)
    {
        this.transactionRepository = transactionRepository;
        this.itemRepository = itemRepository;
        this.warehouseRepository = warehouseRepository;
        this.logger = logger;
        this.lotSerialTrackingService = lotSerialTrackingService;
    }

    public async Task<ApiResponse<InventoryTransactionDto>> CreateReceiptAsync(CreateReceiptDto dto, CancellationToken cancellationToken = default)
    {
        // Validate lot/serial tracking
        await lotSerialTrackingService.ValidateLotTrackingAsync(
            dto.ItemId, dto.WarehouseId, dto.Quantity, dto.LotNumber, TransactionType.Receipt, cancellationToken);

        await lotSerialTrackingService.ValidateSerialTrackingAsync(
            dto.ItemId, dto.WarehouseId, dto.Quantity, dto.SerialNumber, TransactionType.Receipt, cancellationToken);

        // Create lot if it doesn't exist
        if (!string.IsNullOrWhiteSpace(dto.LotNumber))
        {
            await lotSerialTrackingService.GetOrCreateLotAsync(
                dto.ItemId, dto.WarehouseId, dto.LotNumber, DateTime.UtcNow, dto.ExpirationDate, null, cancellationToken);
        }

        // Create serial number if provided
        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            await lotSerialTrackingService.CreateSerialNumberAsync(
                dto.ItemId, dto.WarehouseId, dto.SerialNumber, DateTime.UtcNow, null, null, null, cancellationToken);
        }

        var validation = await this.ValidateTransactionAsync(dto.ItemId, dto.WarehouseId, dto.LotNumber, dto.SerialNumber, cancellationToken);
        if (!validation.Success)
        {
            return ApiResponse<InventoryTransactionDto>.Failure(validation.ErrorMessage!);
        }

        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.WarehouseId,
            LocationId = dto.LocationId,
            TransactionType = "RECEIPT",
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            ExtendedCost = dto.Quantity * dto.UnitCost,
            LotNumber = dto.LotNumber,
            SerialNumber = dto.SerialNumber,
            ExpirationDate = dto.ExpirationDate,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            Notes = dto.Notes,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
        };

        await this.transactionRepository.AddAsync(transaction, cancellationToken);

        this.logger.LogInformation("Created inventory receipt transaction {TransactionId} for item {ItemId}, qty {Quantity}", transaction.Id, dto.ItemId, dto.Quantity);

        return ApiResponse<InventoryTransactionDto>.Success(this.MapToDto(transaction));
    }

    public async Task<ApiResponse<InventoryTransactionDto>> CreateIssueAsync(CreateIssueDto dto, CancellationToken cancellationToken = default)
    {
        // Validate lot/serial tracking
        await lotSerialTrackingService.ValidateLotTrackingAsync(
            dto.ItemId, dto.WarehouseId, dto.Quantity, dto.LotNumber, TransactionType.Issue, cancellationToken);

        await lotSerialTrackingService.ValidateSerialTrackingAsync(
            dto.ItemId, dto.WarehouseId, dto.Quantity, dto.SerialNumber, TransactionType.Issue, cancellationToken);

        // Release serial number if applicable
        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            await lotSerialTrackingService.ReleaseSerialNumberAsync(
                dto.ItemId, dto.WarehouseId, dto.SerialNumber, cancellationToken);
        }

        var validation = await this.ValidateTransactionAsync(dto.ItemId, dto.WarehouseId, dto.LotNumber, dto.SerialNumber, cancellationToken);
        if (!validation.Success)
        {
            return ApiResponse<InventoryTransactionDto>.Failure(validation.ErrorMessage!);
        }

        var availableQty = await this.transactionRepository.GetAvailableQuantityAsync(dto.ItemId, dto.WarehouseId, dto.LocationId, dto.LotNumber, dto.SerialNumber, cancellationToken);
        if (availableQty < dto.Quantity)
        {
            return ApiResponse<InventoryTransactionDto>.Failure($"Insufficient quantity available. Available: {availableQty}, Requested: {dto.Quantity}");
        }

        var avgCost = await this.transactionRepository.GetAverageCostAsync(dto.ItemId, dto.WarehouseId, cancellationToken);

        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.WarehouseId,
            LocationId = dto.LocationId,
            TransactionType = "ISSUE",
            Quantity = -dto.Quantity, // Negative for issues
            UnitCost = avgCost,
            ExtendedCost = -dto.Quantity * avgCost,
            LotNumber = dto.LotNumber,
            SerialNumber = dto.SerialNumber,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            Notes = dto.Notes,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
        };

        await this.transactionRepository.AddAsync(transaction, cancellationToken);

        this.logger.LogInformation("Created inventory issue transaction {TransactionId} for item {ItemId}, qty {Quantity}", transaction.Id, dto.ItemId, dto.Quantity);

        return ApiResponse<InventoryTransactionDto>.Success(this.MapToDto(transaction));
    }

    public async Task<ApiResponse<InventoryTransactionDto>> CreateAdjustmentAsync(CreateAdjustmentDto dto, CancellationToken cancellationToken = default)
    {
        // Validate lot/serial tracking for adjustments
        await lotSerialTrackingService.ValidateLotTrackingAsync(
            dto.ItemId, dto.WarehouseId, dto.QuantityAdjustment, dto.LotNumber, TransactionType.Adjustment, cancellationToken);

        await lotSerialTrackingService.ValidateSerialTrackingAsync(
            dto.ItemId, dto.WarehouseId, dto.QuantityAdjustment, dto.SerialNumber, TransactionType.Adjustment, cancellationToken);

        var validation = await this.ValidateTransactionAsync(dto.ItemId, dto.WarehouseId, dto.LotNumber, dto.SerialNumber, cancellationToken);
        if (!validation.Success)
        {
            return ApiResponse<InventoryTransactionDto>.Failure(validation.ErrorMessage!);
        }

        var avgCost = await this.transactionRepository.GetAverageCostAsync(dto.ItemId, dto.WarehouseId, cancellationToken);

        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.WarehouseId,
            LocationId = dto.LocationId,
            TransactionType = "ADJUSTMENT",
            Quantity = dto.QuantityAdjustment,
            UnitCost = avgCost,
            ExtendedCost = dto.QuantityAdjustment * avgCost,
            LotNumber = dto.LotNumber,
            SerialNumber = dto.SerialNumber,
            Notes = $"Reason: {dto.ReasonCode}. {dto.Notes}",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
        };

        await this.transactionRepository.AddAsync(transaction, cancellationToken);

        this.logger.LogInformation("Created inventory adjustment transaction {TransactionId} for item {ItemId}, qty adjustment {Quantity}", transaction.Id, dto.ItemId, dto.QuantityAdjustment);

        return ApiResponse<InventoryTransactionDto>.Success(this.MapToDto(transaction));
    }

    public async Task<ApiResponse<InventoryTransactionDto>> CreateTransferAsync(CreateTransferDto dto, CancellationToken cancellationToken = default)
    {
        // Validate lot/serial tracking for transfer out
        await lotSerialTrackingService.ValidateLotTrackingAsync(
            dto.ItemId, dto.FromWarehouseId, dto.Quantity, dto.LotNumber, TransactionType.TransferOut, cancellationToken);

        await lotSerialTrackingService.ValidateSerialTrackingAsync(
            dto.ItemId, dto.FromWarehouseId, dto.Quantity, dto.SerialNumber, TransactionType.TransferOut, cancellationToken);

        // Validate lot/serial tracking for transfer in
        await lotSerialTrackingService.ValidateLotTrackingAsync(
            dto.ItemId, dto.ToWarehouseId, dto.Quantity, dto.LotNumber, TransactionType.TransferIn, cancellationToken);

        await lotSerialTrackingService.ValidateSerialTrackingAsync(
            dto.ItemId, dto.ToWarehouseId, dto.Quantity, dto.SerialNumber, TransactionType.TransferIn, cancellationToken);

        // Release serial from source warehouse
        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            await lotSerialTrackingService.ReleaseSerialNumberAsync(
                dto.ItemId, dto.FromWarehouseId, dto.SerialNumber, cancellationToken);
        }

        var validation = await this.ValidateTransactionAsync(dto.ItemId, dto.FromWarehouseId, dto.LotNumber, dto.SerialNumber, cancellationToken);
        if (!validation.Success)
        {
            return ApiResponse<InventoryTransactionDto>.Failure(validation.ErrorMessage!);
        }

        var toWarehouse = await this.warehouseRepository.GetByIdAsync(dto.ToWarehouseId, cancellationToken);
        if (toWarehouse == null)
        {
            return ApiResponse<InventoryTransactionDto>.Failure($"Destination warehouse {dto.ToWarehouseId} not found");
        }

        var availableQty = await this.transactionRepository.GetAvailableQuantityAsync(dto.ItemId, dto.FromWarehouseId, dto.FromLocationId, dto.LotNumber, dto.SerialNumber, cancellationToken);
        if (availableQty < dto.Quantity)
        {
            return ApiResponse<InventoryTransactionDto>.Failure($"Insufficient quantity available. Available: {availableQty}, Requested: {dto.Quantity}");
        }

        var avgCost = await this.transactionRepository.GetAverageCostAsync(dto.ItemId, dto.FromWarehouseId, cancellationToken);

        var issueTransaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.FromWarehouseId,
            LocationId = dto.FromLocationId,
            TransactionType = "TRANSFER_OUT",
            Quantity = -dto.Quantity,
            UnitCost = avgCost,
            ExtendedCost = -dto.Quantity * avgCost,
            LotNumber = dto.LotNumber,
            SerialNumber = dto.SerialNumber,
            Notes = $"Transfer to {toWarehouse.WarehouseCode}. {dto.Notes}",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
        };

        var receiptTransaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.ToWarehouseId,
            LocationId = dto.ToLocationId,
            TransactionType = "TRANSFER_IN",
            Quantity = dto.Quantity,
            UnitCost = avgCost,
            ExtendedCost = dto.Quantity * avgCost,
            LotNumber = dto.LotNumber,
            SerialNumber = dto.SerialNumber,
            ReferenceType = "TRANSFER",
            ReferenceId = issueTransaction.Id,
            Notes = $"Transfer from warehouse. {dto.Notes}",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
        };

        await this.transactionRepository.AddAsync(issueTransaction, cancellationToken);
        await this.transactionRepository.AddAsync(receiptTransaction, cancellationToken);

        this.logger.LogInformation("Created inventory transfer from warehouse {FromWarehouseId} to {ToWarehouseId}, item {ItemId}, qty {Quantity}", dto.FromWarehouseId, dto.ToWarehouseId, dto.ItemId, dto.Quantity);

        return ApiResponse<InventoryTransactionDto>.Success(this.MapToDto(receiptTransaction));
    }

    public async Task<ApiResponse<PhysicalCountResultDto>> ProcessPhysicalCountAsync(ProcessPhysicalCountDto dto, CancellationToken cancellationToken = default)
    {
        var variances = new List<CountVarianceDto>();
        int adjustmentsCreated = 0;

        foreach (var line in dto.Lines)
        {
            var systemQty = await this.transactionRepository.GetAvailableQuantityAsync(
                line.ItemId,
                dto.WarehouseId,
                line.LocationId,
                line.LotNumber,
                line.SerialNumber,
                cancellationToken);

            var variance = line.CountedQuantity - systemQty;

            if (Math.Abs(variance) > 0.0001m)
            {
                var item = await this.itemRepository.GetByIdAsync(line.ItemId, cancellationToken);
                var avgCost = await this.transactionRepository.GetAverageCostAsync(line.ItemId, dto.WarehouseId, cancellationToken);

                Guid? adjustmentId = null;

                if (dto.AutoCreateAdjustments)
                {
                    var adjustment = new InventoryTransaction
                    {
                        Id = Guid.NewGuid(),
                        ItemId = line.ItemId,
                        WarehouseId = dto.WarehouseId,
                        LocationId = line.LocationId,
                        TransactionType = "ADJUSTMENT",
                        Quantity = variance,
                        UnitCost = avgCost,
                        ExtendedCost = variance * avgCost,
                        LotNumber = line.LotNumber,
                        SerialNumber = line.SerialNumber,
                        Notes = $"Physical count adjustment. Count date: {dto.CountDate:yyyy-MM-dd}. {line.Notes}",
                        TransactionDate = dto.CountDate,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "SYSTEM",
                    };

                    await this.transactionRepository.AddAsync(adjustment, cancellationToken);
                    adjustmentId = adjustment.Id;
                    adjustmentsCreated++;
                }

                variances.Add(new CountVarianceDto
                {
                    ItemId = line.ItemId,
                    ItemNumber = item?.ItemNumber ?? "UNKNOWN",
                    SystemQuantity = systemQty,
                    CountedQuantity = line.CountedQuantity,
                    Variance = variance,
                    VarianceValue = variance * avgCost,
                    AdjustmentTransactionId = adjustmentId,
                });
            }
        }

        this.logger.LogInformation("Processed physical count for warehouse {WarehouseId}: {TotalLines} lines, {Variances} variances, {Adjustments} adjustments created",
            dto.WarehouseId, dto.Lines.Count, variances.Count, adjustmentsCreated);

        return ApiResponse<PhysicalCountResultDto>.Success(new PhysicalCountResultDto
        {
            TotalLines = dto.Lines.Count,
            AdjustmentsCreated = adjustmentsCreated,
            Variances = variances,
        });
    }

    public async Task<ApiResponse<LotSerialValidationDto>> ValidateLotSerialAsync(ValidateLotSerialDto dto, CancellationToken cancellationToken = default)
    {
        var item = await this.itemRepository.GetByIdAsync(dto.ItemId, cancellationToken);
        if (item == null)
        {
            return ApiResponse<LotSerialValidationDto>.Failure($"Item {dto.ItemId} not found");
        }

        var messages = new List<string>();
        bool isValid = true;

        bool lotExists = false;
        bool serialExists = false;

        if (item.LotTracked && string.IsNullOrWhiteSpace(dto.LotNumber))
        {
            messages.Add("Lot number is required for this item");
            isValid = false;
        }

        if (item.SerialTracked && string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            messages.Add("Serial number is required for this item");
            isValid = false;
        }

        if (!string.IsNullOrWhiteSpace(dto.LotNumber))
        {
            lotExists = await this.transactionRepository.LotExistsAsync(dto.ItemId, dto.LotNumber, cancellationToken);
            if (dto.TransactionType == "ISSUE" && !lotExists)
            {
                messages.Add($"Lot number {dto.LotNumber} does not exist for this item");
                isValid = false;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            serialExists = await this.transactionRepository.SerialExistsAsync(dto.ItemId, dto.SerialNumber, cancellationToken);
            if (dto.TransactionType == "RECEIPT" && serialExists)
            {
                messages.Add($"Serial number {dto.SerialNumber} already exists for this item");
                isValid = false;
            }
            else if (dto.TransactionType == "ISSUE" && !serialExists)
            {
                messages.Add($"Serial number {dto.SerialNumber} does not exist for this item");
                isValid = false;
            }
        }

        return ApiResponse<LotSerialValidationDto>.Success(new LotSerialValidationDto
        {
            IsValid = isValid,
            ValidationMessages = messages,
            LotRequired = item.LotTracked,
            SerialRequired = item.SerialTracked,
            LotExists = lotExists,
            SerialExists = serialExists,
        });
    }

    private async Task<ApiResponse<bool>> ValidateTransactionAsync(Guid itemId, Guid warehouseId, string? lotNumber, string? serialNumber, CancellationToken cancellationToken)
    {
        var item = await this.itemRepository.GetByIdAsync(itemId, cancellationToken);
        if (item == null)
        {
            return ApiResponse<bool>.Failure($"Item {itemId} not found");
        }

        var warehouse = await this.warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse == null)
        {
            return ApiResponse<bool>.Failure($"Warehouse {warehouseId} not found");
        }

        if (item.LotTracked && string.IsNullOrWhiteSpace(lotNumber))
        {
            return ApiResponse<bool>.Failure("Lot number is required for this item");
        }

        if (item.SerialTracked && string.IsNullOrWhiteSpace(serialNumber))
        {
            return ApiResponse<bool>.Failure("Serial number is required for this item");
        }

        return ApiResponse<bool>.Success(true);
    }

    private InventoryTransactionDto MapToDto(InventoryTransaction transaction)
    {
        return new InventoryTransactionDto
        {
            Id = transaction.Id,
            ItemId = transaction.ItemId,
            WarehouseId = transaction.WarehouseId,
            LocationId = transaction.LocationId,
            TransactionType = transaction.TransactionType,
            Quantity = transaction.Quantity,
            UnitCost = transaction.UnitCost,
            ExtendedCost = transaction.ExtendedCost,
            LotNumber = transaction.LotNumber,
            SerialNumber = transaction.SerialNumber,
            ExpirationDate = transaction.ExpirationDate,
            ReferenceType = transaction.ReferenceType,
            ReferenceId = transaction.ReferenceId,
            Notes = transaction.Notes,
            TransactionDate = transaction.TransactionDate,
            CreatedAt = transaction.CreatedAt,
            CreatedBy = transaction.CreatedBy,
        };
    }
}