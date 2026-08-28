// <copyright file="CycleCountController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/cycle-counts")]
public class CycleCountController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CycleCountController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CycleCountDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] CycleCountStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.CycleCounts
            .Include(c => c.Lines)
            .AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, c => c.CompanyId, companyId);

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

        var cycleCounts = await query
            .OrderByDescending(c => c.CountDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = cycleCounts.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<CycleCountDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Cycle count not found."]));
        }

        return Ok(ApiResponse<CycleCountDto>.Success(MapToDto(cycleCount)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> Create(
        [FromBody] CreateCycleCountRequest request,
        CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure([$"Warehouse {request.WarehouseId} not found"]));
        }

        var cycleCount = new CycleCount(
            request.CompanyId,
            request.WarehouseId,
            request.CountNumber,
            request.CountDate,
            CycleCountStatus.Draft,
            request.Notes);

        if (request.Lines != null)
        {
            foreach (var line in request.Lines)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                if (item == null)
                {
                    return BadRequest(ApiResponse<CycleCountDto>.Failure([$"Item {line.ItemId} not found"]));
                }

                // Get system quantity for this item/warehouse/bin/lot/serial
                var systemQty = await GetSystemQuantityAsync(
                    line.ItemId,
                    request.WarehouseId,
                    line.BinId,
                    line.LotNumber,
                    line.SerialNumber,
                    cancellationToken);

                var cycleCountLine = new CycleCountLine(
                    cycleCount.Id,
                    line.ItemId,
                    line.BinId,
                    systemQty,
                    line.CountedQuantity,
                    line.LotNumber,
                    line.SerialNumber,
                    line.Notes);

                cycleCount.AddLine(cycleCountLine);
            }
        }

        _context.CycleCounts.Add(cycleCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(cycleCount);
        return CreatedAtAction(nameof(GetById), new { id = cycleCount.Id }, ApiResponse<CycleCountDto>.Success(dto));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> StartCount(Guid id, CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts.FindAsync(new object[] { id }, cancellationToken);
        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Cycle count not found."]));
        }

        if (cycleCount.Status != CycleCountStatus.Draft)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure(["Only draft cycle counts can be started."]));
        }

        cycleCount.UpdateStatus(CycleCountStatus.InProgress);
        _context.CycleCounts.Update(cycleCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CycleCountDto>.Success(MapToDto(cycleCount)));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> CompleteCount(Guid id, CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Cycle count not found."]));
        }

        if (cycleCount.Status != CycleCountStatus.InProgress)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure(["Only in-progress cycle counts can be completed."]));
        }

        // Validate all lines have counted quantities
        var missingLines = cycleCount.Lines.Where(l => !l.CountedQuantity.HasValue).ToList();
        if (missingLines.Count > 0)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure([$"{missingLines.Count} lines are missing counted quantities."]));
        }

        cycleCount.UpdateStatus(CycleCountStatus.Completed);
        _context.CycleCounts.Update(cycleCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CycleCountDto>.Success(MapToDto(cycleCount)));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> ApproveCycleCount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Cycle count not found."]));
        }

        if (cycleCount.Status != CycleCountStatus.Completed)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure(["Only completed cycle counts can be approved."]));
        }

        cycleCount.UpdateStatus(CycleCountStatus.Approved);
        _context.CycleCounts.Update(cycleCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CycleCountDto>.Success(MapToDto(cycleCount)));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<CycleCountPostResultDto>>> PostCycleCount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountPostResultDto>.Failure(["Cycle count not found."]));
        }

        if (cycleCount.Status != CycleCountStatus.Completed)
        {
            return BadRequest(ApiResponse<CycleCountPostResultDto>.Failure(["Only completed cycle counts can be posted."]));
        }

        // Phase 7 gap: Cycle count >$1,000 requires approval before posting
        decimal totalVarianceValue = 0m;
        foreach (var preLine in cycleCount.Lines)
        {
            var preVariance = preLine.Variance ?? 0;
            if (Math.Abs(preVariance) > 0.0001m)
            {
                var preItem = await _context.Items.FindAsync(new object[] { preLine.ItemId }, cancellationToken);
                var preAvgCost = await GetAverageCostAsync(preLine.ItemId, cycleCount.WarehouseId, cancellationToken);
                totalVarianceValue += Math.Abs(preVariance) * (preAvgCost ?? preItem?.StandardCost ?? 0m);
            }
        }

        if (totalVarianceValue > 1000m && cycleCount.Status != CycleCountStatus.Approved)
        {
            return BadRequest(ApiResponse<CycleCountPostResultDto>.Failure([$"Cycle count variance value (${totalVarianceValue:N2}) exceeds $1,000 threshold and requires approval before posting. Use POST /inventory/cycle-counts/{id}/approve to approve."]));
        }

        var variances = new List<CycleCountVarianceDto>();
        int adjustmentsCreated = 0;

        foreach (var line in cycleCount.Lines)
        {
            var variance = line.Variance ?? 0;
            if (Math.Abs(variance) > 0.0001m)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                var avgCost = await GetAverageCostAsync(line.ItemId, cycleCount.WarehouseId, cancellationToken);

                Guid? adjustmentId = null;

                // Create adjustment transaction
                var adjustment = new InventoryTransaction(
                    cycleCount.CompanyId,
                    line.ItemId,
                    cycleCount.WarehouseId,
                    TransactionType.Adjustment,
                    variance,
                    item?.BaseUnitOfMeasure ?? "EA",
                    avgCost ?? item?.StandardCost ?? 0m,
                    DateTime.UtcNow,
                    line.BinId,
                    null, // LotId - would need to be looked up
                    line.SerialNumber,
                    $"CC-{cycleCount.CountNumber}",
                    null, // ProjectId
                    $"Cycle count adjustment. Count date: {cycleCount.CountDate:yyyy-MM-dd}. {line.Notes}");

                _context.InventoryTransactions.Add(adjustment);
                adjustmentId = adjustment.Id;
                adjustmentsCreated++;

                variances.Add(new CycleCountVarianceDto
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

        cycleCount.UpdateStatus(CycleCountStatus.Posted);
        _context.CycleCounts.Update(cycleCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new CycleCountPostResultDto
        {
            CycleCountId = cycleCount.Id,
            TotalLines = cycleCount.Lines.Count,
            VariancesFound = variances.Count,
            AdjustmentsCreated = adjustmentsCreated,
        };
        result.Variances.AddRange(variances);

        return Ok(ApiResponse<CycleCountPostResultDto>.Success(result));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> CancelCount(Guid id, CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts.FindAsync(new object[] { id }, cancellationToken);
        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Cycle count not found."]));
        }

        if (cycleCount.Status == CycleCountStatus.Posted)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure(["Cannot cancel a posted cycle count."]));
        }

        cycleCount.UpdateStatus(CycleCountStatus.Cancelled);
        _context.CycleCounts.Update(cycleCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CycleCountDto>.Success(MapToDto(cycleCount)));
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<ApiResponse<CycleCountDto>>> UpdateLine(
        Guid id,
        Guid lineId,
        [FromBody] UpdateCycleCountLineRequest request,
        CancellationToken cancellationToken)
    {
        var cycleCount = await _context.CycleCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cycleCount == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Cycle count not found."]));
        }

        if (cycleCount.Status != CycleCountStatus.InProgress)
        {
            return BadRequest(ApiResponse<CycleCountDto>.Failure(["Lines can only be updated when cycle count is in progress."]));
        }

        var line = cycleCount.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line == null)
        {
            return NotFound(ApiResponse<CycleCountDto>.Failure(["Line not found."]));
        }

        if (request.CountedQuantity.HasValue)
        {
            line.SetCountedQuantity(request.CountedQuantity.Value);
        }

        if (request.Notes != null)
        {
            line.UpdateNotes(request.Notes);
        }

        _context.CycleCountLines.Update(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CycleCountDto>.Success(MapToDto(cycleCount)));
    }

    /// <summary>
    /// Auto-generate cycle count sheets from ABC classification and frequency.
    /// A-items → monthly, B-items → quarterly, C-items → annually. Optionally filter
    /// by a specific ABC class. Each generated sheet is a Draft containing one line
    /// per matching item in the warehouse, pre-populated with the system quantity.
    /// </summary>
    [HttpPost("schedule")]
    public async Task<ActionResult<ApiResponse<List<CycleCountDto>>>> Schedule(
        [FromBody] ScheduleCycleCountRequest request,
        CancellationToken cancellationToken)
    {
        const int Monthly = 1, Quarterly = 3, Annually = 12;
        int months = request.FrequencyMonths switch
        {
            >= 1 and <= 2 => Monthly,
            3 => Quarterly,
            >= 4 => Annually,
            _ => Monthly,
        };

        var query = _context.Items.Where(i => i.CompanyId == request.CompanyId && i.Status == ItemStatus.Active);
        if (request.WarehouseId.HasValue)
        {
            // Only include items that have stock in the warehouse.
            var stocked = _context.ItemStocks
                .Where(s => s.CompanyId == request.CompanyId && s.WarehouseId == request.WarehouseId.Value)
                .Select(s => s.ItemId);
            query = query.Where(i => stocked.Contains(i.Id));
        }

        if (!string.IsNullOrEmpty(request.AbcClass))
        {
            // "U" targets items that have not yet been ABC-classified (NULL).
            if (request.AbcClass == "U")
                query = query.Where(i => i.ABCClass == null || i.ABCClass == "U");
            else
                query = query.Where(i => i.ABCClass == request.AbcClass);
        }
        else
        {
            // Default: include items whose class matches the requested frequency.
            var targetClass = months switch
            {
                Monthly => "A",
                Quarterly => "B",
                _ => "C",
            };
            query = query.Where(i => i.ABCClass == targetClass);
        }

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync(cancellationToken);
        if (items.Count == 0)
            return Ok(ApiResponse<List<CycleCountDto>>.Success(new List<CycleCountDto>()));

        var created = new List<CycleCount>();
        var countDate = request.CountDate ?? DateTime.UtcNow.Date;

        // Group by ABC class so each class becomes its own count sheet.
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        foreach (var group in items.GroupBy(i => i.ABCClass ?? "U"))
        {
            var cc = new CycleCount(
                request.CompanyId,
                request.WarehouseId ?? Guid.Empty,
                $"CC-{group.Key}-{countDate:yyyyMMdd}-{stamp}-{created.Count + 1:00}",
                countDate,
                CycleCountStatus.Draft,
                $"Auto-scheduled {group.Key}-class cycle count (freq {months}mo).");

            foreach (var item in group)
            {
                var systemQty = await GetSystemQuantityAsync(item.Id, request.WarehouseId ?? Guid.Empty, null, null, null, cancellationToken);
                var line = new CycleCountLine(cc.Id, item.Id, null, systemQty, null, null, null, "scheduled");
                cc.AddLine(line);
            }

            _context.CycleCounts.Add(cc);
            created.Add(cc);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<List<CycleCountDto>>.Success(created.Select(MapToDto).ToList()));
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

    private CycleCountDto MapToDto(CycleCount cycleCount)
    {
        var dto = new CycleCountDto
        {
            Id = cycleCount.Id,
            CompanyId = cycleCount.CompanyId,
            WarehouseId = cycleCount.WarehouseId,
            CountNumber = cycleCount.CountNumber,
            CountDate = cycleCount.CountDate,
            Status = cycleCount.Status.ToString(),
            Notes = cycleCount.Notes,
            CreatedAt = cycleCount.CreatedOn,
            CreatedBy = cycleCount.CreatedBy,
        };
        dto.Lines.AddRange(cycleCount.Lines.Select(l => new CycleCountLineDto
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
public class CycleCountDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<CycleCountLineDto> Lines { get; } = new List<CycleCountLineDto>();
}
#pragma warning restore CA1002, CA2227

public class CycleCountLineDto
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
public class CreateCycleCountRequest
{
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateCycleCountLineRequest> Lines { get; } = new List<CreateCycleCountLineRequest>();
}

public class ScheduleCycleCountRequest
{
    public Guid CompanyId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int FrequencyMonths { get; set; } = 1;
    public string? AbcClass { get; set; }
    public DateTime? CountDate { get; set; }
}
#pragma warning restore CA1002

public class CreateCycleCountLineRequest
{
    public Guid ItemId { get; set; }
    public Guid? BinId { get; set; }
    public decimal? CountedQuantity { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCycleCountLineRequest
{
    public decimal? CountedQuantity { get; set; }
    public string? Notes { get; set; }
}

#pragma warning disable CA1002, CA2227
public class CycleCountPostResultDto
{
    public Guid CycleCountId { get; set; }
    public int TotalLines { get; set; }
    public int VariancesFound { get; set; }
    public int AdjustmentsCreated { get; set; }
    public List<CycleCountVarianceDto> Variances { get; } = new List<CycleCountVarianceDto>();
}
#pragma warning restore CA1002, CA2227

public class CycleCountVarianceDto
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