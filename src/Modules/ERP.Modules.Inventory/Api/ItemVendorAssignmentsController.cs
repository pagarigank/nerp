// <copyright file="ItemVendorAssignmentsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/inventory/items/{itemId:guid}/vendors")]
public class ItemVendorAssignmentsController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public ItemVendorAssignmentsController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemVendorAssignmentDto>>>> GetAll(
        Guid itemId, CancellationToken cancellationToken)
    {
        var list = await _context.ItemVendorAssignments
            .Where(v => v.ItemId == itemId)
            .OrderBy(v => v.IsPrimaryVendor ? 0 : 1).ThenBy(v => v.VendorId)
            .Select(v => new ItemVendorAssignmentDto(
                v.Id, v.ItemId, v.VendorId, v.IsPrimaryVendor, v.VendorItemCode,
                v.VendorDescription, v.VendorCost, v.LeadTimeDays, v.MinimumOrderQuantity, v.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ItemVendorAssignmentDto>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemVendorAssignmentDto>>> GetById(
        Guid itemId, Guid id, CancellationToken cancellationToken)
    {
        var v = await _context.ItemVendorAssignments
            .FirstOrDefaultAsync(x => x.Id == id && x.ItemId == itemId, cancellationToken);

        if (v is null)
            return NotFound(ApiResponse<ItemVendorAssignmentDto>.Failure([$"Vendor assignment {id} not found."]));

        return Ok(ApiResponse<ItemVendorAssignmentDto>.Success(new ItemVendorAssignmentDto(
            v.Id, v.ItemId, v.VendorId, v.IsPrimaryVendor, v.VendorItemCode,
            v.VendorDescription, v.VendorCost, v.LeadTimeDays, v.MinimumOrderQuantity, v.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        Guid itemId, [FromBody] CreateItemVendorAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = new ItemVendorAssignment(
            itemId, request.VendorId, request.IsPrimaryVendor, request.VendorItemCode,
            request.VendorDescription, request.VendorCost, request.LeadTimeDays, request.MinimumOrderQuantity);

        _context.ItemVendorAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { itemId, id = assignment.Id },
            ApiResponse<Guid>.Success(assignment.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        Guid itemId, Guid id, [FromBody] UpdateItemVendorAssignmentRequest request, CancellationToken cancellationToken)
    {
        var v = await _context.ItemVendorAssignments
            .FirstOrDefaultAsync(x => x.Id == id && x.ItemId == itemId, cancellationToken);

        if (v is null)
            return NotFound(ApiResponse<string>.Failure([$"Vendor assignment {id} not found."]));

        v.UpdateVendorDetails(request.VendorItemCode, request.VendorDescription,
            request.VendorCost, request.LeadTimeDays, request.MinimumOrderQuantity);
        v.SetPrimaryVendor(request.IsPrimaryVendor);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        Guid itemId, Guid id, CancellationToken cancellationToken)
    {
        var v = await _context.ItemVendorAssignments
            .FirstOrDefaultAsync(x => x.Id == id && x.ItemId == itemId, cancellationToken);

        if (v is null)
            return NotFound(ApiResponse<string>.Failure([$"Vendor assignment {id} not found."]));

        v.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deactivated"));
    }
}

public record ItemVendorAssignmentDto(
    Guid Id, Guid ItemId, Guid VendorId, bool IsPrimaryVendor, string? VendorItemCode,
    string? VendorDescription, decimal? VendorCost, int? LeadTimeDays, decimal? MinimumOrderQuantity, bool IsActive);

public record CreateItemVendorAssignmentRequest(
    Guid VendorId, bool IsPrimaryVendor, string? VendorItemCode, string? VendorDescription,
    decimal? VendorCost, int? LeadTimeDays, decimal? MinimumOrderQuantity);

public record UpdateItemVendorAssignmentRequest(
    bool IsPrimaryVendor, string? VendorItemCode, string? VendorDescription,
    decimal? VendorCost, int? LeadTimeDays, decimal? MinimumOrderQuantity);
