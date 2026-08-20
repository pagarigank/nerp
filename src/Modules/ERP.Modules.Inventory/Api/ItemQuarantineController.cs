// <copyright file="ItemQuarantineController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/quarantine")]
public class ItemQuarantineController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemQuarantineController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemQuarantineDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] QuarantineStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] bool includeDispositions = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemQuarantines.AsQueryable();

        if (includeDispositions)
        {
            query = query.Include(q => q.Dispositions);
        }

        query = query.ApplyCompanyScope(HttpContext, q => q.CompanyId, companyId);

        if (itemId.HasValue)
        {
            query = query.Where(q => q.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(q => q.WarehouseId == warehouseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(q => q.QuarantineDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(q => q.QuarantineDate <= endDate.Value);
        }

        var quarantines = await query
            .OrderByDescending(q => q.QuarantineDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = quarantines.Select(q => MapToDto(q, includeDispositions)).ToList();
        return Ok(ApiResponse<List<ItemQuarantineDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemQuarantineDto>>> GetById(
        Guid id,
        [FromQuery] bool includeDispositions = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemQuarantines.AsQueryable();

        if (includeDispositions)
        {
            query = query.Include(q => q.Dispositions);
        }

        var quarantine = await query.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quarantine == null)
        {
            return NotFound(ApiResponse<ItemQuarantineDto>.Failure(["Quarantine record not found."]));
        }

        return Ok(ApiResponse<ItemQuarantineDto>.Success(MapToDto(quarantine, includeDispositions)));
    }

    [HttpPost]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemQuarantineDto>>> Create(
        [FromBody] CreateItemQuarantineRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { request.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Item {request.ItemId} not found"]));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Warehouse {request.WarehouseId} not found"]));
        }

        if (request.BinId.HasValue)
        {
            var bin = await _context.WarehouseBins.FindAsync(new object[] { request.BinId.Value }, cancellationToken);
            if (bin == null)
            {
                return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Bin {request.BinId.Value} not found"]));
            }
        }

        if (request.LotId.HasValue)
        {
            var lot = await _context.Lots.FindAsync(new object[] { request.LotId.Value }, cancellationToken);
            if (lot == null)
            {
                return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Lot {request.LotId.Value} not found"]));
            }
        }

        if (request.SerialNumberId.HasValue)
        {
            var serial = await _context.SerialNumbers.FindAsync(new object[] { request.SerialNumberId.Value }, cancellationToken);
            if (serial == null)
            {
                return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Serial number {request.SerialNumberId.Value} not found"]));
            }
        }

        var quarantine = new ItemQuarantine(
            request.CompanyId,
            request.ItemId,
            request.WarehouseId,
            request.BinId,
            request.LotId,
            request.SerialNumberId,
            request.Quantity,
            request.UnitOfMeasure,
            request.Reason,
            request.QuarantinedBy,
            request.ReferenceNumber,
            request.Notes);

        _context.ItemQuarantines.Add(quarantine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(quarantine, false);
        return CreatedAtAction(nameof(GetById), new { id = quarantine.Id }, ApiResponse<ItemQuarantineDto>.Success(dto));
    }

    [HttpPost("{id:guid}/release")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemQuarantineDto>>> Release(
        Guid id,
        [FromBody] QuarantineDispositionRequest request,
        CancellationToken cancellationToken)
    {
        var quarantine = await _context.ItemQuarantines
            .Include(q => q.Dispositions)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quarantine == null)
        {
            return NotFound(ApiResponse<ItemQuarantineDto>.Failure(["Quarantine record not found."]));
        }

        if (quarantine.Status != QuarantineStatus.Active)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot release quarantine with status {quarantine.Status}."]));
        }

        if (request.Quantity > quarantine.Quantity)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot release {request.Quantity}, only {quarantine.Quantity} available."]));
        }

        if (request.Quantity == quarantine.Quantity)
        {
            quarantine.Release(request.PerformedBy, request.Reason);
        }
        else
        {
            // Partial release - create a new quarantine for remaining quantity
            quarantine.UpdateQuantity(quarantine.Quantity - request.Quantity);
            quarantine.UpdateStatus(QuarantineStatus.PartiallyReleased);

            var newQuarantine = new ItemQuarantine(
                quarantine.CompanyId,
                quarantine.ItemId,
                quarantine.WarehouseId,
                quarantine.BinId,
                quarantine.LotId,
                quarantine.SerialNumberId,
                request.Quantity,
                quarantine.UnitOfMeasure,
                quarantine.Reason,
                request.PerformedBy,
                quarantine.ReferenceNumber,
                $"Partial release of quarantine {quarantine.Id}");

            newQuarantine.Release(request.PerformedBy, request.Reason);
            _context.ItemQuarantines.Add(newQuarantine);
        }

        var disposition = new QuarantineDisposition(
            quarantine.Id,
            QuarantineAction.Release,
            request.Quantity,
            request.DestinationWarehouseId,
            request.DestinationBinId,
            request.Notes,
            request.PerformedBy);

        quarantine.AddDisposition(disposition);
        _context.QuarantineDispositions.Add(disposition);
        _context.ItemQuarantines.Update(quarantine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemQuarantineDto>.Success(MapToDto(quarantine, true)));
    }

    [HttpPost("{id:guid}/mark-disposed")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemQuarantineDto>>> MarkAsDisposed(
        Guid id,
        [FromBody] QuarantineDispositionRequest request,
        CancellationToken cancellationToken)
    {
        var quarantine = await _context.ItemQuarantines
            .Include(q => q.Dispositions)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quarantine == null)
        {
            return NotFound(ApiResponse<ItemQuarantineDto>.Failure(["Quarantine record not found."]));
        }

        if (quarantine.Status != QuarantineStatus.Active && quarantine.Status != QuarantineStatus.PartiallyReleased)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot dispose quarantine with status {quarantine.Status}."]));
        }

        if (request.Quantity > quarantine.Quantity)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot dispose {request.Quantity}, only {quarantine.Quantity} available."]));
        }

        if (request.Quantity == quarantine.Quantity)
        {
            quarantine.MarkAsDisposed(request.PerformedBy, request.Reason);
        }
        else
        {
            // Partial disposal
            quarantine.UpdateQuantity(quarantine.Quantity - request.Quantity);
            quarantine.UpdateStatus(QuarantineStatus.PartiallyReleased);

            var newQuarantine = new ItemQuarantine(
                quarantine.CompanyId,
                quarantine.ItemId,
                quarantine.WarehouseId,
                quarantine.BinId,
                quarantine.LotId,
                quarantine.SerialNumberId,
                request.Quantity,
                quarantine.UnitOfMeasure,
                quarantine.Reason,
                request.PerformedBy,
                quarantine.ReferenceNumber,
                $"Partial disposal of quarantine {quarantine.Id}");

            newQuarantine.MarkAsDisposed(request.PerformedBy, request.Reason);
            _context.ItemQuarantines.Add(newQuarantine);
        }

        var disposition = new QuarantineDisposition(
            quarantine.Id,
            QuarantineAction.Dispose,
            request.Quantity,
            null,
            null,
            request.Notes,
            request.PerformedBy);

        quarantine.AddDisposition(disposition);
        _context.QuarantineDispositions.Add(disposition);
        _context.ItemQuarantines.Update(quarantine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemQuarantineDto>.Success(MapToDto(quarantine, true)));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemQuarantineDto>>> Reject(
        Guid id,
        [FromBody] QuarantineDispositionRequest request,
        CancellationToken cancellationToken)
    {
        var quarantine = await _context.ItemQuarantines
            .Include(q => q.Dispositions)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quarantine == null)
        {
            return NotFound(ApiResponse<ItemQuarantineDto>.Failure(["Quarantine record not found."]));
        }

        if (quarantine.Status != QuarantineStatus.Active && quarantine.Status != QuarantineStatus.PartiallyReleased)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot reject quarantine with status {quarantine.Status}."]));
        }

        if (request.Quantity > quarantine.Quantity)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot reject {request.Quantity}, only {quarantine.Quantity} available."]));
        }

        if (request.Quantity == quarantine.Quantity)
        {
            quarantine.Reject(request.PerformedBy, request.Reason);
        }
        else
        {
            // Partial rejection
            quarantine.UpdateQuantity(quarantine.Quantity - request.Quantity);
            quarantine.UpdateStatus(QuarantineStatus.PartiallyReleased);

            var newQuarantine = new ItemQuarantine(
                quarantine.CompanyId,
                quarantine.ItemId,
                quarantine.WarehouseId,
                quarantine.BinId,
                quarantine.LotId,
                quarantine.SerialNumberId,
                request.Quantity,
                quarantine.UnitOfMeasure,
                quarantine.Reason,
                request.PerformedBy,
                quarantine.ReferenceNumber,
                $"Partial rejection of quarantine {quarantine.Id}");

            newQuarantine.Reject(request.PerformedBy, request.Reason);
            _context.ItemQuarantines.Add(newQuarantine);
        }

        var disposition = new QuarantineDisposition(
            quarantine.Id,
            QuarantineAction.Reject,
            request.Quantity,
            request.DestinationWarehouseId,
            request.DestinationBinId,
            request.Notes,
            request.PerformedBy);

        quarantine.AddDisposition(disposition);
        _context.QuarantineDispositions.Add(disposition);
        _context.ItemQuarantines.Update(quarantine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemQuarantineDto>.Success(MapToDto(quarantine, true)));
    }

    [HttpPost("{id:guid}/transfer")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemQuarantineDto>>> Transfer(
        Guid id,
        [FromBody] QuarantineDispositionRequest request,
        CancellationToken cancellationToken)
    {
        var quarantine = await _context.ItemQuarantines
            .Include(q => q.Dispositions)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quarantine == null)
        {
            return NotFound(ApiResponse<ItemQuarantineDto>.Failure(["Quarantine record not found."]));
        }

        if (quarantine.Status != QuarantineStatus.Active && quarantine.Status != QuarantineStatus.PartiallyReleased)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot transfer quarantine with status {quarantine.Status}."]));
        }

        if (request.Quantity > quarantine.Quantity)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Cannot transfer {request.Quantity}, only {quarantine.Quantity} available."]));
        }

        if (!request.DestinationWarehouseId.HasValue)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure(["Destination warehouse is required for transfer."]));
        }

        var destWarehouse = await _context.Warehouses.FindAsync(new object[] { request.DestinationWarehouseId.Value }, cancellationToken);
        if (destWarehouse == null)
        {
            return BadRequest(ApiResponse<ItemQuarantineDto>.Failure([$"Destination warehouse {request.DestinationWarehouseId.Value} not found."]));
        }

        if (request.Quantity == quarantine.Quantity)
        {
            // Transfer entire quantity - update the warehouse
            quarantine.UpdateWarehouse(request.DestinationWarehouseId.Value, request.DestinationBinId);
        }
        else
        {
            // Partial transfer - create new quarantine at destination
            quarantine.UpdateQuantity(quarantine.Quantity - request.Quantity);
            quarantine.UpdateStatus(QuarantineStatus.PartiallyReleased);

            var newQuarantine = new ItemQuarantine(
                quarantine.CompanyId,
                quarantine.ItemId,
                request.DestinationWarehouseId.Value,
                request.DestinationBinId,
                quarantine.LotId,
                quarantine.SerialNumberId,
                request.Quantity,
                quarantine.UnitOfMeasure,
                quarantine.Reason,
                request.PerformedBy,
                quarantine.ReferenceNumber,
                $"Transfer from quarantine {quarantine.Id}");

            _context.ItemQuarantines.Add(newQuarantine);
        }

        var disposition = new QuarantineDisposition(
            quarantine.Id,
            QuarantineAction.Transfer,
            request.Quantity,
            request.DestinationWarehouseId,
            request.DestinationBinId,
            request.Notes,
            request.PerformedBy);

        quarantine.AddDisposition(disposition);
        _context.QuarantineDispositions.Add(disposition);
        _context.ItemQuarantines.Update(quarantine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemQuarantineDto>.Success(MapToDto(quarantine, true)));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<QuarantineDashboardDto>>> GetDashboard(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var activeCount = await _context.ItemQuarantines
            .Where(q => q.CompanyId == companyId && q.Status == QuarantineStatus.Active)
            .CountAsync(cancellationToken);

        var partiallyReleasedCount = await _context.ItemQuarantines
            .Where(q => q.CompanyId == companyId && q.Status == QuarantineStatus.PartiallyReleased)
            .CountAsync(cancellationToken);

        var releasedCount = await _context.ItemQuarantines
            .Where(q => q.CompanyId == companyId && q.Status == QuarantineStatus.Released)
            .CountAsync(cancellationToken);

        var disposedCount = await _context.ItemQuarantines
            .Where(q => q.CompanyId == companyId && q.Status == QuarantineStatus.Disposed)
            .CountAsync(cancellationToken);

        var rejectedCount = await _context.ItemQuarantines
            .Where(q => q.CompanyId == companyId && q.Status == QuarantineStatus.Rejected)
            .CountAsync(cancellationToken);

        var totalQty = await _context.ItemQuarantines
            .Where(q => q.CompanyId == companyId && (q.Status == QuarantineStatus.Active || q.Status == QuarantineStatus.PartiallyReleased))
            .SumAsync(q => q.Quantity, cancellationToken);

        return Ok(ApiResponse<QuarantineDashboardDto>.Success(new QuarantineDashboardDto
        {
            CompanyId = companyId,
            ActiveQuarantines = activeCount,
            PartiallyReleasedQuarantines = partiallyReleasedCount,
            ReleasedQuarantines = releasedCount,
            DisposedQuarantines = disposedCount,
            RejectedQuarantines = rejectedCount,
            TotalQuarantinedQuantity = totalQty,
            GeneratedAt = DateTime.UtcNow,
        }));
    }

    private static ItemQuarantineDto MapToDto(ItemQuarantine quarantine, bool includeDispositions = false)
    {
        var dto = new ItemQuarantineDto
        {
            Id = quarantine.Id,
            CompanyId = quarantine.CompanyId,
            ItemId = quarantine.ItemId,
            WarehouseId = quarantine.WarehouseId,
            BinId = quarantine.BinId,
            LotId = quarantine.LotId,
            SerialNumberId = quarantine.SerialNumberId,
            Quantity = quarantine.Quantity,
            UnitOfMeasure = quarantine.UnitOfMeasure,
            Reason = quarantine.Reason,
            ReferenceNumber = quarantine.ReferenceNumber,
            Notes = quarantine.Notes,
            Status = quarantine.Status.ToString(),
            QuarantineDate = quarantine.QuarantineDate,
            QuarantinedBy = quarantine.QuarantinedBy,
            ReleasedDate = quarantine.ReleasedDate,
            ReleasedBy = quarantine.ReleasedBy,
            ReleaseReason = quarantine.ReleaseReason,
            CreatedAt = quarantine.CreatedOn,
            CreatedBy = quarantine.CreatedBy,
        };

        if (includeDispositions)
        {
            dto.Dispositions.AddRange(quarantine.Dispositions.Select(d => new QuarantineDispositionDto
            {
                Id = d.Id,
                QuarantineId = d.QuarantineId,
                Action = d.Action.ToString(),
                Quantity = d.Quantity,
                DestinationWarehouseId = d.DestinationWarehouseId,
                DestinationBinId = d.DestinationBinId,
                Notes = d.Notes,
                PerformedBy = d.PerformedBy,
                DispositionDate = d.DispositionDate,
            }));
        }

        return dto;
    }
}

#pragma warning disable CA1002, CA2227
public class ItemQuarantineDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialNumberId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime QuarantineDate { get; set; }
    public Guid QuarantinedBy { get; set; }
    public DateTime? ReleasedDate { get; set; }
    public Guid? ReleasedBy { get; set; }
    public string? ReleaseReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<QuarantineDispositionDto> Dispositions { get; } = new List<QuarantineDispositionDto>();
}
#pragma warning restore CA1002, CA2227

public class QuarantineDispositionDto
{
    public Guid Id { get; set; }
    public Guid QuarantineId { get; set; }
    public string Action { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public Guid? DestinationBinId { get; set; }
    public string? Notes { get; set; }
    public Guid? PerformedBy { get; set; }
    public DateTime DispositionDate { get; set; }
}

public class CreateItemQuarantineRequest
{
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialNumberId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid QuarantinedBy { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class QuarantineDispositionRequest
{
    public decimal Quantity { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public Guid? DestinationBinId { get; set; }
    public string? Notes { get; set; }
    public Guid PerformedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
}

#pragma warning disable CA1002, CA2227
public class QuarantineDashboardDto
{
    public Guid CompanyId { get; set; }
    public int ActiveQuarantines { get; set; }
    public int PartiallyReleasedQuarantines { get; set; }
    public int ReleasedQuarantines { get; set; }
    public int DisposedQuarantines { get; set; }
    public int RejectedQuarantines { get; set; }
    public decimal TotalQuarantinedQuantity { get; set; }
    public DateTime GeneratedAt { get; set; }
}
#pragma warning restore CA1002, CA2227