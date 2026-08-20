// <copyright file="ItemMovementController.cs" company="ERP Project">
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
[Route("api/v1/inventory/movements")]
public class ItemMovementController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public ItemMovementController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ItemMovementDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] MovementType? movementType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? referenceNumber,
        [FromQuery] Guid? lotId,
        [FromQuery] Guid? serialNumberId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemMovements.AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, m => m.CompanyId, companyId);

        if (itemId.HasValue)
        {
            query = query.Where(m => m.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(m => m.WarehouseId == warehouseId.Value);
        }

        if (movementType.HasValue)
        {
            query = query.Where(m => m.MovementType == movementType.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(m => m.MovementDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.MovementDate <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(referenceNumber))
        {
            query = query.Where(m => m.ReferenceNumber != null && m.ReferenceNumber.Contains(referenceNumber));
        }

        if (lotId.HasValue)
        {
            query = query.Where(m => m.LotId == lotId.Value);
        }

        if (serialNumberId.HasValue)
        {
            query = query.Where(m => m.SerialNumberId == serialNumberId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = movements.Select(MapToDto).ToList();
        var response = new PaginatedResponse<ItemMovementDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        };
        return Ok(ApiResponse<PaginatedResponse<ItemMovementDto>>.Success(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemMovementDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var movement = await _context.ItemMovements.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (movement == null)
        {
            return NotFound(ApiResponse<ItemMovementDto>.Failure(["Movement record not found."]));
        }

        return Ok(ApiResponse<ItemMovementDto>.Success(MapToDto(movement)));
    }

    [HttpGet("item/{itemId:guid}/history")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ItemMovementDto>>>> GetItemHistory(
        Guid itemId,
        [FromQuery] Guid companyId,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemMovements
            .Where(m => m.CompanyId == companyId && m.ItemId == itemId);

        if (warehouseId.HasValue)
        {
            query = query.Where(m => m.WarehouseId == warehouseId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(m => m.MovementDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.MovementDate <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = movements.Select(MapToDto).ToList();
        var response = new PaginatedResponse<ItemMovementDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        };
        return Ok(ApiResponse<PaginatedResponse<ItemMovementDto>>.Success(response));
    }

    [HttpGet("warehouse/{warehouseId:guid}/history")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ItemMovementDto>>>> GetWarehouseHistory(
        Guid warehouseId,
        [FromQuery] Guid companyId,
        [FromQuery] Guid? itemId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemMovements
            .Where(m => m.CompanyId == companyId && m.WarehouseId == warehouseId);

        if (itemId.HasValue)
        {
            query = query.Where(m => m.ItemId == itemId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(m => m.MovementDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.MovementDate <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = movements.Select(MapToDto).ToList();
        var response = new PaginatedResponse<ItemMovementDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        };
        return Ok(ApiResponse<PaginatedResponse<ItemMovementDto>>.Success(response));
    }

    [HttpGet("lot/{lotId:guid}/history")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ItemMovementDto>>>> GetLotHistory(
        Guid lotId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemMovements.Where(m => m.LotId == lotId);

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = movements.Select(MapToDto).ToList();
        var response = new PaginatedResponse<ItemMovementDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        };
        return Ok(ApiResponse<PaginatedResponse<ItemMovementDto>>.Success(response));
    }

    [HttpGet("serial/{serialNumberId:guid}/history")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ItemMovementDto>>>> GetSerialHistory(
        Guid serialNumberId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemMovements.Where(m => m.SerialNumberId == serialNumberId);

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = movements.Select(MapToDto).ToList();
        var response = new PaginatedResponse<ItemMovementDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        };
        return Ok(ApiResponse<PaginatedResponse<ItemMovementDto>>.Success(response));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<MovementDashboardDto>>> GetDashboard(
        [FromQuery] Guid companyId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemMovements.Where(m => m.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(m => m.MovementDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.MovementDate <= endDate.Value);
        }

        var totalMovements = await query.CountAsync(cancellationToken);

        var receipts = await query.Where(m => m.MovementType == MovementType.Receipt).CountAsync(cancellationToken);
        var issues = await query.Where(m => m.MovementType == MovementType.Issue).CountAsync(cancellationToken);
        var transfers = await query.Where(m => m.MovementType == MovementType.TransferIn || m.MovementType == MovementType.TransferOut).CountAsync(cancellationToken);
        var adjustments = await query.Where(m => m.MovementType == MovementType.AdjustmentIn || m.MovementType == MovementType.AdjustmentOut).CountAsync(cancellationToken);

        var totalQty = await query.SumAsync(m => Math.Abs(m.Quantity), cancellationToken);

        var byType = await query
            .GroupBy(m => m.MovementType)
            .Select(g => new MovementTypeCountDto
            {
                MovementType = g.Key.ToString(),
                Count = g.Count(),
                TotalQuantity = g.Sum(m => Math.Abs(m.Quantity)),
            })
            .ToListAsync(cancellationToken);

        var dashboard = new MovementDashboardDto
        {
            CompanyId = companyId,
            TotalMovements = totalMovements,
            TotalQuantity = totalQty,
            ReceiptCount = receipts,
            IssueCount = issues,
            TransferCount = transfers,
            AdjustmentCount = adjustments,
            GeneratedAt = DateTime.UtcNow,
        };
        dashboard.ByType.AddRange(byType);

        return Ok(ApiResponse<MovementDashboardDto>.Success(dashboard));
    }

    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<List<ItemMovementDto>>>> GetRecent(
        [FromQuery] Guid companyId,
        [FromQuery] int hours = 24,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);

        var movements = await _context.ItemMovements
            .Where(m => m.CompanyId == companyId && m.MovementDate >= cutoff)
            .OrderByDescending(m => m.MovementDate)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var dtos = movements.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ItemMovementDto>>.Success(dtos));
    }

    private static ItemMovementDto MapToDto(ItemMovement movement)
    {
        return new ItemMovementDto
        {
            Id = movement.Id,
            CompanyId = movement.CompanyId,
            ItemId = movement.ItemId,
            WarehouseId = movement.WarehouseId,
            BinId = movement.BinId,
            LotId = movement.LotId,
            SerialNumberId = movement.SerialNumberId,
            MovementType = movement.MovementType.ToString(),
            Quantity = movement.Quantity,
            UnitOfMeasure = movement.UnitOfMeasure,
            UnitCost = movement.UnitCost,
            ExtendedCost = movement.ExtendedCost,
            ReferenceNumber = movement.ReferenceNumber,
            ReferenceType = movement.ReferenceType,
            ReferenceId = movement.ReferenceId,
            Notes = movement.Notes,
            MovementDate = movement.MovementDate,
            CreatedAt = movement.CreatedOn,
            CreatedBy = movement.CreatedBy,
        };
    }
}

#pragma warning disable CA1002, CA2227
public class ItemMovementDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialNumberId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public decimal? ExtendedCost { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
#pragma warning restore CA1002, CA2227

public class MovementTypeCountDto
{
    public string MovementType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalQuantity { get; set; }
}

#pragma warning disable CA1002, CA2227
public class MovementDashboardDto
{
    public Guid CompanyId { get; set; }
    public int TotalMovements { get; set; }
    public decimal TotalQuantity { get; set; }
    public int ReceiptCount { get; set; }
    public int IssueCount { get; set; }
    public int TransferCount { get; set; }
    public int AdjustmentCount { get; set; }
    public List<MovementTypeCountDto> ByType { get; } = new List<MovementTypeCountDto>();
    public DateTime GeneratedAt { get; set; }
}
#pragma warning restore CA1002, CA2227

#pragma warning disable CA1002, CA2227
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
#pragma warning restore CA1002, CA2227