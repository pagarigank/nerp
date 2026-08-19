// <copyright file="InventoryTransactionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

[ApiController]
[Route("api/v1/inventory/transactions")]
public class InventoryTransactionController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public InventoryTransactionController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InventoryTransactionDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] TransactionType? transactionType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryTransactions.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        if (itemId.HasValue)
        {
            query = query.Where(t => t.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        }

        if (transactionType.HasValue)
        {
            query = query.Where(t => t.TransactionType == transactionType.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = transactions.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<InventoryTransactionDto>>.Success(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _context.InventoryTransactions.FindAsync(new object[] { id }, cancellationToken);
        if (transaction == null)
        {
            return NotFound(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Transaction {id} not found" }));
        }

        return Ok(ApiResponse<InventoryTransactionDto>.Success(MapToDto(transaction)));
    }

    [HttpPost("receipt")]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> CreateReceipt(
        [FromBody] CreateReceiptDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { dto.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Item {dto.ItemId} not found" }));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Warehouse {dto.WarehouseId} not found" }));
        }

        var transaction = new InventoryTransaction(
            dto.CompanyId,
            dto.ItemId,
            dto.WarehouseId,
            TransactionType.Receipt,
            dto.Quantity,
            dto.UnitOfMeasure ?? item.BaseUnitOfMeasure,
            dto.UnitCost,
            dto.TransactionDate ?? DateTime.UtcNow,
            dto.BinId,
            dto.LotId,
            dto.SerialNumber,
            dto.ReferenceNumber,
            dto.ProjectId,
            dto.Notes);

        _context.InventoryTransactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventDispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
            transaction.Id, dto.CompanyId, dto.ItemId, dto.WarehouseId,
            TransactionType.Receipt.ToString(), transaction.Quantity, transaction.UnitCost,
            transaction.ExtendedCost, transaction.TransactionDate, dto.ProjectId), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, ApiResponse<InventoryTransactionDto>.Success(MapToDto(transaction)));
    }

    [HttpPost("issue")]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> CreateIssue(
        [FromBody] CreateIssueDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { dto.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Item {dto.ItemId} not found" }));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Warehouse {dto.WarehouseId} not found" }));
        }

        // Check available quantity
        var availableQty = await GetAvailableQuantityAsync(dto.ItemId, dto.WarehouseId, dto.BinId, dto.LotId, dto.SerialNumber, cancellationToken);
        if (availableQty < dto.Quantity)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Insufficient quantity. Available: {availableQty}, Requested: {dto.Quantity}" }));
        }

        // Get average cost
        var avgCostNullable = await GetAverageCostAsync(dto.ItemId, dto.WarehouseId, cancellationToken);
        var avgCost = avgCostNullable ?? item.StandardCost ?? 0m;

        var transaction = new InventoryTransaction(
            dto.CompanyId,
            dto.ItemId,
            dto.WarehouseId,
            TransactionType.Issue,
            -dto.Quantity, // Negative for issues
            dto.UnitOfMeasure ?? item.BaseUnitOfMeasure,
            avgCost,
            dto.TransactionDate ?? DateTime.UtcNow,
            dto.BinId,
            dto.LotId,
            dto.SerialNumber,
            dto.ReferenceNumber,
            dto.ProjectId,
            dto.Notes);

        _context.InventoryTransactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventDispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
            transaction.Id, dto.CompanyId, dto.ItemId, dto.WarehouseId,
            TransactionType.Issue.ToString(), transaction.Quantity, transaction.UnitCost,
            transaction.ExtendedCost, transaction.TransactionDate, dto.ProjectId), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, ApiResponse<InventoryTransactionDto>.Success(MapToDto(transaction)));
    }

    [HttpPost("adjustment")]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> CreateAdjustment(
        [FromBody] CreateAdjustmentDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { dto.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Item {dto.ItemId} not found" }));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Warehouse {dto.WarehouseId} not found" }));
        }

        var avgCostNullable = await GetAverageCostAsync(dto.ItemId, dto.WarehouseId, cancellationToken);
        var avgCost = avgCostNullable ?? item.StandardCost ?? 0m;

        var transaction = new InventoryTransaction(
            dto.CompanyId,
            dto.ItemId,
            dto.WarehouseId,
            TransactionType.Adjustment,
            dto.QuantityAdjustment,
            dto.UnitOfMeasure ?? item.BaseUnitOfMeasure,
            avgCost,
            dto.TransactionDate ?? DateTime.UtcNow,
            dto.BinId,
            dto.LotId,
            dto.SerialNumber,
            dto.ReferenceNumber,
            dto.ProjectId,
            $"Reason: {dto.ReasonCode}. {dto.Notes}");

        _context.InventoryTransactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventDispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
            transaction.Id, dto.CompanyId, dto.ItemId, dto.WarehouseId,
            TransactionType.Adjustment.ToString(), transaction.Quantity, transaction.UnitCost,
            transaction.ExtendedCost, transaction.TransactionDate, dto.ProjectId), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, ApiResponse<InventoryTransactionDto>.Success(MapToDto(transaction)));
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> CreateTransfer(
        [FromBody] CreateTransferDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { dto.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Item {dto.ItemId} not found" }));
        }

        var fromWarehouse = await _context.Warehouses.FindAsync(new object[] { dto.FromWarehouseId }, cancellationToken);
        if (fromWarehouse == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"From warehouse {dto.FromWarehouseId} not found" }));
        }

        var toWarehouse = await _context.Warehouses.FindAsync(new object[] { dto.ToWarehouseId }, cancellationToken);
        if (toWarehouse == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"To warehouse {dto.ToWarehouseId} not found" }));
        }

        // Check available quantity
        var availableQty = await GetAvailableQuantityAsync(dto.ItemId, dto.FromWarehouseId, dto.FromBinId, dto.LotId, dto.SerialNumber, cancellationToken);
        if (availableQty < dto.Quantity)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Insufficient quantity. Available: {availableQty}, Requested: {dto.Quantity}" }));
        }

        var avgCostNullable = await GetAverageCostAsync(dto.ItemId, dto.FromWarehouseId, cancellationToken);
        var avgCost = avgCostNullable ?? item.StandardCost ?? 0m;

        // Create issue transaction
        var issueTransaction = new InventoryTransaction(
            dto.CompanyId,
            dto.ItemId,
            dto.FromWarehouseId,
            TransactionType.Transfer,
            -dto.Quantity,
            dto.UnitOfMeasure ?? item.BaseUnitOfMeasure,
            avgCost,
            dto.TransactionDate ?? DateTime.UtcNow,
            dto.FromBinId,
            dto.LotId,
            dto.SerialNumber,
            dto.ReferenceNumber,
            dto.ProjectId,
            $"Transfer to {toWarehouse.WarehouseCode}. {dto.Notes}");

        // Create receipt transaction
        var receiptTransaction = new InventoryTransaction(
            dto.CompanyId,
            dto.ItemId,
            dto.ToWarehouseId,
            TransactionType.Transfer,
            dto.Quantity,
            dto.UnitOfMeasure ?? item.BaseUnitOfMeasure,
            avgCost,
            dto.TransactionDate ?? DateTime.UtcNow,
            dto.ToBinId,
            dto.LotId,
            dto.SerialNumber,
            dto.ReferenceNumber,
            dto.ProjectId,
            $"Transfer from {fromWarehouse.WarehouseCode}. {dto.Notes}");

        _context.InventoryTransactions.Add(issueTransaction);
        _context.InventoryTransactions.Add(receiptTransaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Internal transfers do not change total inventory value; the handler
        // skips them, but we still emit the events for audit/traceability.
        await _domainEventDispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
            issueTransaction.Id, dto.CompanyId, dto.ItemId, dto.FromWarehouseId,
            TransactionType.Transfer.ToString(), issueTransaction.Quantity, issueTransaction.UnitCost,
            issueTransaction.ExtendedCost, issueTransaction.TransactionDate, dto.ProjectId), cancellationToken);
        await _domainEventDispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
            receiptTransaction.Id, dto.CompanyId, dto.ItemId, dto.ToWarehouseId,
            TransactionType.Transfer.ToString(), receiptTransaction.Quantity, receiptTransaction.UnitCost,
            receiptTransaction.ExtendedCost, receiptTransaction.TransactionDate, dto.ProjectId), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = receiptTransaction.Id }, ApiResponse<InventoryTransactionDto>.Success(MapToDto(receiptTransaction)));
    }

    [HttpPost("scrap")]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> CreateScrap(
        [FromBody] CreateScrapDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { dto.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Item {dto.ItemId} not found" }));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Warehouse {dto.WarehouseId} not found" }));
        }

        // Validate available quantity for the scrap (cannot scrap more than on hand).
        var availableQty = await GetAvailableQuantityAsync(dto.ItemId, dto.WarehouseId, dto.BinId, dto.LotId, dto.SerialNumber, cancellationToken);
        if (availableQty < dto.Quantity)
        {
            return BadRequest(ApiResponse<InventoryTransactionDto>.Failure(new[] { $"Insufficient quantity to scrap. Available: {availableQty}, Requested: {dto.Quantity}" }));
        }

        var avgCostNullable = await GetAverageCostAsync(dto.ItemId, dto.WarehouseId, cancellationToken);
        var avgCost = avgCostNullable ?? item.StandardCost ?? 0m;

        var transaction = new InventoryTransaction(
            dto.CompanyId,
            dto.ItemId,
            dto.WarehouseId,
            TransactionType.Scrap,
            -dto.Quantity,
            dto.UnitOfMeasure ?? item.BaseUnitOfMeasure,
            avgCost,
            dto.TransactionDate ?? DateTime.UtcNow,
            dto.BinId,
            dto.LotId,
            dto.SerialNumber,
            dto.ReferenceNumber,
            dto.ProjectId,
            $"Scrap: {dto.ScrapReasonCode}. {dto.Notes}");

        _context.InventoryTransactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventDispatcher.DispatchAsync(new InventoryTransactionPostedEvent(
            transaction.Id, dto.CompanyId, dto.ItemId, dto.WarehouseId,
            TransactionType.Scrap.ToString(), transaction.Quantity, transaction.UnitCost,
            transaction.ExtendedCost, transaction.TransactionDate, dto.ProjectId), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, ApiResponse<InventoryTransactionDto>.Success(MapToDto(transaction)));
    }

    [HttpGet("available-quantity")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetAvailableQuantity(
        [FromQuery] Guid itemId,
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid? binId,
        [FromQuery] Guid? lotId,
        [FromQuery] string? serialNumber,
        CancellationToken cancellationToken)
    {
        var quantity = await GetAvailableQuantityAsync(itemId, warehouseId, binId, lotId, serialNumber, cancellationToken);
        return Ok(ApiResponse<decimal>.Success(quantity));
    }

    private async Task<decimal> GetAvailableQuantityAsync(
        Guid itemId,
        Guid warehouseId,
        Guid? binId,
        Guid? lotId,
        string? serialNumber,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId);

        if (binId.HasValue)
        {
            query = query.Where(t => t.BinId == binId.Value);
        }

        if (lotId.HasValue)
        {
            query = query.Where(t => t.LotId == lotId.Value);
        }

        if (!string.IsNullOrEmpty(serialNumber))
        {
            query = query.Where(t => t.SerialNumber == serialNumber);
        }

        return await query.SumAsync(t => t.Quantity, cancellationToken);
    }

    private async Task<decimal?> GetAverageCostAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var receipts = await _context.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId && t.Quantity > 0)
            .Select(t => new { t.Quantity, t.ExtendedCost })
            .ToListAsync(cancellationToken);

        if (receipts.Count == 0)
        {
            return null;
        }

        var totalQty = receipts.Sum(r => r.Quantity);
        var totalCost = receipts.Sum(r => r.ExtendedCost);

        return totalQty > 0 ? totalCost / totalQty : null;
    }

    private InventoryTransactionDto MapToDto(InventoryTransaction transaction)
    {
        return new InventoryTransactionDto
        {
            Id = transaction.Id,
            CompanyId = transaction.CompanyId,
            ItemId = transaction.ItemId,
            WarehouseId = transaction.WarehouseId,
            TransactionType = transaction.TransactionType.ToString(),
            Quantity = transaction.Quantity,
            UnitOfMeasure = transaction.UnitOfMeasure,
            UnitCost = transaction.UnitCost,
            ExtendedCost = transaction.ExtendedCost,
            TransactionDate = transaction.TransactionDate,
            BinId = transaction.BinId,
            LotId = transaction.LotId,
            SerialNumber = transaction.SerialNumber,
            ReferenceNumber = transaction.ReferenceNumber,
            ProjectId = transaction.ProjectId,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedOn,
            CreatedBy = transaction.CreatedBy,
        };
    }
}

public record InventoryTransactionDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string UnitOfMeasure { get; init; } = string.Empty;
    public decimal UnitCost { get; init; }
    public decimal ExtendedCost { get; init; }
    public DateTime TransactionDate { get; init; }
    public Guid? BinId { get; init; }
    public Guid? LotId { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record CreateReceiptDto
{
    public Guid CompanyId { get; init; }
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? UnitOfMeasure { get; init; }
    public DateTime? TransactionDate { get; init; }
    public Guid? BinId { get; init; }
    public Guid? LotId { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Notes { get; init; }
}

public record CreateIssueDto
{
    public Guid CompanyId { get; init; }
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public string? UnitOfMeasure { get; init; }
    public DateTime? TransactionDate { get; init; }
    public Guid? BinId { get; init; }
    public Guid? LotId { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Notes { get; init; }
}

public record CreateAdjustmentDto
{
    public Guid CompanyId { get; init; }
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal QuantityAdjustment { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string? UnitOfMeasure { get; init; }
    public DateTime? TransactionDate { get; init; }
    public Guid? BinId { get; init; }
    public Guid? LotId { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Notes { get; init; }
}

public record CreateTransferDto
{
    public Guid CompanyId { get; init; }
    public Guid ItemId { get; init; }
    public Guid FromWarehouseId { get; init; }
    public Guid ToWarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public string? UnitOfMeasure { get; init; }
    public DateTime? TransactionDate { get; init; }
    public Guid? FromBinId { get; init; }
    public Guid? ToBinId { get; init; }
    public Guid? LotId { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Notes { get; init; }
}

public record CreateScrapDto
{
    public Guid CompanyId { get; init; }
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public string ScrapReasonCode { get; init; } = string.Empty;
    public string? UnitOfMeasure { get; init; }
    public DateTime? TransactionDate { get; init; }
    public Guid? BinId { get; init; }
    public Guid? LotId { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Notes { get; init; }
}
