// <copyright file="LandedCostController.cs" company="ERP Project">
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
[Route("api/v1/inventory/landed-costs")]
public class LandedCostController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public LandedCostController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LandedCostDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? vendorId,
        [FromQuery] LandedCostStatus? status,
        [FromQuery] LandedCostType? costType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.LandedCosts.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(c => c.CompanyId == companyId.Value);
        }

        if (vendorId.HasValue)
        {
            query = query.Where(c => c.VendorId == vendorId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (costType.HasValue)
        {
            query = query.Where(c => c.CostType == costType.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(c => c.CostDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(c => c.CostDate <= endDate.Value);
        }

        var landedCosts = await query
            .OrderByDescending(c => c.CostDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = landedCosts.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<LandedCostDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LandedCostDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var landedCost = await _context.LandedCosts.FindAsync(new object[] { id }, cancellationToken);

        if (landedCost == null)
        {
            return NotFound(ApiResponse<LandedCostDto>.Failure(["Landed cost not found."]));
        }

        return Ok(ApiResponse<LandedCostDto>.Success(MapToDto(landedCost)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LandedCostDto>>> Create(
        [FromBody] CreateLandedCostRequest request,
        CancellationToken cancellationToken)
    {
        var landedCost = new LandedCost(
            request.CompanyId,
            request.VendorId,
            request.CostCode,
            request.Description,
            request.CostType,
            request.Amount,
            request.CostDate,
            request.ReferenceNumber);

        _context.LandedCosts.Add(landedCost);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(landedCost);
        return CreatedAtAction(nameof(GetById), new { id = landedCost.Id }, ApiResponse<LandedCostDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LandedCostDto>>> Update(
        Guid id,
        [FromBody] UpdateLandedCostRequest request,
        CancellationToken cancellationToken)
    {
        var landedCost = await _context.LandedCosts.FindAsync(new object[] { id }, cancellationToken);

        if (landedCost == null)
        {
            return NotFound(ApiResponse<LandedCostDto>.Failure(["Landed cost not found."]));
        }

        if (landedCost.Status != LandedCostStatus.PendingAllocation)
        {
            return BadRequest(ApiResponse<LandedCostDto>.Failure(["Only pending allocation landed costs can be updated."]));
        }

        if (request.Amount.HasValue)
        {
            landedCost.UpdateAmount(request.Amount.Value);
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            landedCost.UpdateDescription(request.Description);
        }

        if (request.ReferenceNumber != null)
        {
            // Note: ReferenceNumber doesn't have a setter, would need to add one
        }

        _context.LandedCosts.Update(landedCost);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<LandedCostDto>.Success(MapToDto(landedCost)));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<LandedCostDto>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var landedCost = await _context.LandedCosts.FindAsync(new object[] { id }, cancellationToken);

        if (landedCost == null)
        {
            return NotFound(ApiResponse<LandedCostDto>.Failure(["Landed cost not found."]));
        }

        if (landedCost.Status == LandedCostStatus.FullyAllocated)
        {
            return BadRequest(ApiResponse<LandedCostDto>.Failure(["Cannot cancel a fully allocated landed cost."]));
        }

        landedCost.UpdateStatus(LandedCostStatus.Cancelled);
        _context.LandedCosts.Update(landedCost);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<LandedCostDto>.Success(MapToDto(landedCost)));
    }

    private LandedCostDto MapToDto(LandedCost landedCost)
    {
        return new LandedCostDto
        {
            Id = landedCost.Id,
            CompanyId = landedCost.CompanyId,
            VendorId = landedCost.VendorId,
            CostCode = landedCost.CostCode,
            Description = landedCost.Description,
            CostType = landedCost.CostType.ToString(),
            Amount = landedCost.Amount,
            CostDate = landedCost.CostDate,
            ReferenceNumber = landedCost.ReferenceNumber,
            Status = landedCost.Status.ToString(),
            AllocatedAmount = landedCost.AllocatedAmount,
            RemainingAmount = landedCost.RemainingAmount,
            CreatedAt = landedCost.CreatedOn,
            CreatedBy = landedCost.CreatedBy,
        };
    }
}

public class LandedCostDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public string CostCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CostType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CostDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateLandedCostRequest
{
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public string CostCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public LandedCostType CostType { get; set; }
    public decimal Amount { get; set; }
    public DateTime CostDate { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class UpdateLandedCostRequest
{
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
}