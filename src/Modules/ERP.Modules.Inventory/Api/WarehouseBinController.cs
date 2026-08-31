// <copyright file="WarehouseBinController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/warehouse-bins")]
public class WarehouseBinController : ControllerBase
{
    private readonly IRepository<WarehouseBin> _repository;
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public WarehouseBinController(
        IRepository<WarehouseBin> repository,
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WarehouseBinDto>>>> GetAll(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var query = _context.WarehouseBins.AsQueryable();
        query = query.Where(b => b.DeletedOn == null);

        if (warehouseId.HasValue)
        {
            query = query.Where(b => b.WarehouseId == warehouseId.Value);
        }

        var bins = await query.OrderBy(b => b.BinCode).ToListAsync(cancellationToken);

        var dtos = bins.Select(b => new WarehouseBinDto
        {
            Id = b.Id,
            WarehouseId = b.WarehouseId,
            BinCode = b.BinCode,
            Aisle = b.Aisle,
            Rack = b.Rack,
            Shelf = b.Shelf,
            IsActive = b.IsActive,
        }).ToList();

        return Ok(ApiResponse<List<WarehouseBinDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseBinDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var bin = await _repository.GetByIdAsync(id, cancellationToken);

        if (bin == null)
        {
            return NotFound(ApiResponse<WarehouseBinDto>.Failure(["Bin not found."]));
        }

        var dto = new WarehouseBinDto
        {
            Id = bin.Id,
            WarehouseId = bin.WarehouseId,
            BinCode = bin.BinCode,
            Aisle = bin.Aisle,
            Rack = bin.Rack,
            Shelf = bin.Shelf,
            IsActive = bin.IsActive,
        };

        return Ok(ApiResponse<WarehouseBinDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseBinDto>>> Create(
        [FromBody] CreateWarehouseBinRequest request,
        CancellationToken cancellationToken)
    {
        var bin = new WarehouseBin(
            request.WarehouseId,
            request.BinCode,
            BinType.Picking,
            request.Aisle,
            request.Rack,
            request.Shelf);

        await _repository.AddAsync(bin, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseBinDto
        {
            Id = bin.Id,
            WarehouseId = bin.WarehouseId,
            BinCode = bin.BinCode,
            Aisle = bin.Aisle,
            Rack = bin.Rack,
            Shelf = bin.Shelf,
            IsActive = bin.IsActive,
        };

        return CreatedAtAction(nameof(GetById), new { id = bin.Id }, ApiResponse<WarehouseBinDto>.Success(dto));
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<WarehouseBinDto>>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var bin = await _repository.GetByIdAsync(id, cancellationToken);

        if (bin == null)
        {
            return NotFound(ApiResponse<WarehouseBinDto>.Failure(["Bin not found."]));
        }

        bin.Activate();
        _repository.Update(bin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseBinDto
        {
            Id = bin.Id,
            WarehouseId = bin.WarehouseId,
            BinCode = bin.BinCode,
            Aisle = bin.Aisle,
            Rack = bin.Rack,
            Shelf = bin.Shelf,
            IsActive = bin.IsActive,
        };

        return Ok(ApiResponse<WarehouseBinDto>.Success(dto));
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<WarehouseBinDto>>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var bin = await _repository.GetByIdAsync(id, cancellationToken);

        if (bin == null)
        {
            return NotFound(ApiResponse<WarehouseBinDto>.Failure(["Bin not found."]));
        }

        bin.Deactivate();
        _repository.Update(bin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseBinDto
        {
            Id = bin.Id,
            WarehouseId = bin.WarehouseId,
            BinCode = bin.BinCode,
            Aisle = bin.Aisle,
            Rack = bin.Rack,
            Shelf = bin.Shelf,
            IsActive = bin.IsActive,
        };

        return Ok(ApiResponse<WarehouseBinDto>.Success(dto));
    }

    [HttpPut("{id:guid}/location")]
    public async Task<ActionResult<ApiResponse<WarehouseBinDto>>> UpdateLocation(
        Guid id,
        [FromBody] UpdateWarehouseBinLocationRequest request,
        CancellationToken cancellationToken)
    {
        var bin = await _repository.GetByIdAsync(id, cancellationToken);

        if (bin == null)
        {
            return NotFound(ApiResponse<WarehouseBinDto>.Failure(["Bin not found."]));
        }

        bin.UpdateLocation(request.Aisle, request.Rack, request.Shelf);
        _repository.Update(bin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseBinDto
        {
            Id = bin.Id,
            WarehouseId = bin.WarehouseId,
            BinCode = bin.BinCode,
            Aisle = bin.Aisle,
            Rack = bin.Rack,
            Shelf = bin.Shelf,
            IsActive = bin.IsActive,
        };

        return Ok(ApiResponse<WarehouseBinDto>.Success(dto));
    }
}

public class UpdateWarehouseBinLocationRequest
{
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
}

public class WarehouseBinDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string BinCode { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWarehouseBinRequest
{
    public Guid WarehouseId { get; set; }
    public string BinCode { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
}
