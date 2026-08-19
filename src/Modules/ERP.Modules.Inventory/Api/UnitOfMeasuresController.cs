// <copyright file="UnitOfMeasuresController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventory/uoms")]
public class UnitOfMeasuresController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public UnitOfMeasuresController(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UnitOfMeasureDto>>>> GetAll(
        [FromQuery] Guid companyId, CancellationToken ct)
    {
        var list = await _context.UnitOfMeasures
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.Code)
            .Select(u => new UnitOfMeasureDto(u.Id, u.Code, u.Description, u.BaseUOM, u.FactorToBase, u.IsActive))
            .ToListAsync(ct);

        return Ok(ApiResponse<List<UnitOfMeasureDto>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UnitOfMeasureDto>>> GetById(
        [FromQuery] Guid companyId, Guid id, CancellationToken ct)
    {
        var entity = await _context.UnitOfMeasures
            .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId, ct);

        if (entity is null)
            return NotFound(ApiResponse<UnitOfMeasureDto>.Failure([$"UOM {id} not found."]));

        var dto = new UnitOfMeasureDto(entity.Id, entity.Code, entity.Description, entity.BaseUOM, entity.FactorToBase, entity.IsActive);
        return Ok(ApiResponse<UnitOfMeasureDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UnitOfMeasureDto>>> Create(
        [FromBody] CreateUnitOfMeasureRequest request, CancellationToken ct)
    {
        if (request.CompanyId == Guid.Empty)
            return BadRequest(ApiResponse<UnitOfMeasureDto>.Failure(["Company is required."]));

        var entity = new UnitOfMeasure(request.CompanyId, request.Code, request.Description, request.BaseUOM, request.FactorToBase);
        _context.UnitOfMeasures.Add(entity);
        await _context.SaveChangesAsync(ct);

        var dto = new UnitOfMeasureDto(entity.Id, entity.Code, entity.Description, entity.BaseUOM, entity.FactorToBase, entity.IsActive);
        return Ok(ApiResponse<UnitOfMeasureDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        [FromQuery] Guid companyId, Guid id, [FromBody] UpdateUnitOfMeasureRequest request, CancellationToken ct)
    {
        var entity = await _context.UnitOfMeasures
            .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId, ct);

        if (entity is null)
            return NotFound(ApiResponse<string>.Failure([$"UOM {id} not found."]));

        entity.Update(request.Description, request.BaseUOM, request.FactorToBase, request.IsActive);
        await _context.SaveChangesAsync(ct);

        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        [FromQuery] Guid companyId, Guid id, CancellationToken ct)
    {
        var entity = await _context.UnitOfMeasures
            .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId, ct);

        if (entity is null)
            return NotFound(ApiResponse<string>.Failure([$"UOM {id} not found."]));

        _context.UnitOfMeasures.Remove(entity);
        await _context.SaveChangesAsync(ct);

        return Ok(ApiResponse<string>.Success("Deleted"));
    }
}

public record UnitOfMeasureDto(
    Guid Id,
    string Code,
    string Description,
    string BaseUOM,
    decimal FactorToBase,
    bool IsActive);

public record CreateUnitOfMeasureRequest(
    Guid CompanyId,
    string Code,
    string Description,
    string BaseUOM,
    decimal FactorToBase);

public record UpdateUnitOfMeasureRequest(
    string Description,
    string BaseUOM,
    decimal FactorToBase,
    bool IsActive);
