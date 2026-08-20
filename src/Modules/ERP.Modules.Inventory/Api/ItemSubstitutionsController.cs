// <copyright file="ItemSubstitutionsController.cs" company="ERP Project">
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

[ApiController]
[Route("api/v1/inventory/item-substitutions")]
public class ItemSubstitutionsController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemSubstitutionsController(InventoryDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemSubstitutionDto>>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] Guid? itemId, [FromQuery] SubstitutionStatus? status, CancellationToken cancellationToken)
    {
        var query = _context.ItemSubstitutions.AsQueryable();
        query = query.ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId);
        if (itemId.HasValue) query = query.Where(s => s.ItemId == itemId.Value);
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        var subs = await query.OrderBy(s => s.ItemId).Take(1000).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ItemSubstitutionDto>>.Success(subs.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemSubstitutionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sub = await _context.ItemSubstitutions.FindAsync(new object[] { id }, cancellationToken);
        if (sub is null) return NotFound(ApiResponse<ItemSubstitutionDto>.Failure(["Item substitution not found."]));
        return Ok(ApiResponse<ItemSubstitutionDto>.Success(MapToDto(sub)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemSubstitutionDto>>> Create([FromBody] CreateItemSubstitutionRequest request, CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { request.ItemId }, cancellationToken);
        if (item is null) return BadRequest(ApiResponse<ItemSubstitutionDto>.Failure([$"Item {request.ItemId} not found"]));
        var subItem = await _context.Items.FindAsync(new object[] { request.SubstituteItemId }, cancellationToken);
        if (subItem is null) return BadRequest(ApiResponse<ItemSubstitutionDto>.Failure([$"Substitute item {request.SubstituteItemId} not found"]));

        var sub = new ItemSubstitution(request.CompanyId, request.ItemId, request.SubstituteItemId, request.Direction, request.Reason, request.RequiresApproval);
        _context.ItemSubstitutions.Add(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sub.Id }, ApiResponse<ItemSubstitutionDto>.Success(MapToDto(sub)));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemSubstitutionDto>>> Approve(Guid id, [FromBody] ApproveItemSubstitutionRequest request, CancellationToken cancellationToken)
    {
        var sub = await _context.ItemSubstitutions.FindAsync(new object[] { id }, cancellationToken);
        if (sub is null) return NotFound(ApiResponse<ItemSubstitutionDto>.Failure(["Item substitution not found."]));
        sub.Approve(request.ApprovedBy);
        _context.ItemSubstitutions.Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<ItemSubstitutionDto>.Success(MapToDto(sub)));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemSubstitutionDto>>> Reject(Guid id, [FromBody] RejectItemSubstitutionRequest request, CancellationToken cancellationToken)
    {
        var sub = await _context.ItemSubstitutions.FindAsync(new object[] { id }, cancellationToken);
        if (sub is null) return NotFound(ApiResponse<ItemSubstitutionDto>.Failure(["Item substitution not found."]));
        sub.Reject(request.RejectedBy, request.RejectionReason);
        _context.ItemSubstitutions.Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<ItemSubstitutionDto>.Success(MapToDto(sub)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var sub = await _context.ItemSubstitutions.FindAsync(new object[] { id }, cancellationToken);
        if (sub is null) return NotFound(ApiResponse<string>.Failure(["Item substitution not found."]));
        _context.ItemSubstitutions.Remove(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("deleted"));
    }

    private static ItemSubstitutionDto MapToDto(ItemSubstitution s) => new ItemSubstitutionDto
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        ItemId = s.ItemId,
        SubstituteItemId = s.SubstituteItemId,
        Direction = s.Direction.ToString(),
        Reason = s.Reason,
        RequiresApproval = s.RequiresApproval,
        Status = s.Status.ToString(),
        ApprovedBy = s.ApprovedBy,
        ApprovedOn = s.ApprovedOn,
        RejectionReason = s.RejectionReason,
    };
}

public class ItemSubstitutionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid SubstituteItemId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool RequiresApproval { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? RejectionReason { get; set; }
}

public class CreateItemSubstitutionRequest
{
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid SubstituteItemId { get; set; }
    public SubstitutionDirection Direction { get; set; }
    public string? Reason { get; set; }
    public bool RequiresApproval { get; set; }
}

public class ApproveItemSubstitutionRequest
{
    public string ApprovedBy { get; set; } = string.Empty;
}

public class RejectItemSubstitutionRequest
{
    public string RejectedBy { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
}
