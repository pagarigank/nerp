// <copyright file="ConsignmentController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

[ApiController]
[Route("api/v1/inventory/consignment-stock")]
public class ConsignmentController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public ConsignmentController(InventoryDbContext context, IUnitOfWork unitOfWork, IDomainEventDispatcher domainEventDispatcher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ConsignmentStockDto>>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] Guid? vendorId, [FromQuery] Guid? itemId, CancellationToken cancellationToken)
    {
        var query = _context.ConsignmentStocks.AsQueryable();
        if (companyId.HasValue) query = query.Where(s => s.CompanyId == companyId.Value);
        if (vendorId.HasValue) query = query.Where(s => s.VendorId == vendorId.Value);
        if (itemId.HasValue) query = query.Where(s => s.ItemId == itemId.Value);
        var rows = await query.OrderBy(s => s.VendorId).ThenBy(s => s.ItemId).Take(1000).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ConsignmentStockDto>>.Success(rows.Select(MapToDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConsignmentStockDto>>> Create([FromBody] CreateConsignmentStockRequest request, CancellationToken cancellationToken)
    {
        var row = new ConsignmentStock(request.CompanyId, request.VendorId, request.ItemId, request.WarehouseId, request.QuantityOnHand, request.UnitOfMeasure, request.ConsignmentCost, request.LotId);
        _context.ConsignmentStocks.Add(row);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), ApiResponse<ConsignmentStockDto>.Success(MapToDto(row)));
    }

    /// <summary>
    /// Vendor delivers more consignment stock onto the premises.
    /// </summary>
    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<ConsignmentStockDto>>> Receive(Guid id, [FromBody] ConsignmentQtyRequest request, CancellationToken cancellationToken)
    {
        var row = await _context.ConsignmentStocks.FindAsync(new object[] { id }, cancellationToken);
        if (row is null) return NotFound(ApiResponse<ConsignmentStockDto>.Failure(["Consignment stock not found."]));
        row.Receive(request.Quantity);
        _context.ConsignmentStocks.Update(row);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<ConsignmentStockDto>.Success(MapToDto(row)));
    }

    /// <summary>
    /// Consume consignment stock into owned inventory. Emits a ConsignmentConsumedEvent
    /// the AP module can subscribe to (to create a payable to the vendor).
    /// </summary>
    [HttpPost("{id:guid}/consume")]
    public async Task<ActionResult<ApiResponse<ConsignmentStockDto>>> Consume(Guid id, [FromBody] ConsignmentConsumeRequest request, CancellationToken cancellationToken)
    {
        var row = await _context.ConsignmentStocks.FindAsync(new object[] { id }, cancellationToken);
        if (row is null) return NotFound(ApiResponse<ConsignmentStockDto>.Failure(["Consignment stock not found."]));
        try
        {
            row.Consume(request.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ConsignmentStockDto>.Failure([ex.Message]));
        }

        _context.ConsignmentStocks.Update(row);

        // Move the consumed qty into owned stock.
        var stock = await _context.ItemStocks
            .FirstOrDefaultAsync(s => s.CompanyId == row.CompanyId && s.ItemId == row.ItemId && s.WarehouseId == row.WarehouseId && s.LotId == row.LotId, cancellationToken);
        if (stock is null)
        {
            stock = new ItemStock(row.CompanyId, row.ItemId, row.WarehouseId, null, row.LotId);
            _context.ItemStocks.Add(stock);
        }

        stock.AdjustOnHand(request.Quantity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify AP via the domain event dispatcher (AP subscribes if wired).
        await _domainEventDispatcher.DispatchAsync(
            new ConsignmentConsumedEvent(row.Id, row.CompanyId, row.VendorId, row.ItemId, request.Quantity, request.UnitCost ?? row.ConsignmentCost ?? 0m, DateTime.UtcNow),
            cancellationToken);

        return Ok(ApiResponse<ConsignmentStockDto>.Success(MapToDto(row)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var row = await _context.ConsignmentStocks.FindAsync(new object[] { id }, cancellationToken);
        if (row is null) return NotFound(ApiResponse<string>.Failure(["Consignment stock not found."]));
        _context.ConsignmentStocks.Remove(row);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("deleted"));
    }

    private static ConsignmentStockDto MapToDto(ConsignmentStock s) => new ConsignmentStockDto
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        VendorId = s.VendorId,
        ItemId = s.ItemId,
        WarehouseId = s.WarehouseId,
        QuantityOnHand = s.QuantityOnHand,
        UnitOfMeasure = s.UnitOfMeasure,
        ConsignmentCost = s.ConsignmentCost,
        LotId = s.LotId,
    };
}

public class ConsignmentStockDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? ConsignmentCost { get; set; }
    public Guid? LotId { get; set; }
}

public class CreateConsignmentStockRequest
{
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? ConsignmentCost { get; set; }
    public Guid? LotId { get; set; }
}

public class ConsignmentQtyRequest
{
    public decimal Quantity { get; set; }
}

public class ConsignmentConsumeRequest
{
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}
