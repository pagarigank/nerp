// <copyright file="NegativeInventoryOverrideController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/negative-inventory-overrides")]
public class NegativeInventoryOverrideController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NegativeInventoryOverrideController> _logger;

    public NegativeInventoryOverrideController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<NegativeInventoryOverrideController> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NegativeInventoryOverrideDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] NegativeInventoryOverrideStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.NegativeInventoryOverrides.AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, o => o.CompanyId, companyId);

        if (itemId.HasValue)
        {
            query = query.Where(o => o.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(o => o.WarehouseId == warehouseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedOn >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.CreatedOn <= endDate.Value);
        }

        var overrides = await query
            .OrderByDescending(o => o.CreatedOn)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = overrides.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<NegativeInventoryOverrideDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NegativeInventoryOverrideDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var override_ = await _context.NegativeInventoryOverrides.FindAsync(new object[] { id }, cancellationToken);

        if (override_ == null)
        {
            return NotFound(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Negative inventory override not found."]));
        }

        return Ok(ApiResponse<NegativeInventoryOverrideDto>.Success(MapToDto(override_)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NegativeInventoryOverrideDto>>> Create(
        [FromBody] CreateNegativeInventoryOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { request.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<NegativeInventoryOverrideDto>.Failure([$"Item {request.ItemId} not found"]));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<NegativeInventoryOverrideDto>.Failure([$"Warehouse {request.WarehouseId} not found"]));
        }

        if (request.BinId.HasValue)
        {
            var bin = await _context.WarehouseBins.FindAsync(new object[] { request.BinId.Value }, cancellationToken);
            if (bin == null)
            {
                return BadRequest(ApiResponse<NegativeInventoryOverrideDto>.Failure([$"Bin {request.BinId.Value} not found"]));
            }
        }

        // Check if item allows negative inventory
        if (!item.AllowNegativeInventory)
        {
            // Override required - will go through approval workflow
        }

        var override_ = new NegativeInventoryOverride(
            request.CompanyId,
            request.ItemId,
            request.WarehouseId,
            request.BinId,
            request.RequestedQuantity,
            request.UnitOfMeasure,
            request.Reason,
            request.RequestedBy,
            request.ReferenceNumber);

        _context.NegativeInventoryOverrides.Add(override_);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Phase 7 gap: Notify warehouse manager of negative inventory override request
        _logger.LogWarning("NEGATIVE_INVENTORY_OVERRIDE_REQUESTED ItemId={ItemId} WarehouseId={WarehouseId} RequestedBy={RequestedBy} Quantity={Quantity} Reason={Reason}",
            request.ItemId, request.WarehouseId, request.RequestedBy, request.RequestedQuantity, request.Reason);

        var dto = MapToDto(override_);
        return CreatedAtAction(nameof(GetById), new { id = override_.Id }, ApiResponse<NegativeInventoryOverrideDto>.Success(dto));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<NegativeInventoryOverrideDto>>> Approve(
        Guid id,
        [FromBody] ApproveNegativeInventoryOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var override_ = await _context.NegativeInventoryOverrides.FindAsync(new object[] { id }, cancellationToken);

        if (override_ == null)
        {
            return NotFound(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Negative inventory override not found."]));
        }

        if (override_.Status != NegativeInventoryOverrideStatus.Pending)
        {
            return BadRequest(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Only pending overrides can be approved."]));
        }

        override_.Approve(request.ApprovedBy, request.ApprovalNotes);
        _context.NegativeInventoryOverrides.Update(override_);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<NegativeInventoryOverrideDto>.Success(MapToDto(override_)));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<NegativeInventoryOverrideDto>>> Reject(
        Guid id,
        [FromBody] RejectNegativeInventoryOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var override_ = await _context.NegativeInventoryOverrides.FindAsync(new object[] { id }, cancellationToken);

        if (override_ == null)
        {
            return NotFound(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Negative inventory override not found."]));
        }

        if (override_.Status != NegativeInventoryOverrideStatus.Pending)
        {
            return BadRequest(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Only pending overrides can be rejected."]));
        }

        override_.Reject(request.RejectedBy, request.RejectionReason);
        _context.NegativeInventoryOverrides.Update(override_);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<NegativeInventoryOverrideDto>.Success(MapToDto(override_)));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<NegativeInventoryOverrideDto>>> Cancel(
        Guid id,
        [FromBody] CancelNegativeInventoryOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var override_ = await _context.NegativeInventoryOverrides.FindAsync(new object[] { id }, cancellationToken);

        if (override_ == null)
        {
            return NotFound(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Negative inventory override not found."]));
        }

        if (override_.Status != NegativeInventoryOverrideStatus.Pending)
        {
            return BadRequest(ApiResponse<NegativeInventoryOverrideDto>.Failure(["Only pending overrides can be cancelled."]));
        }

        override_.Cancel(request.CancelledBy);
        _context.NegativeInventoryOverrides.Update(override_);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<NegativeInventoryOverrideDto>.Success(MapToDto(override_)));
    }

    private NegativeInventoryOverrideDto MapToDto(NegativeInventoryOverride override_)
    {
        return new NegativeInventoryOverrideDto
        {
            Id = override_.Id,
            CompanyId = override_.CompanyId,
            ItemId = override_.ItemId,
            WarehouseId = override_.WarehouseId,
            BinId = override_.BinId,
            RequestedQuantity = override_.RequestedQuantity,
            UnitOfMeasure = override_.UnitOfMeasure,
            Reason = override_.Reason,
            RequestedBy = override_.RequestedBy,
            ReferenceNumber = override_.ReferenceNumber,
            Status = override_.Status.ToString(),
            ApprovedBy = override_.ApprovedBy,
            ApprovedDate = override_.ApprovedDate,
            ApprovalNotes = override_.ApprovalNotes,
            RejectedBy = override_.RejectedBy,
            RejectedDate = override_.RejectedDate,
            RejectionReason = override_.RejectionReason,
            CreatedAt = override_.CreatedOn,
            CreatedBy = override_.CreatedBy,
        };
    }
}

public class NegativeInventoryOverrideDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid RequestedBy { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovalNotes { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedDate { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateNegativeInventoryOverrideRequest
{
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid RequestedBy { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class ApproveNegativeInventoryOverrideRequest
{
    public Guid ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
}

public class RejectNegativeInventoryOverrideRequest
{
    public Guid RejectedBy { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
}

public class CancelNegativeInventoryOverrideRequest
{
    public Guid CancelledBy { get; set; }
}