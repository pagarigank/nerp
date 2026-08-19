// <copyright file="ItemStockController.cs" company="ERP Project">
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
[Route("api/v1/inventory/item-stock")]
public class ItemStockController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public ItemStockController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemStockDto>>>> GetAll(
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var query = _context.ItemStocks.AsQueryable();

        if (itemId.HasValue)
        {
            query = query.Where(s => s.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        }

        var stocks = await query.ToListAsync(cancellationToken);
        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var warehouseIds = stocks.Select(s => s.WarehouseId).Distinct().ToList();

        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        var warehouses = await _context.Warehouses.Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);

        var dtos = stocks.Select(s => new ItemStockDto
        {
            Id = s.Id,
            ItemId = s.ItemId,
            ItemCode = items[s.ItemId].ItemCode,
            ItemDescription = items[s.ItemId].Description,
            WarehouseId = s.WarehouseId,
            WarehouseCode = warehouses[s.WarehouseId].WarehouseCode,
            BinId = s.BinId,
            QuantityOnHand = s.OnHandQuantity,
            QuantityAllocated = s.AllocatedQuantity,
            QuantityOnOrder = s.OnOrderQuantity,
            QuantityAvailable = s.AvailableQuantity,
        }).ToList();

        return Ok(ApiResponse<List<ItemStockDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemStockDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stock = await _context.ItemStocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (stock == null)
        {
            return NotFound(ApiResponse<ItemStockDto>.Failure(["Stock record not found."]));
        }

        var item = await _context.Items.FindAsync(new object[] { stock.ItemId }, cancellationToken);
        var warehouse = await _context.Warehouses.FindAsync(new object[] { stock.WarehouseId }, cancellationToken);

        var dto = new ItemStockDto
        {
            Id = stock.Id,
            ItemId = stock.ItemId,
            ItemCode = item!.ItemCode,
            ItemDescription = item.Description,
            WarehouseId = stock.WarehouseId,
            WarehouseCode = warehouse!.WarehouseCode,
            BinId = stock.BinId,
            QuantityOnHand = stock.OnHandQuantity,
            QuantityAllocated = stock.AllocatedQuantity,
            QuantityOnOrder = stock.OnOrderQuantity,
            QuantityAvailable = stock.AvailableQuantity,
        };

        return Ok(ApiResponse<ItemStockDto>.Success(dto));
    }

    [HttpGet("by-item/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<ItemStockSummaryDto>>> GetStockSummaryByItem(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var stocks = await _context.ItemStocks
            .Where(s => s.ItemId == itemId)
            .ToListAsync(cancellationToken);

        var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);

        if (item == null)
        {
            return NotFound(ApiResponse<ItemStockSummaryDto>.Failure(["Item not found."]));
        }

        var warehouseIds = stocks.Select(s => s.WarehouseId).ToList();
        var warehouses = await _context.Warehouses
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var summary = new ItemStockSummaryDto
        {
            ItemId = itemId,
            ItemCode = item.ItemCode,
            ItemDescription = item.Description,
            TotalOnHand = stocks.Sum(s => s.OnHandQuantity),
            TotalAllocated = stocks.Sum(s => s.AllocatedQuantity),
            TotalOnOrder = stocks.Sum(s => s.OnOrderQuantity),
            TotalAvailable = stocks.Sum(s => s.AvailableQuantity),
            ByWarehouse = stocks.Select(s => new WarehouseStockDto
            {
                WarehouseId = s.WarehouseId,
                WarehouseCode = warehouses[s.WarehouseId].WarehouseCode,
                WarehouseName = warehouses[s.WarehouseId].WarehouseName,
                QuantityOnHand = s.OnHandQuantity,
                QuantityAllocated = s.AllocatedQuantity,
                QuantityAvailable = s.AvailableQuantity,
            }).ToList(),
        };

        return Ok(ApiResponse<ItemStockSummaryDto>.Success(summary));
    }
}

public class ItemStockDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public Guid? BinId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAllocated { get; set; }
    public decimal QuantityOnOrder { get; set; }
    public decimal QuantityAvailable { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

public class ItemStockSummaryDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal TotalOnHand { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal TotalOnOrder { get; set; }
    public decimal TotalAvailable { get; set; }

#pragma warning disable CA1002, CA2227
    public List<WarehouseStockDto> ByWarehouse { get; set; } = new ();
#pragma warning restore CA1002, CA2227
}

public class WarehouseStockDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAllocated { get; set; }
    public decimal QuantityAvailable { get; set; }
}
