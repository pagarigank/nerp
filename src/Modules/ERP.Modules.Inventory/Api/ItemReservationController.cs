// <copyright file="ItemReservationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/reservations")]
public class ItemReservationController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemReservationController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemReservationDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] ItemReservationStatus? status,
        [FromQuery] ReservationSourceType? sourceType,
        [FromQuery] Guid? sourceId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.ItemReservations.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        if (itemId.HasValue)
        {
            query = query.Where(r => r.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(r => r.WarehouseId == warehouseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (sourceType.HasValue)
        {
            query = query.Where(r => r.SourceType == sourceType.Value);
        }

        if (sourceId.HasValue)
        {
            query = query.Where(r => r.SourceId == sourceId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedOn >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedOn <= endDate.Value);
        }

        var reservations = await query
            .OrderByDescending(r => r.CreatedOn)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = reservations.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ItemReservationDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemReservationDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await _context.ItemReservations.FindAsync(new object[] { id }, cancellationToken);

        if (reservation == null)
        {
            return NotFound(ApiResponse<ItemReservationDto>.Failure(["Item reservation not found."]));
        }

        return Ok(ApiResponse<ItemReservationDto>.Success(MapToDto(reservation)));
    }

    [HttpPost]
    [Authorize(Roles = "InventoryManager,Admin,SalesManager,ProductionManager")]
    public async Task<ActionResult<ApiResponse<ItemReservationDto>>> Create(
        [FromBody] CreateItemReservationRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { request.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure([$"Item {request.ItemId} not found"]));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure([$"Warehouse {request.WarehouseId} not found"]));
        }

        if (request.BinId.HasValue)
        {
            var bin = await _context.WarehouseBins.FindAsync(new object[] { request.BinId.Value }, cancellationToken);
            if (bin == null)
            {
                return BadRequest(ApiResponse<ItemReservationDto>.Failure([$"Bin {request.BinId.Value} not found"]));
            }
        }

        // Check available quantity
        var stock = await _context.ItemStocks
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId
                                 && s.ItemId == request.ItemId
                                 && s.WarehouseId == request.WarehouseId
                                 && (s.BinId == request.BinId || request.BinId == null), cancellationToken);

        decimal availableQty = stock?.AvailableQuantity ?? 0;

        if (!item.AllowNegativeInventory && availableQty < request.Quantity)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure(
                [$"Insufficient available quantity. Available: {availableQty}, Requested: {request.Quantity}"]));
        }

        var reservation = new ItemReservation(
            request.CompanyId,
            request.ItemId,
            request.WarehouseId,
            request.Quantity,
            request.UnitOfMeasure,
            request.SourceType,
            request.SourceId,
            request.BinId,
            request.LotNumber,
            request.SerialNumber,
            request.ExpirationDate,
            request.Notes);

        _context.ItemReservations.Add(reservation);

        // Update allocated quantity on stock
        if (stock != null)
        {
            stock.AdjustAllocated(request.Quantity);
            _context.ItemStocks.Update(stock);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(reservation);
        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, ApiResponse<ItemReservationDto>.Success(dto));
    }

    [HttpPost("{id:guid}/release")]
    [Authorize(Roles = "InventoryManager,Admin,SalesManager,ProductionManager")]
    public async Task<ActionResult<ApiResponse<ItemReservationDto>>> Release(
        Guid id,
        [FromBody] ReleaseItemReservationRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = await _context.ItemReservations.FindAsync(new object[] { id }, cancellationToken);

        if (reservation == null)
        {
            return NotFound(ApiResponse<ItemReservationDto>.Failure(["Item reservation not found."]));
        }

        if (reservation.Status == ItemReservationStatus.FullyReleased)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure(["Reservation is already fully released."]));
        }

        if (reservation.Status == ItemReservationStatus.Cancelled)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure(["Cannot release a cancelled reservation."]));
        }

        if (request.Quantity > reservation.RemainingQuantity)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure(
                [$"Cannot release {request.Quantity}, only {reservation.RemainingQuantity} remaining."]));
        }

        reservation.Release(request.Quantity);
        _context.ItemReservations.Update(reservation);

        // Update stock allocated quantity
        var stock = await _context.ItemStocks
            .FirstOrDefaultAsync(s => s.CompanyId == reservation.CompanyId
                                 && s.ItemId == reservation.ItemId
                                 && s.WarehouseId == reservation.WarehouseId
                                 && (s.BinId == reservation.BinId || reservation.BinId == null), cancellationToken);

        if (stock != null)
        {
            stock.AdjustAllocated(-request.Quantity);
            _context.ItemStocks.Update(stock);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemReservationDto>.Success(MapToDto(reservation)));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "InventoryManager,Admin,SalesManager,ProductionManager")]
    public async Task<ActionResult<ApiResponse<ItemReservationDto>>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reservation = await _context.ItemReservations.FindAsync(new object[] { id }, cancellationToken);

        if (reservation == null)
        {
            return NotFound(ApiResponse<ItemReservationDto>.Failure(["Item reservation not found."]));
        }

        if (reservation.Status == ItemReservationStatus.FullyReleased)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure(["Cannot cancel a fully released reservation."]));
        }

        if (reservation.Status == ItemReservationStatus.Cancelled)
        {
            return BadRequest(ApiResponse<ItemReservationDto>.Failure(["Reservation is already cancelled."]));
        }

        reservation.Cancel(Guid.Empty); // In real implementation, pass current user ID
        _context.ItemReservations.Update(reservation);

        // Update stock allocated quantity
        var stock = await _context.ItemStocks
            .FirstOrDefaultAsync(s => s.CompanyId == reservation.CompanyId
                                 && s.ItemId == reservation.ItemId
                                 && s.WarehouseId == reservation.WarehouseId
                                 && (s.BinId == reservation.BinId || reservation.BinId == null), cancellationToken);

        if (stock != null)
        {
            stock.AdjustAllocated(-reservation.RemainingQuantity);
            _context.ItemStocks.Update(stock);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemReservationDto>.Success(MapToDto(reservation)));
    }

    [HttpGet("available/{itemId:guid}/{warehouseId:guid}")]
    public async Task<ActionResult<ApiResponse<AvailableQuantityDto>>> GetAvailableQuantity(
        Guid itemId,
        Guid warehouseId,
        [FromQuery] Guid? binId,
        [FromQuery] string? lotNumber,
        [FromQuery] string? serialNumber,
        CancellationToken cancellationToken)
    {
        var query = _context.ItemStocks
            .Where(s => s.ItemId == itemId && s.WarehouseId == warehouseId);

        if (binId.HasValue)
        {
            query = query.Where(s => s.BinId == binId.Value);
        }

        var stocks = await query.ToListAsync(cancellationToken);
        decimal totalOnHand = stocks.Sum(s => s.OnHandQuantity);
        decimal totalAllocated = stocks.Sum(s => s.AllocatedQuantity);
        decimal available = totalOnHand - totalAllocated;

        // If lot/serial specified, we'd filter further
        return Ok(ApiResponse<AvailableQuantityDto>.Success(new AvailableQuantityDto
        {
            ItemId = itemId,
            WarehouseId = warehouseId,
            BinId = binId,
            LotNumber = lotNumber,
            SerialNumber = serialNumber,
            TotalOnHand = totalOnHand,
            TotalAllocated = totalAllocated,
            Available = available,
        }));
    }

    private ItemReservationDto MapToDto(ItemReservation reservation)
    {
        return new ItemReservationDto
        {
            Id = reservation.Id,
            CompanyId = reservation.CompanyId,
            ItemId = reservation.ItemId,
            WarehouseId = reservation.WarehouseId,
            BinId = reservation.BinId,
            Quantity = reservation.Quantity,
            UnitOfMeasure = reservation.UnitOfMeasure,
            SourceType = reservation.SourceType.ToString(),
            SourceId = reservation.SourceId,
            LotNumber = reservation.LotNumber,
            SerialNumber = reservation.SerialNumber,
            ExpirationDate = reservation.ExpirationDate,
            Notes = reservation.Notes,
            Status = reservation.Status.ToString(),
            ReleasedQuantity = reservation.ReleasedQuantity,
            RemainingQuantity = reservation.RemainingQuantity,
            CreatedAt = reservation.CreatedOn,
            CreatedBy = reservation.CreatedBy,
        };
    }
}

public class ItemReservationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ReleasedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateItemReservationRequest
{
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public ReservationSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Notes { get; set; }
}

public class ReleaseItemReservationRequest
{
    public decimal Quantity { get; set; }
}

public class AvailableQuantityDto
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal TotalOnHand { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal Available { get; set; }
}