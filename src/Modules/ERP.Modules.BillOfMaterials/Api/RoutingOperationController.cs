// <copyright file="RoutingOperationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.BillOfMaterials.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/bom/routing-operations")]
public class RoutingOperationController : ControllerBase
{
    private readonly BomDbContext _context;
    private readonly IBomUnitOfWork _unitOfWork;

    public RoutingOperationController(BomDbContext context, IBomUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoutingOperationDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.RoutingOperations.AsQueryable();
        query = query.ApplyCompanyScope(HttpContext, o => o.CompanyId, companyId);

        var operations = await query.OrderBy(o => o.OperationCode).ToListAsync(cancellationToken);
        var dtos = operations.Select(MapToDto).ToList();

        return Ok(ApiResponse<List<RoutingOperationDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoutingOperationDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var operation = await _context.RoutingOperations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (operation is null)
        {
            return NotFound(ApiResponse<RoutingOperationDto>.Failure(new[] { "Routing operation not found." }, 404));
        }

        return Ok(ApiResponse<RoutingOperationDto>.Success(MapToDto(operation)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateRoutingOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = new RoutingOperation(
            request.CompanyId,
            request.OperationCode,
            request.Description,
            request.WorkCenterId,
            request.StandardSetupTimeMinutes,
            request.StandardRunTimeMinutesPerUnit);

        _context.RoutingOperations.Add(operation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(operation.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id,
        [FromBody] UpdateRoutingOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = await _context.RoutingOperations.FindAsync(new object[] { id }, cancellationToken);
        if (operation is null)
            return NotFound(ApiResponse.Failure(new[] { "Routing operation not found." }, 404));

        operation.Update(
            request.Description,
            request.WorkCenterId,
            request.StandardSetupTimeMinutes,
            request.StandardRunTimeMinutesPerUnit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse>> Activate(
        Guid id, CancellationToken cancellationToken)
    {
        var operation = await _context.RoutingOperations.FindAsync(new object[] { id }, cancellationToken);
        if (operation is null)
            return NotFound(ApiResponse.Failure(new[] { "Routing operation not found." }, 404));

        operation.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse>> Deactivate(
        Guid id, CancellationToken cancellationToken)
    {
        var operation = await _context.RoutingOperations.FindAsync(new object[] { id }, cancellationToken);
        if (operation is null)
            return NotFound(ApiResponse.Failure(new[] { "Routing operation not found." }, 404));

        operation.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        var operation = await _context.RoutingOperations.FindAsync(new object[] { id }, cancellationToken);
        if (operation is null)
            return NotFound(ApiResponse.Failure(new[] { "Routing operation not found." }, 404));

        _context.RoutingOperations.Remove(operation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    private static RoutingOperationDto MapToDto(RoutingOperation o) => new ()
    {
        Id = o.Id,
        CompanyId = o.CompanyId,
        OperationCode = o.OperationCode,
        Description = o.Description,
        WorkCenterId = o.WorkCenterId,
        StandardSetupTimeMinutes = o.StandardSetupTimeMinutes,
        StandardRunTimeMinutesPerUnit = o.StandardRunTimeMinutesPerUnit,
        IsActive = o.IsActive,
    };
}

public class RoutingOperationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? WorkCenterId { get; set; }
    public decimal StandardSetupTimeMinutes { get; set; }
    public decimal StandardRunTimeMinutesPerUnit { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRoutingOperationRequest
{
    public Guid CompanyId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? WorkCenterId { get; set; }
    public decimal StandardSetupTimeMinutes { get; set; }
    public decimal StandardRunTimeMinutesPerUnit { get; set; }
}

public class UpdateRoutingOperationRequest
{
    public string? Description { get; set; }
    public Guid? WorkCenterId { get; set; }
    public decimal? StandardSetupTimeMinutes { get; set; }
    public decimal? StandardRunTimeMinutesPerUnit { get; set; }
}
