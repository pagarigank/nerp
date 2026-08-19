// <copyright file="PhysicalCountController.cs" company="ERP Project">
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
[Route("api/v1/inventory/physical-counts")]
public class PhysicalCountController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public PhysicalCountController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PhysicalCountDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] PhysicalCountStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.PhysicalCounts
            .Include(c => c.Lines)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(c => c.CompanyId == companyId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(c => c.WarehouseId == warehouseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(c => c.CountDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(c => c.CountDate <= endDate.Value);
        }

        var physicalCounts = await query
            .OrderByDescending(c => c.CountDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = physicalCounts.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<PhysicalCountDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PhysicalCountDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var physicalCount = await _context.PhysicalCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (physicalCount == null)
        {
            return NotFound(ApiResponse<PhysicalCountDto>.Failure(["Physical count not found."]));
        }

        return Ok(ApiResponse<PhysicalCountDto>.Success(MapToDto(physicalCount)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PhysicalCountDto>>> Create(
        [FromBody] CreatePhysicalCountRequest request,
        CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<PhysicalCountDto>.Failure([$"Warehouse {request.WarehouseId} not found"]));
        }

        var physicalCount = new PhysicalCount(
            request.CompanyId,
            request.WarehouseId,
            request.CountNumber,
            request.CountDate,
            PhysicalCountStatus.Draft,
            request.BlindCount,
            request.Notes);

        if (request.Lines != null)
        {
            foreach (var line in request.Lines)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                if (item == null)
                {
                    return BadRequest(ApiResponse<PhysicalCountDto>.Failure([$"Item {line.ItemId} not found"]));
                }

                var systemQty = await GetSystemQuantityAsync(
                    line.ItemId,
                    request.WarehouseId,
                    line.BinId,
                    line.LotNumber,
                    line.SerialNumber,
                    cancellationToken);

                var physicalCountLine = new PhysicalCountLine(
                    physicalCount.Id,
                    line.ItemId,
                    line.BinId,
                    systemQty,
                    line.CountedQuantity,
                    line.LotNumber,
                    line.SerialNumber,
                    line.Notes);

                physicalCount.AddLine(physicalCountLine);
            }
        }

        _context.PhysicalCounts.Add(physicalCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(physicalCount);
        return CreatedAtAction(nameof(GetById), new { id = physicalCount.Id }, ApiResponse<PhysicalCountDto>.Success(dto));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ApiResponse<PhysicalCountDto>>> StartCount(Guid id, CancellationToken cancellationToken)
    {
        var physicalCount = await _context.PhysicalCounts.FindAsync(new object[] { id }, cancellationToken);
        if (physicalCount == null)
        {
            return NotFound(ApiResponse<PhysicalCountDto>.Failure(["Physical count not found."]));
        }

        if (physicalCount.Status != PhysicalCountStatus.Draft)
        {
            return BadRequest(ApiResponse<PhysicalCountDto>.Failure(["Only draft physical counts can be started."]));
        }

        physicalCount.UpdateStatus(PhysicalCountStatus.InProgress);
        _context.PhysicalCounts.Update(physicalCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<PhysicalCountDto>.Success(MapToDto(physicalCount)));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<PhysicalCountDto>>> CompleteCount(Guid id, CancellationToken cancellationToken)
    {
        var physicalCount = await _context.PhysicalCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (physicalCount == null)
        {
            return NotFound(ApiResponse<PhysicalCountDto>.Failure(["Physical count not found."]));
        }

        if (physicalCount.Status != PhysicalCountStatus.InProgress)
        {
            return BadRequest(ApiResponse<PhysicalCountDto>.Failure(["Only in-progress physical counts can be completed."]));
        }

        var missingLines = physicalCount.Lines.Where(l => !l.CountedQuantity.HasValue).ToList();
        if (missingLines.Count > 0)
        {
            return BadRequest(ApiResponse<PhysicalCountDto>.Failure([$"{missingLines.Count} lines are missing counted quantities."]));
        }

        physicalCount.UpdateStatus(PhysicalCountStatus.Completed);
        _context.PhysicalCounts.Update(physicalCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<PhysicalCountDto>.Success(MapToDto(physicalCount)));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<PhysicalCountPostResultDto>>> PostPhysicalCount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var physicalCount = await _context.PhysicalCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (physicalCount == null)
        {
            return NotFound(ApiResponse<PhysicalCountPostResultDto>.Failure(["Physical count not found."]));
        }

        if (physicalCount.Status != PhysicalCountStatus.Completed)
        {
            return BadRequest(ApiResponse<PhysicalCountPostResultDto>.Failure(["Only completed physical counts can be posted."]));
        }

        var variances = new List<PhysicalCountVarianceDto>();
        int adjustmentsCreated = 0;

        foreach (var line in physicalCount.Lines)
        {
            var variance = line.Variance ?? 0;
            if (Math.Abs(variance) > 0.0001m)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                var avgCost = await GetAverageCostAsync(line.ItemId, physicalCount.WarehouseId, cancellationToken);

                Guid? adjustmentId = null;

                var adjustment = new InventoryTransaction(
                    physicalCount.CompanyId,
                    line.ItemId,
                    physicalCount.WarehouseId,
                    TransactionType.Adjustment,
                    variance,
                    item?.BaseUnitOfMeasure ?? "EA",
                    avgCost ?? item?.StandardCost ?? 0m,
                    DateTime.UtcNow,
                    line.BinId,
                    null,
                    line.SerialNumber,
                    $"PC-{physicalCount.CountNumber}",
                    null,
                    $"Physical count adjustment. Count date: {physicalCount.CountDate:yyyy-MM-dd}. {line.Notes}");

                _context.InventoryTransactions.Add(adjustment);
                adjustmentId = adjustment.Id;
                adjustmentsCreated++;

                variances.Add(new PhysicalCountVarianceDto
                {
                    ItemId = line.ItemId,
                    ItemCode = item?.ItemCode ?? "UNKNOWN",
                    ItemDescription = item?.Description ?? "UNKNOWN",
                    BinId = line.BinId,
                    LotNumber = line.LotNumber,
                    SerialNumber = line.SerialNumber,
                    SystemQuantity = line.SystemQuantity,
                    CountedQuantity = line.CountedQuantity!.Value,
                    Variance = variance,
                    VarianceValue = variance * (avgCost ?? item?.StandardCost ?? 0m),
                    AdjustmentTransactionId = adjustmentId,
                });
            }
        }

        physicalCount.UpdateStatus(PhysicalCountStatus.Posted);
        _context.PhysicalCounts.Update(physicalCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new PhysicalCountPostResultDto
        {
            PhysicalCountId = physicalCount.Id,
            TotalLines = physicalCount.Lines.Count,
            VariancesFound = variances.Count,
            AdjustmentsCreated = adjustmentsCreated,
        };
        result.Variances.AddRange(variances);

        return Ok(ApiResponse<PhysicalCountPostResultDto>.Success(result));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<PhysicalCountDto>>> CancelCount(Guid id, CancellationToken cancellationToken)
    {
        var physicalCount = await _context.PhysicalCounts.FindAsync(new object[] { id }, cancellationToken);
        if (physicalCount == null)
        {
            return NotFound(ApiResponse<PhysicalCountDto>.Failure(["Physical count not found."]));
        }

        if (physicalCount.Status == PhysicalCountStatus.Posted)
        {
            return BadRequest(ApiResponse<PhysicalCountDto>.Failure(["Cannot cancel a posted physical count."]));
        }

        physicalCount.UpdateStatus(PhysicalCountStatus.Cancelled);
        _context.PhysicalCounts.Update(physicalCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<PhysicalCountDto>.Success(MapToDto(physicalCount)));
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<ApiResponse<PhysicalCountDto>>> UpdateLine(
        Guid id,
        Guid lineId,
        [FromBody] UpdatePhysicalCountLineRequest request,
        CancellationToken cancellationToken)
    {
        var physicalCount = await _context.PhysicalCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (physicalCount == null)
        {
            return NotFound(ApiResponse<PhysicalCountDto>.Failure(["Physical count not found."]));
        }

        if (physicalCount.Status != PhysicalCountStatus.InProgress)
        {
            return BadRequest(ApiResponse<PhysicalCountDto>.Failure(["Lines can only be updated when physical count is in progress."]));
        }

        var line = physicalCount.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line == null)
        {
            return NotFound(ApiResponse<PhysicalCountDto>.Failure(["Line not found."]));
        }

        if (request.CountedQuantity.HasValue)
        {
            line.SetCountedQuantity(request.CountedQuantity.Value);
        }

        if (request.Notes != null)
        {
            line.UpdateNotes(request.Notes);
        }

        _context.PhysicalCountLines.Update(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<PhysicalCountDto>.Success(MapToDto(physicalCount)));
    }

    [HttpPost("generate-count-sheet")]
    public async Task<ActionResult<ApiResponse<CountSheetDto>>> GenerateCountSheet(
        [FromBody] GenerateCountSheetRequest request,
        CancellationToken cancellationToken)
    {
        var query = _context.ItemStocks
            .Where(s => s.WarehouseId == request.WarehouseId);

        var stocks = await query.ToListAsync(cancellationToken);

        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var binIds = stocks.Where(s => s.BinId.HasValue).Select(s => s.BinId!.Value).Distinct().ToList();

        var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        var bins = await _context.WarehouseBins.Where(b => binIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken);

        // Filter by ABCClass if provided
        if (!string.IsNullOrEmpty(request.ABCClass))
        {
            var filteredItemIds = items.Where(kvp => kvp.Value.ABCClass == request.ABCClass).Select(kvp => kvp.Key).ToHashSet();
            stocks = stocks.Where(s => filteredItemIds.Contains(s.ItemId)).ToList();
        }

        var lines = stocks.Select(s =>
        {
            var item = items.GetValueOrDefault(s.ItemId);
            var bin = s.BinId.HasValue ? bins.GetValueOrDefault(s.BinId.Value) : null;
            return new CountSheetLineDto
            {
                ItemId = s.ItemId,
                ItemCode = item?.ItemCode ?? string.Empty,
                ItemDescription = item?.Description ?? string.Empty,
                BinId = s.BinId,
                BinCode = bin?.BinCode,
                SystemQuantity = s.OnHandQuantity,
                UnitOfMeasure = item?.BaseUnitOfMeasure ?? string.Empty,
                CountedQuantity = 0,
                LotNumber = string.Empty,
                SerialNumber = string.Empty,
            };
        }).ToList();

        var result = new CountSheetDto
        {
            WarehouseId = request.WarehouseId,
            WarehouseCode = (await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken))?.WarehouseCode,
            GeneratedAt = DateTime.UtcNow,
        };
        result.Lines.AddRange(lines);

        return Ok(ApiResponse<CountSheetDto>.Success(result));
    }

    private async Task<decimal> GetSystemQuantityAsync(
        Guid itemId,
        Guid warehouseId,
        Guid? binId,
        string? lotNumber,
        string? serialNumber,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId);

        if (binId.HasValue)
        {
            query = query.Where(t => t.BinId == binId.Value);
        }

        if (!string.IsNullOrEmpty(lotNumber))
        {
            var lot = await _context.Lots.FirstOrDefaultAsync(l => l.ItemId == itemId && l.LotNumber == lotNumber, cancellationToken);
            if (lot != null)
            {
                query = query.Where(t => t.LotId == lot.Id);
            }
        }

        if (!string.IsNullOrEmpty(serialNumber))
        {
            query = query.Where(t => t.SerialNumber == serialNumber);
        }

        return await query.SumAsync(t => t.Quantity, cancellationToken);
    }

    private async Task<decimal?> GetAverageCostAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var receipts = await _context.InventoryTransactions
            .Where(t => t.ItemId == itemId && t.WarehouseId == warehouseId && t.Quantity > 0)
            .Select(t => new { t.Quantity, t.ExtendedCost })
            .ToListAsync(cancellationToken);

        if (receipts.Count == 0)
        {
            return null;
        }

        var totalQty = receipts.Sum(r => r.Quantity);
        var totalCost = receipts.Sum(r => r.ExtendedCost);

        return totalQty > 0 ? totalCost / totalQty : null;
    }

    private PhysicalCountDto MapToDto(PhysicalCount physicalCount)
    {
        var dto = new PhysicalCountDto
        {
            Id = physicalCount.Id,
            CompanyId = physicalCount.CompanyId,
            WarehouseId = physicalCount.WarehouseId,
            CountNumber = physicalCount.CountNumber,
            CountDate = physicalCount.CountDate,
            Status = physicalCount.Status.ToString(),
            BlindCount = physicalCount.BlindCount,
            Notes = physicalCount.Notes,
            CreatedAt = physicalCount.CreatedOn,
            CreatedBy = physicalCount.CreatedBy,
        };
        dto.Lines.AddRange(physicalCount.Lines.Select(l => new PhysicalCountLineDto
        {
            Id = l.Id,
            ItemId = l.ItemId,
            BinId = l.BinId,
            SystemQuantity = l.SystemQuantity,
            CountedQuantity = l.CountedQuantity,
            Variance = l.Variance,
            LotNumber = l.LotNumber,
            SerialNumber = l.SerialNumber,
            Notes = l.Notes,
        }));
        return dto;
    }
}

#pragma warning disable CA1002, CA2227
public class PhysicalCountDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool BlindCount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<PhysicalCountLineDto> Lines { get; } = new List<PhysicalCountLineDto>();
}
#pragma warning restore CA1002, CA2227

public class PhysicalCountLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid? BinId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    public decimal? Variance { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}

#pragma warning disable CA1002
public class CreatePhysicalCountRequest
{
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public bool BlindCount { get; set; }
    public string? Notes { get; set; }
    public List<CreatePhysicalCountLineRequest> Lines { get; } = new List<CreatePhysicalCountLineRequest>();
}
#pragma warning restore CA1002

public class CreatePhysicalCountLineRequest
{
    public Guid ItemId { get; set; }
    public Guid? BinId { get; set; }
    public decimal? CountedQuantity { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePhysicalCountLineRequest
{
    public decimal? CountedQuantity { get; set; }
    public string? Notes { get; set; }
}

#pragma warning disable CA1002, CA2227
public class PhysicalCountPostResultDto
{
    public Guid PhysicalCountId { get; set; }
    public int TotalLines { get; set; }
    public int VariancesFound { get; set; }
    public int AdjustmentsCreated { get; set; }
    public List<PhysicalCountVarianceDto> Variances { get; } = new List<PhysicalCountVarianceDto>();
}
#pragma warning restore CA1002, CA2227

public class PhysicalCountVarianceDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public Guid? BinId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Variance { get; set; }
    public decimal VarianceValue { get; set; }
    public Guid? AdjustmentTransactionId { get; set; }
}

public class GenerateCountSheetRequest
{
    public Guid WarehouseId { get; set; }
    public string? ABCClass { get; set; }
}

#pragma warning disable CA1002, CA2227
public class CountSheetDto
{
    public Guid WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<CountSheetLineDto> Lines { get; } = new List<CountSheetLineDto>();
}
#pragma warning restore CA1002, CA2227

#pragma warning disable CA1002
public class CountSheetLineDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public Guid? BinId { get; set; }
    public string? BinCode { get; set; }
    public decimal SystemQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CountedQuantity { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
}
#pragma warning restore CA1002