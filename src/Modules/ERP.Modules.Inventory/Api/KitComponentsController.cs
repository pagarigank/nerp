// <copyright file="KitComponentsController.cs" company="ERP Project">
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
[Route("api/v1/inventory/kit-components")]
public class KitComponentsController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public KitComponentsController(InventoryDbContext context, IUnitOfWork unitOfWork, IDomainEventDispatcher domainEventDispatcher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    [HttpGet("by-kit/{kitItemId:guid}")]
    public async Task<ActionResult<ApiResponse<List<KitComponentDto>>>> GetByKit(Guid kitItemId, CancellationToken cancellationToken)
    {
        var comps = await _context.KitComponents.Where(c => c.KitItemId == kitItemId).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<KitComponentDto>>.Success(comps.Select(MapToDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<KitComponentDto>>> Create([FromBody] CreateKitComponentRequest request, CancellationToken cancellationToken)
    {
        var kit = await _context.Items.FindAsync(new object[] { request.KitItemId }, cancellationToken);
        if (kit is null) return BadRequest(ApiResponse<KitComponentDto>.Failure([$"Kit item {request.KitItemId} not found"]));
        var comp = await _context.Items.FindAsync(new object[] { request.ComponentItemId }, cancellationToken);
        if (comp is null) return BadRequest(ApiResponse<KitComponentDto>.Failure([$"Component item {request.ComponentItemId} not found"]));

        var kc = new KitComponent(request.CompanyId, request.KitItemId, request.ComponentItemId, request.QuantityPerKit, request.UnitOfMeasure);
        kit.SetKit(true);
        _context.KitComponents.Add(kc);
        _context.Items.Update(kit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetByKit), new { kitItemId = kc.KitItemId }, ApiResponse<KitComponentDto>.Success(MapToDto(kc)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<KitComponentDto>>> Update(Guid id, [FromBody] UpdateKitComponentRequest request, CancellationToken cancellationToken)
    {
        var kc = await _context.KitComponents.FindAsync(new object[] { id }, cancellationToken);
        if (kc is null) return NotFound(ApiResponse<KitComponentDto>.Failure(["Kit component not found."]));
        kc.Update(request.QuantityPerKit, request.UnitOfMeasure);
        _context.KitComponents.Update(kc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<KitComponentDto>.Success(MapToDto(kc)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var kc = await _context.KitComponents.FindAsync(new object[] { id }, cancellationToken);
        if (kc is null) return NotFound(ApiResponse<string>.Failure(["Kit component not found."]));
        _context.KitComponents.Remove(kc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("deleted"));
    }

    [HttpPost("receipt")]
    public async Task<ActionResult<ApiResponse<string>>> KitReceipt([FromBody] KitTransactionRequest request, CancellationToken cancellationToken)
    {
        var kit = await _context.Items.FindAsync(new object[] { request.KitItemId }, cancellationToken);
        if (kit is null) return BadRequest(ApiResponse<string>.Failure([$"Kit item {request.KitItemId} not found"]));

        var components = await _context.KitComponents.Where(c => c.KitItemId == request.KitItemId).ToListAsync(cancellationToken);
        if (components.Count == 0) return BadRequest(ApiResponse<string>.Failure(["Kit has no components defined."]));

        var kitTxn = new InventoryTransaction(request.CompanyId, request.KitItemId, request.WarehouseId, TransactionType.Receipt, request.Quantity, request.UnitOfMeasure, kit.StandardCost ?? 0m, request.TransactionDate, request.BinId, request.LotId, null, request.ReferenceNumber, null, request.Notes);
        _context.InventoryTransactions.Add(kitTxn);

        var events = new List<InventoryTransactionPostedEvent>
        {
            new InventoryTransactionPostedEvent(kitTxn.Id, request.CompanyId, request.KitItemId, request.WarehouseId, TransactionType.Receipt.ToString(), kitTxn.Quantity, kitTxn.UnitCost, kitTxn.ExtendedCost, kitTxn.TransactionDate, null),
        };

        foreach (var comp in components)
        {
            var compItem = await _context.Items.FindAsync(new object[] { comp.ComponentItemId }, cancellationToken);
            var compQty = comp.QuantityPerKit * request.Quantity;
            var compTxn = new InventoryTransaction(request.CompanyId, comp.ComponentItemId, request.WarehouseId, TransactionType.Issue, -compQty, comp.UnitOfMeasure, compItem?.StandardCost ?? 0m, request.TransactionDate, request.BinId, request.LotId, null, request.ReferenceNumber, null, $"Kitting {kit.ItemCode}");
            _context.InventoryTransactions.Add(compTxn);
            events.Add(new InventoryTransactionPostedEvent(compTxn.Id, request.CompanyId, comp.ComponentItemId, request.WarehouseId, TransactionType.Issue.ToString(), compTxn.Quantity, compTxn.UnitCost, compTxn.ExtendedCost, compTxn.TransactionDate, null));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var e in events)
            await _domainEventDispatcher.DispatchAsync(e, cancellationToken);

        return Ok(ApiResponse<string>.Success("kit receipt posted"));
    }

    [HttpPost("issue")]
    public async Task<ActionResult<ApiResponse<string>>> KitIssue([FromBody] KitTransactionRequest request, CancellationToken cancellationToken)
    {
        var kit = await _context.Items.FindAsync(new object[] { request.KitItemId }, cancellationToken);
        if (kit is null) return BadRequest(ApiResponse<string>.Failure([$"Kit item {request.KitItemId} not found"]));

        var components = await _context.KitComponents.Where(c => c.KitItemId == request.KitItemId).ToListAsync(cancellationToken);
        if (components.Count == 0) return BadRequest(ApiResponse<string>.Failure(["Kit has no components defined."]));

        var kitTxn = new InventoryTransaction(request.CompanyId, request.KitItemId, request.WarehouseId, TransactionType.Issue, -request.Quantity, request.UnitOfMeasure, kit.StandardCost ?? 0m, request.TransactionDate, request.BinId, request.LotId, null, request.ReferenceNumber, null, request.Notes);
        _context.InventoryTransactions.Add(kitTxn);

        var events = new List<InventoryTransactionPostedEvent>
        {
            new InventoryTransactionPostedEvent(kitTxn.Id, request.CompanyId, request.KitItemId, request.WarehouseId, TransactionType.Issue.ToString(), kitTxn.Quantity, kitTxn.UnitCost, kitTxn.ExtendedCost, kitTxn.TransactionDate, null),
        };

        foreach (var comp in components)
        {
            var compItem = await _context.Items.FindAsync(new object[] { comp.ComponentItemId }, cancellationToken);
            var compQty = comp.QuantityPerKit * request.Quantity;
            var compTxn = new InventoryTransaction(request.CompanyId, comp.ComponentItemId, request.WarehouseId, TransactionType.Receipt, compQty, comp.UnitOfMeasure, compItem?.StandardCost ?? 0m, request.TransactionDate, request.BinId, request.LotId, null, request.ReferenceNumber, null, $"Unkitting {kit.ItemCode}");
            _context.InventoryTransactions.Add(compTxn);
            events.Add(new InventoryTransactionPostedEvent(compTxn.Id, request.CompanyId, comp.ComponentItemId, request.WarehouseId, TransactionType.Receipt.ToString(), compTxn.Quantity, compTxn.UnitCost, compTxn.ExtendedCost, compTxn.TransactionDate, null));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var e in events)
            await _domainEventDispatcher.DispatchAsync(e, cancellationToken);

        return Ok(ApiResponse<string>.Success("kit issue posted"));
    }

    private static KitComponentDto MapToDto(KitComponent c) => new KitComponentDto
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        KitItemId = c.KitItemId,
        ComponentItemId = c.ComponentItemId,
        QuantityPerKit = c.QuantityPerKit,
        UnitOfMeasure = c.UnitOfMeasure,
    };
}

public class KitComponentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid KitItemId { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerKit { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}

public class CreateKitComponentRequest
{
    public Guid CompanyId { get; set; }
    public Guid KitItemId { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerKit { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}

public class UpdateKitComponentRequest
{
    public decimal QuantityPerKit { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}

public class KitTransactionRequest
{
    public Guid CompanyId { get; set; }
    public Guid KitItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid? BinId { get; set; }
    public Guid? LotId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
