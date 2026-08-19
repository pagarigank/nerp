// <copyright file="ItemUomConversionController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/inventory/items/{itemId:guid}/uom-conversions")]
public class ItemUomConversionController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public ItemUomConversionController(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UomConversionDto>>>> GetAll(
        Guid itemId, CancellationToken ct)
    {
        var list = await _context.ItemUnitOfMeasureConversions
            .Where(c => c.ItemId == itemId)
            .OrderBy(c => c.FromUOM).ThenBy(c => c.ToUOM)
            .Select(c => new UomConversionDto(c.Id, c.ItemId, c.FromUOM, c.ToUOM, c.ConversionFactor))
            .ToListAsync(ct);

        return Ok(ApiResponse<List<UomConversionDto>>.Success(list));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UomConversionDto>>> Create(
        Guid itemId, [FromBody] CreateUomConversionRequest request, CancellationToken ct)
    {
        var entity = new ItemUnitOfMeasureConversion(itemId, request.FromUOM, request.ToUOM, request.ConversionFactor);
        _context.ItemUnitOfMeasureConversions.Add(entity);
        await _context.SaveChangesAsync(ct);

        var dto = new UomConversionDto(entity.Id, entity.ItemId, entity.FromUOM, entity.ToUOM, entity.ConversionFactor);
        return Ok(ApiResponse<UomConversionDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        Guid itemId, Guid id, [FromBody] UpdateUomConversionRequest request, CancellationToken ct)
    {
        var entity = await _context.ItemUnitOfMeasureConversions
            .FirstOrDefaultAsync(c => c.Id == id && c.ItemId == itemId, ct);

        if (entity is null)
            return NotFound(ApiResponse<string>.Failure([$"UOM conversion {id} not found for item {itemId}."]));

        entity.UpdateConversionFactor(request.ConversionFactor);
        await _context.SaveChangesAsync(ct);

        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        Guid itemId, Guid id, CancellationToken ct)
    {
        var entity = await _context.ItemUnitOfMeasureConversions
            .FirstOrDefaultAsync(c => c.Id == id && c.ItemId == itemId, ct);

        if (entity is null)
            return NotFound(ApiResponse<string>.Failure([$"UOM conversion {id} not found for item {itemId}."]));

        _context.ItemUnitOfMeasureConversions.Remove(entity);
        await _context.SaveChangesAsync(ct);

        return Ok(ApiResponse<string>.Success("Deleted"));
    }

    /// <summary>
    /// Convert a quantity from one UOM to another for a specific item.
    /// Returns the converted quantity and the conversion factor used.
    /// </summary>
    [HttpPost("convert")]
    public async Task<ActionResult<ApiResponse<UomConvertResult>>> Convert(
        Guid itemId, [FromBody] UomConvertRequest request, CancellationToken ct)
    {
        if (request.FromUOM == request.ToUOM)
        {
            return Ok(ApiResponse<UomConvertResult>.Success(new UomConvertResult(
                request.Quantity, 1m, request.FromUOM, request.ToUOM)));
        }

        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null)
            return NotFound(ApiResponse<UomConvertResult>.Failure([$"Item {itemId} not found."]));

        // Try direct conversion
        var direct = await _context.ItemUnitOfMeasureConversions
            .FirstOrDefaultAsync(c => c.ItemId == itemId && c.FromUOM == request.FromUOM && c.ToUOM == request.ToUOM, ct);

        if (direct is not null)
        {
            var converted = request.Quantity * direct.ConversionFactor;
            return Ok(ApiResponse<UomConvertResult>.Success(new UomConvertResult(
                converted, direct.ConversionFactor, request.FromUOM, request.ToUOM)));
        }

        // Try reverse conversion
        var reverse = await _context.ItemUnitOfMeasureConversions
            .FirstOrDefaultAsync(c => c.ItemId == itemId && c.FromUOM == request.ToUOM && c.ToUOM == request.FromUOM, ct);

        if (reverse is not null && reverse.ConversionFactor != 0)
        {
            var converted = request.Quantity / reverse.ConversionFactor;
            return Ok(ApiResponse<UomConvertResult>.Success(new UomConvertResult(
                converted, 1m / reverse.ConversionFactor, request.FromUOM, request.ToUOM)));
        }

        // Try chain: FromUOM -> BaseUOM -> ToUOM
        if (item.BaseUnitOfMeasure != request.FromUOM && item.BaseUnitOfMeasure != request.ToUOM)
        {
            var toBase = await _context.ItemUnitOfMeasureConversions
                .FirstOrDefaultAsync(c => c.ItemId == itemId && c.FromUOM == request.FromUOM && c.ToUOM == item.BaseUnitOfMeasure, ct);
            var fromBase = await _context.ItemUnitOfMeasureConversions
                .FirstOrDefaultAsync(c => c.ItemId == itemId && c.FromUOM == item.BaseUnitOfMeasure && c.ToUOM == request.ToUOM, ct);

            if (toBase is not null && fromBase is not null)
            {
                var baseQty = request.Quantity * toBase.ConversionFactor;
                var converted = baseQty * fromBase.ConversionFactor;
                return Ok(ApiResponse<UomConvertResult>.Success(new UomConvertResult(
                    converted, toBase.ConversionFactor * fromBase.ConversionFactor, request.FromUOM, request.ToUOM)));
            }
        }

        return BadRequest(ApiResponse<UomConvertResult>.Failure(
            [$"No UOM conversion path found from '{request.FromUOM}' to '{request.ToUOM}' for item {item.ItemCode}. " +
             $"Define a conversion in Inventory → Item → UOM Conversions."]));
    }
}

public record UomConversionDto(Guid Id, Guid ItemId, string FromUOM, string ToUOM, decimal ConversionFactor);
public record CreateUomConversionRequest(string FromUOM, string ToUOM, decimal ConversionFactor);
public record UpdateUomConversionRequest(decimal ConversionFactor);
public record UomConvertRequest(string FromUOM, string ToUOM, decimal Quantity);
public record UomConvertResult(decimal ConvertedQuantity, decimal ConversionFactor, string FromUOM, string ToUOM);
