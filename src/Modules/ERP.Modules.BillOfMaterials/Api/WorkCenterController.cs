// <copyright file="WorkCenterController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.BillOfMaterials.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/bom/work-centers")]
public class WorkCenterController : ControllerBase
{
    private readonly BomDbContext _context;
    private readonly IBomUnitOfWork _unitOfWork;

    public WorkCenterController(BomDbContext context, IBomUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WorkCenterDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.WorkCenters.AsQueryable();
        if (companyId.HasValue)
            query = query.Where(w => w.CompanyId == companyId.Value);

        var centers = await query.OrderBy(w => w.Code).ToListAsync(cancellationToken);
        var dtos = centers.Select(w => new WorkCenterDto
        {
            Id = w.Id,
            CompanyId = w.CompanyId,
            Code = w.Code,
            Name = w.Name,
            Department = w.Department,
            CapacityHoursPerDay = w.CapacityHoursPerDay,
            EfficiencyPercentage = w.EfficiencyPercentage,
            CostRatePerHour = w.CostRatePerHour,
            IsActive = w.IsActive,
        }).ToList();

        return Ok(ApiResponse<List<WorkCenterDto>>.Success(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateWorkCenterRequest request,
        CancellationToken cancellationToken)
    {
        var wc = new WorkCenter(
            request.CompanyId,
            request.Code,
            request.Name,
            request.Department,
            request.CapacityHoursPerDay,
            request.EfficiencyPercentage,
            request.CostRatePerHour);

        _context.WorkCenters.Add(wc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(wc.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id,
        [FromBody] UpdateWorkCenterRequest request,
        CancellationToken cancellationToken)
    {
        var wc = await _context.WorkCenters.FindAsync(new object[] { id }, cancellationToken);
        if (wc is null)
            return NotFound(ApiResponse.Failure(new[] { "Work center not found." }, 404));

        wc.Update(
            request.Name,
            request.Department,
            request.CapacityHoursPerDay,
            request.EfficiencyPercentage,
            request.CostRatePerHour,
            request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        var wc = await _context.WorkCenters.FindAsync(new object[] { id }, cancellationToken);
        if (wc is null)
            return NotFound(ApiResponse.Failure(new[] { "Work center not found." }, 404));

        _context.WorkCenters.Remove(wc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }
}

public class WorkCenterDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public decimal CapacityHoursPerDay { get; set; }
    public decimal EfficiencyPercentage { get; set; }
    public decimal CostRatePerHour { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkCenterRequest
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public decimal CapacityHoursPerDay { get; set; }
    public decimal EfficiencyPercentage { get; set; }
    public decimal CostRatePerHour { get; set; }
}

public class UpdateWorkCenterRequest
{
    public string? Name { get; set; }
    public string? Department { get; set; }
    public decimal? CapacityHoursPerDay { get; set; }
    public decimal? EfficiencyPercentage { get; set; }
    public decimal? CostRatePerHour { get; set; }
    public bool? IsActive { get; set; }
}
