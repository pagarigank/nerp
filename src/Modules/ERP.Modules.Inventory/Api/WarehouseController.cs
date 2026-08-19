// <copyright file="WarehouseController.cs" company="ERP Project">
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
        if (companyId.HasValue)
        {
            query = query.Where(w => w.CompanyId == companyId.Value);
        }

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
