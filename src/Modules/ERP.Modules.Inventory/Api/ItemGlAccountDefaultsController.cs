// <copyright file="ItemGlAccountDefaultsController.cs" company="ERP Project">
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
[Route("api/v1/inventory/items/{itemId:guid}/gl-accounts")]
public class ItemGlAccountDefaultsController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public ItemGlAccountDefaultsController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ItemGlAccountDefaultsDto?>>> Get(
        Guid itemId, CancellationToken cancellationToken)
    {
        var defaults = await _context.ItemGLAccountDefaults
            .FirstOrDefaultAsync(x => x.ItemId == itemId, cancellationToken);

        if (defaults is null)
            return Ok(ApiResponse<ItemGlAccountDefaultsDto?>.Success(null));

        return Ok(ApiResponse<ItemGlAccountDefaultsDto?>.Success(new ItemGlAccountDefaultsDto(
            defaults.Id, defaults.ItemId, defaults.InventoryAssetAccountId, defaults.COGSAccountId,
            defaults.VarianceAccountId, defaults.PurchasePriceVarianceAccountId,
            defaults.SalesRevenueAccountId, defaults.InventoryAdjustmentAccountId,
            defaults.LandedCostClearingAccountId)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        Guid itemId, [FromBody] UpsertItemGlAccountDefaultsRequest request, CancellationToken cancellationToken)
    {
        var existing = await _context.ItemGLAccountDefaults
            .FirstOrDefaultAsync(x => x.ItemId == itemId, cancellationToken);

        if (existing is not null)
            return BadRequest(ApiResponse<Guid>.Failure([$"GL account defaults already exist for item {itemId}. Use PUT to update."]));

        var defaults = new ItemGLAccountDefaults(
            itemId, request.InventoryAssetAccountId, request.COGSAccountId, request.VarianceAccountId,
            request.PurchasePriceVarianceAccountId, request.SalesRevenueAccountId,
            request.InventoryAdjustmentAccountId, request.LandedCostClearingAccountId);

        _context.ItemGLAccountDefaults.Add(defaults);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { itemId }, ApiResponse<Guid>.Success(defaults.Id));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        Guid itemId, [FromBody] UpsertItemGlAccountDefaultsRequest request, CancellationToken cancellationToken)
    {
        var defaults = await _context.ItemGLAccountDefaults
            .FirstOrDefaultAsync(x => x.ItemId == itemId, cancellationToken);

        if (defaults is null)
            return NotFound(ApiResponse<string>.Failure([$"GL account defaults not found for item {itemId}."]));

        defaults.UpdateAccounts(
            request.InventoryAssetAccountId, request.COGSAccountId, request.VarianceAccountId,
            request.PurchasePriceVarianceAccountId, request.SalesRevenueAccountId,
            request.InventoryAdjustmentAccountId, request.LandedCostClearingAccountId);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }
}

public record ItemGlAccountDefaultsDto(
    Guid Id, Guid ItemId, Guid? InventoryAssetAccountId, Guid? COGSAccountId,
    Guid? VarianceAccountId, Guid? PurchasePriceVarianceAccountId, Guid? SalesRevenueAccountId,
    Guid? InventoryAdjustmentAccountId, Guid? LandedCostClearingAccountId);

public record UpsertItemGlAccountDefaultsRequest(
    Guid? InventoryAssetAccountId,
    Guid? COGSAccountId,
    Guid? VarianceAccountId,
    Guid? PurchasePriceVarianceAccountId,
    Guid? SalesRevenueAccountId,
    Guid? InventoryAdjustmentAccountId,
    Guid? LandedCostClearingAccountId);
