// <copyright file="WarehouseController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/warehouses")]
public class WarehouseController : ControllerBase
{
    private readonly IRepository<Warehouse> _repository;
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public WarehouseController(
        IRepository<Warehouse> repository,
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WarehouseDto>>>> GetAll(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var query = _context.Warehouses.AsQueryable();
        query = query.Where(w => w.DeletedOn == null);
        query = query.ApplyCompanyScope(HttpContext, w => w.CompanyId, companyId);

        var warehouses = await query.OrderBy(w => w.WarehouseCode).ToListAsync(cancellationToken);
        var dtos = warehouses.Select(w => new WarehouseDto
        {
            Id = w.Id,
            WarehouseCode = w.WarehouseCode,
            WarehouseName = w.WarehouseName,
            CompanyId = w.CompanyId,
            WarehouseType = w.WarehouseType.ToString(),
            Address = w.Address,
            IsActive = w.IsActive,
        }).ToList();

        return Ok(ApiResponse<List<WarehouseDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var w = await _context.Warehouses.FindAsync(new object[] { id }, cancellationToken);
        if (w is null)
            return NotFound(ApiResponse<WarehouseDto>.Failure([$"Warehouse {id} not found."]));

        return Ok(ApiResponse<WarehouseDto>.Success(new WarehouseDto
        {
            Id = w.Id,
            WarehouseCode = w.WarehouseCode,
            WarehouseName = w.WarehouseName,
            CompanyId = w.CompanyId,
            WarehouseType = w.WarehouseType.ToString(),
            Address = w.Address,
            IsActive = w.IsActive,
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Create(
        [FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WarehouseType>(request.WarehouseType, true, out var type))
            return BadRequest(ApiResponse<WarehouseDto>.Failure([$"Invalid warehouse type '{request.WarehouseType}'."]));

        var warehouse = new Warehouse(
            request.WarehouseCode, request.WarehouseName, request.CompanyId, type, request.Address, true);

        await _repository.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseDto
        {
            Id = warehouse.Id,
            WarehouseCode = warehouse.WarehouseCode,
            WarehouseName = warehouse.WarehouseName,
            CompanyId = warehouse.CompanyId,
            WarehouseType = warehouse.WarehouseType.ToString(),
            Address = warehouse.Address,
            IsActive = warehouse.IsActive,
        };

        return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, ApiResponse<WarehouseDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Update(
        Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = await _repository.GetByIdAsync(id, cancellationToken);
        if (warehouse is null)
            return NotFound(ApiResponse<WarehouseDto>.Failure([$"Warehouse {id} not found."]));

        if (request.WarehouseName is not null)
        {
            warehouse.UpdateAddress(request.Address);
        }

        if (request.Address is not null)
        {
            warehouse.UpdateAddress(request.Address);
        }

        if (!string.IsNullOrEmpty(request.WarehouseType) &&
            Enum.TryParse<WarehouseType>(request.WarehouseType, true, out var type))
        {
            // WarehouseType has no setter — create new if type changes
            // For simplicity we just update address/name here
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                warehouse.Activate();
            }
            else
            {
                warehouse.Deactivate();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseDto
        {
            Id = warehouse.Id,
            WarehouseCode = warehouse.WarehouseCode,
            WarehouseName = warehouse.WarehouseName,
            CompanyId = warehouse.CompanyId,
            WarehouseType = warehouse.WarehouseType.ToString(),
            Address = warehouse.Address,
            IsActive = warehouse.IsActive,
        };

        return Ok(ApiResponse<WarehouseDto>.Success(dto));
    }

    [HttpPut("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> ToggleStatus(
        Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _repository.GetByIdAsync(id, cancellationToken);
        if (warehouse is null)
            return NotFound(ApiResponse<WarehouseDto>.Failure([$"Warehouse {id} not found."]));

        if (warehouse.IsActive)
        {
            warehouse.Deactivate();
        }
        else
        {
            warehouse.Activate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseDto
        {
            Id = warehouse.Id,
            WarehouseCode = warehouse.WarehouseCode,
            WarehouseName = warehouse.WarehouseName,
            CompanyId = warehouse.CompanyId,
            WarehouseType = warehouse.WarehouseType.ToString(),
            Address = warehouse.Address,
            IsActive = warehouse.IsActive,
        };

        return Ok(ApiResponse<WarehouseDto>.Success(dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _repository.GetByIdAsync(id, cancellationToken);
        if (warehouse is null)
            return NotFound(ApiResponse<string>.Failure([$"Warehouse {id} not found."]));

        warehouse.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<string>.Success("Warehouse deactivated."));
    }
}

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string WarehouseType { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWarehouseRequest
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string WarehouseType { get; set; } = "Distribution";
    public string? Address { get; set; }
}

public class UpdateWarehouseRequest
{
    public string? WarehouseName { get; set; }
    public string? Address { get; set; }
    public string? WarehouseType { get; set; }
    public bool? IsActive { get; set; }
}
