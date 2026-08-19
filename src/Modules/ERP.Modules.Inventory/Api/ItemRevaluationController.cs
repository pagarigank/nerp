// <copyright file="ItemRevaluationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/revaluations")]
public class ItemRevaluationController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemRevaluationController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemRevaluationDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] ItemRevaluationStatus? status,
        [FromQuery] RevaluationMethod? method,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.ItemRevaluations
            .Include(r => r.Lines)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (method.HasValue)
        {
            query = query.Where(r => r.Method == method.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RevaluationDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.RevaluationDate <= endDate.Value);
        }

        var revaluations = await query
            .OrderByDescending(r => r.RevaluationDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = revaluations.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ItemRevaluationDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var revaluation = await _context.ItemRevaluations
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (revaluation == null)
        {
            return NotFound(ApiResponse<ItemRevaluationDto>.Failure(["Item revaluation not found."]));
        }

        return Ok(ApiResponse<ItemRevaluationDto>.Success(MapToDto(revaluation)));
    }

    [HttpPost]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationDto>>> Create(
        [FromBody] CreateItemRevaluationRequest request,
        CancellationToken cancellationToken)
    {
        var revaluation = new ItemRevaluation(
            request.CompanyId,
            request.RevaluationNumber,
            request.RevaluationDate,
            request.Method,
            request.StandardCostAccountId,
            request.Notes);

        if (request.Lines != null)
        {
            foreach (var line in request.Lines)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                if (item == null)
                {
                    return BadRequest(ApiResponse<ItemRevaluationDto>.Failure([$"Item {line.ItemId} not found"]));
                }

                var warehouse = await _context.Warehouses.FindAsync(new object[] { line.WarehouseId }, cancellationToken);
                if (warehouse == null)
                {
                    return BadRequest(ApiResponse<ItemRevaluationDto>.Failure([$"Warehouse {line.WarehouseId} not found"]));
                }

                var stock = await _context.ItemStocks
                    .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId
                        && s.ItemId == line.ItemId
                        && s.WarehouseId == line.WarehouseId, cancellationToken);

                decimal currentQty = stock?.OnHandQuantity ?? 0;
                decimal currentCost = item.StandardCost ?? 0;

                var revaluationLine = new ItemRevaluationLine(
                    revaluation.Id,
                    line.ItemId,
                    line.WarehouseId,
                    currentQty,
                    currentCost,
                    line.NewStandardCost,
                    (line.NewStandardCost - currentCost) * currentQty,
                    line.ReasonCode);

                revaluation.AddLine(revaluationLine);
            }

            revaluation.SetTotalAdjustmentValue(revaluation.Lines.Sum(l => l.AdjustmentValue));
        }

        _context.ItemRevaluations.Add(revaluation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(revaluation);
        return CreatedAtAction(nameof(GetById), new { id = revaluation.Id }, ApiResponse<ItemRevaluationDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationDto>>> Update(
        Guid id,
        [FromBody] UpdateItemRevaluationRequest request,
        CancellationToken cancellationToken)
    {
        var revaluation = await _context.ItemRevaluations
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (revaluation == null)
        {
            return NotFound(ApiResponse<ItemRevaluationDto>.Failure(["Item revaluation not found."]));
        }

        if (revaluation.Status != ItemRevaluationStatus.Draft)
        {
            return BadRequest(ApiResponse<ItemRevaluationDto>.Failure(["Only draft revaluations can be updated."]));
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            revaluation.UpdateNotes(request.Notes);
        }

        if (request.StandardCostAccountId.HasValue)
        {
            // Note: StandardCostAccountId doesn't have a setter, would need to add one to entity
        }

        if (request.Lines != null)
        {
            // Remove existing lines
            _context.ItemRevaluationLines.RemoveRange(revaluation.Lines);

            foreach (var line in request.Lines)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                if (item == null)
                {
                    return BadRequest(ApiResponse<ItemRevaluationDto>.Failure([$"Item {line.ItemId} not found"]));
                }

                var warehouse = await _context.Warehouses.FindAsync(new object[] { line.WarehouseId }, cancellationToken);
                if (warehouse == null)
                {
                    return BadRequest(ApiResponse<ItemRevaluationDto>.Failure([$"Warehouse {line.WarehouseId} not found"]));
                }

                var stock = await _context.ItemStocks
                    .FirstOrDefaultAsync(s => s.CompanyId == revaluation.CompanyId
                        && s.ItemId == line.ItemId
                        && s.WarehouseId == line.WarehouseId, cancellationToken);

                decimal currentQty = stock?.OnHandQuantity ?? 0;
                decimal currentCost = item.StandardCost ?? 0;

                var revaluationLine = new ItemRevaluationLine(
                    revaluation.Id,
                    line.ItemId,
                    line.WarehouseId,
                    currentQty,
                    currentCost,
                    line.NewStandardCost,
                    (line.NewStandardCost - currentCost) * currentQty,
                    line.ReasonCode);

                revaluation.AddLine(revaluationLine);
            }

            revaluation.SetTotalAdjustmentValue(revaluation.Lines.Sum(l => l.AdjustmentValue));
        }

        _context.ItemRevaluations.Update(revaluation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemRevaluationDto>.Success(MapToDto(revaluation)));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationDto>>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var revaluation = await _context.ItemRevaluations.FindAsync(new object[] { id }, cancellationToken);

        if (revaluation == null)
        {
            return NotFound(ApiResponse<ItemRevaluationDto>.Failure(["Item revaluation not found."]));
        }

        if (revaluation.Status != ItemRevaluationStatus.Draft)
        {
            return BadRequest(ApiResponse<ItemRevaluationDto>.Failure(["Only draft revaluations can be approved."]));
        }

        revaluation.UpdateStatus(ItemRevaluationStatus.Approved);
        _context.ItemRevaluations.Update(revaluation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemRevaluationDto>.Success(MapToDto(revaluation)));
    }

    [HttpPost("{id:guid}/post")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationPostResultDto>>> Post(Guid id, CancellationToken cancellationToken)
    {
        var revaluation = await _context.ItemRevaluations
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (revaluation == null)
        {
            return NotFound(ApiResponse<ItemRevaluationPostResultDto>.Failure(["Item revaluation not found."]));
        }

        if (revaluation.Status != ItemRevaluationStatus.Approved)
        {
            return BadRequest(ApiResponse<ItemRevaluationPostResultDto>.Failure(["Only approved revaluations can be posted."]));
        }

        if (revaluation.Lines.Count == 0)
        {
            return BadRequest(ApiResponse<ItemRevaluationPostResultDto>.Failure(["Revaluation has no lines to post."]));
        }

        var results = new List<ItemRevaluationLineResultDto>();
        int postedCount = 0;

        foreach (var line in revaluation.Lines)
        {
            var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
            if (item == null)
            {
                results.Add(new ItemRevaluationLineResultDto
                {
                    ItemId = line.ItemId,
                    ItemCode = "UNKNOWN",
                    Success = false,
                    ErrorMessage = "Item not found",
                });
                continue;
            }

            var oldStandardCost = item.StandardCost ?? 0;
            var newStandardCost = line.NewStandardCost;

            // Update item standard cost
            item.UpdateStandardCost(newStandardCost);
            _context.Items.Update(item);

            // Create GL adjustment transaction if needed (for standard cost variance)
            var adjustmentAmount = (newStandardCost - oldStandardCost) * line.CurrentQuantity;
            if (Math.Abs(adjustmentAmount) > 0.0001m && revaluation.StandardCostAccountId.HasValue)
            {
                var adjustment = new InventoryTransaction(
                    revaluation.CompanyId,
                    line.ItemId,
                    line.WarehouseId,
                    TransactionType.Adjustment,
                    0, // Quantity is 0 for cost-only
                    item.BaseUnitOfMeasure,
                    newStandardCost - oldStandardCost,
                    revaluation.RevaluationDate,
                    null,
                    revaluation.StandardCostAccountId,
                    null,
                    $"REV-{revaluation.RevaluationNumber}",
                    null,
                    $"Standard cost revaluation. Old: {oldStandardCost}, New: {newStandardCost}. {revaluation.Notes}");

                _context.InventoryTransactions.Add(adjustment);
            }

            results.Add(new ItemRevaluationLineResultDto
            {
                ItemId = line.ItemId,
                ItemCode = item.ItemCode,
                OldStandardCost = oldStandardCost,
                NewStandardCost = newStandardCost,
                Quantity = line.CurrentQuantity,
                AdjustmentAmount = adjustmentAmount,
                Success = true,
            });

            postedCount++;
        }

        revaluation.UpdateStatus(ItemRevaluationStatus.Posted);
        _context.ItemRevaluations.Update(revaluation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new ItemRevaluationPostResultDto
        {
            RevaluationId = revaluation.Id,
            TotalLines = revaluation.Lines.Count,
            PostedLines = postedCount,
        };
        result.Lines.AddRange(results);

        return Ok(ApiResponse<ItemRevaluationPostResultDto>.Success(result));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationDto>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var revaluation = await _context.ItemRevaluations.FindAsync(new object[] { id }, cancellationToken);

        if (revaluation == null)
        {
            return NotFound(ApiResponse<ItemRevaluationDto>.Failure(["Item revaluation not found."]));
        }

        if (revaluation.Status == ItemRevaluationStatus.Posted)
        {
            return BadRequest(ApiResponse<ItemRevaluationDto>.Failure(["Cannot cancel a posted revaluation."]));
        }

        revaluation.UpdateStatus(ItemRevaluationStatus.Cancelled);
        _context.ItemRevaluations.Update(revaluation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemRevaluationDto>.Success(MapToDto(revaluation)));
    }

    [HttpPost("{id:guid}/generate-lines")]
    [Authorize(Roles = "InventoryManager,Admin")]
    public async Task<ActionResult<ApiResponse<ItemRevaluationDto>>> GenerateLines(
        Guid id,
        [FromBody] GenerateRevaluationLinesRequest request,
        CancellationToken cancellationToken)
    {
        var revaluation = await _context.ItemRevaluations
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (revaluation == null)
        {
            return NotFound(ApiResponse<ItemRevaluationDto>.Failure(["Item revaluation not found."]));
        }

        if (revaluation.Status != ItemRevaluationStatus.Draft)
        {
            return BadRequest(ApiResponse<ItemRevaluationDto>.Failure(["Only draft revaluations can generate lines."]));
        }

        // Remove existing lines
        _context.ItemRevaluationLines.RemoveRange(revaluation.Lines);

        var query = _context.ItemStocks
            .Where(s => s.CompanyId == revaluation.CompanyId && s.OnHandQuantity > 0);

        if (request.ItemIds != null && request.ItemIds.Count > 0)
        {
            query = query.Where(s => request.ItemIds.Contains(s.ItemId));
        }

        if (request.WarehouseIds != null && request.WarehouseIds.Count > 0)
        {
            query = query.Where(s => request.WarehouseIds.Contains(s.WarehouseId));
        }

        var stocks = await query.ToListAsync(cancellationToken);

        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var items = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        foreach (var stock in stocks)
        {
            var item = items.GetValueOrDefault(stock.ItemId);
            if (item == null)
            {
                continue;
            }

            decimal currentCost = item.StandardCost ?? 0;
            decimal newCost = currentCost;

            // Apply percentage change if specified
            if (request.PercentChange.HasValue)
            {
                decimal percent = request.PercentChange.Value;
                newCost = currentCost * (1 + (percent / 100));
            }

            // Or use flat rate if specified
            else if (request.FlatRate.HasValue)
            {
                newCost = request.FlatRate.Value;
            }

            if (newCost != currentCost)
            {
                var revaluationLine = new ItemRevaluationLine(
                    revaluation.Id,
                    stock.ItemId,
                    stock.WarehouseId,
                    stock.OnHandQuantity,
                    currentCost,
                    newCost,
                    (newCost - currentCost) * stock.OnHandQuantity,
                    request.ReasonCode);

                revaluation.AddLine(revaluationLine);
            }
        }

        revaluation.SetTotalAdjustmentValue(revaluation.Lines.Sum(l => l.AdjustmentValue));
        _context.ItemRevaluations.Update(revaluation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemRevaluationDto>.Success(MapToDto(revaluation)));
    }

    private ItemRevaluationDto MapToDto(ItemRevaluation revaluation)
    {
        var dto = new ItemRevaluationDto
        {
            Id = revaluation.Id,
            CompanyId = revaluation.CompanyId,
            RevaluationNumber = revaluation.RevaluationNumber,
            RevaluationDate = revaluation.RevaluationDate,
            Method = revaluation.Method.ToString(),
            StandardCostAccountId = revaluation.StandardCostAccountId,
            Notes = revaluation.Notes,
            Status = revaluation.Status.ToString(),
            TotalAdjustmentValue = revaluation.TotalAdjustmentValue,
            CreatedAt = revaluation.CreatedOn,
            CreatedBy = revaluation.CreatedBy,
        };
        dto.Lines.AddRange(revaluation.Lines.Select(l => new ItemRevaluationLineDto
        {
            Id = l.Id,
            ItemId = l.ItemId,
            WarehouseId = l.WarehouseId,
            CurrentQuantity = l.CurrentQuantity,
            CurrentStandardCost = l.CurrentStandardCost,
            NewStandardCost = l.NewStandardCost,
            AdjustmentValue = l.AdjustmentValue,
            ReasonCode = l.ReasonCode,
        }));
        return dto;
    }
}

#pragma warning disable CA1002, CA2227
public class ItemRevaluationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string RevaluationNumber { get; set; } = string.Empty;
    public DateTime RevaluationDate { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid? StandardCostAccountId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAdjustmentValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<ItemRevaluationLineDto> Lines { get; } = new List<ItemRevaluationLineDto>();
}
#pragma warning restore CA1002, CA2227

public class ItemRevaluationLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal CurrentStandardCost { get; set; }
    public decimal NewStandardCost { get; set; }
    public decimal AdjustmentValue { get; set; }
    public string? ReasonCode { get; set; }
}

#pragma warning disable CA1002, CA2227
public class CreateItemRevaluationRequest
{
    public Guid CompanyId { get; set; }
    public string RevaluationNumber { get; set; } = string.Empty;
    public DateTime RevaluationDate { get; set; }
    public RevaluationMethod Method { get; set; }
    public Guid? StandardCostAccountId { get; set; }
    public string? Notes { get; set; }
    public List<CreateItemRevaluationLineRequest> Lines { get; set; } = new List<CreateItemRevaluationLineRequest>();
}
#pragma warning restore CA1002, CA2227

public class CreateItemRevaluationLineRequest
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal NewStandardCost { get; set; }
    public string? ReasonCode { get; set; }
}

public class UpdateItemRevaluationRequest
{
    public string? Notes { get; set; }
    public Guid? StandardCostAccountId { get; set; }
    #pragma warning disable CA1002, CA2227
    public List<CreateItemRevaluationLineRequest> Lines { get; } = new List<CreateItemRevaluationLineRequest>();
    #pragma warning restore CA1002, CA2227
}

public class GenerateRevaluationLinesRequest
{
    #pragma warning disable CA1002, CA2227
    public List<Guid> ItemIds { get; } = new List<Guid>();
    public List<Guid> WarehouseIds { get; } = new List<Guid>();
    #pragma warning restore CA1002, CA2227
    public string? ABCClass { get; set; }
    public decimal? PercentChange { get; set; }
    public decimal? FlatRate { get; set; }
    public string? ReasonCode { get; set; }
}

#pragma warning disable CA1002, CA2227
public class ItemRevaluationPostResultDto
{
    public Guid RevaluationId { get; set; }
    public int TotalLines { get; set; }
    public int PostedLines { get; set; }
    public List<ItemRevaluationLineResultDto> Lines { get; } = new List<ItemRevaluationLineResultDto>();
}
#pragma warning restore CA1002, CA2227

public class ItemRevaluationLineResultDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal OldStandardCost { get; set; }
    public decimal NewStandardCost { get; set; }
    public decimal Quantity { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}