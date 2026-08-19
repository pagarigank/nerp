// <copyright file="ReorderSuggestionController.cs" company="ERP Project">
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
[Route("api/v1/inventory/reorder-suggestions")]
public class ReorderSuggestionController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderSuggestionController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReorderSuggestionDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] ReorderSuggestionStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.ReorderSuggestions
            .Include(s => s.Lines)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.SuggestionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.SuggestionDate <= endDate.Value);
        }

        var suggestions = await query
            .OrderByDescending(s => s.SuggestionDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = suggestions.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ReorderSuggestionDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReorderSuggestionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _context.ReorderSuggestions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (suggestion == null)
        {
            return NotFound(ApiResponse<ReorderSuggestionDto>.Failure(["Reorder suggestion not found."]));
        }

        return Ok(ApiResponse<ReorderSuggestionDto>.Success(MapToDto(suggestion)));
    }

    [HttpPost]
    [Authorize(Roles = "InventoryManager,Admin,PurchasingManager")]
    public async Task<ActionResult<ApiResponse<ReorderSuggestionDto>>> Generate(
        [FromBody] GenerateReorderSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var suggestion = new ReorderSuggestion(
            request.CompanyId,
            request.SuggestionNumber,
            request.SuggestionDate,
            request.Notes);

        // Get all items that need reordering
        var query = _context.ItemStocks
            .Where(s => s.CompanyId == request.CompanyId && s.OnHandQuantity > 0);

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

            if (!string.IsNullOrEmpty(request.ABCClass) && item.ABCClass != request.ABCClass)
            {
                continue;
            }

            decimal availableQty = stock.OnHandQuantity - stock.AllocatedQuantity;
            decimal reorderPoint = item.ReorderPoint ?? 0;
            decimal safetyStock = item.SafetyStock ?? 0;
            decimal leadTimeDays = item.LeadTimeDays ?? 0;

            // Calculate lead time demand (simplified - average daily usage * lead time)
            var recentIssues = await _context.InventoryTransactions
                .Where(t => t.ItemId == item.Id
                         && t.WarehouseId == stock.WarehouseId
                         && t.TransactionType == TransactionType.Issue
                         && t.TransactionDate >= DateTime.UtcNow.AddDays(-90))
                .SumAsync(t => t.Quantity, cancellationToken);

            decimal avgDailyUsage = recentIssues / 90;
            decimal leadTimeDemand = avgDailyUsage * leadTimeDays;

            // Check if reorder is needed
            if (availableQty <= reorderPoint || availableQty <= safetyStock)
            {
                // Calculate suggested order quantity
                decimal reorderQty = item.ReorderQuantity ?? 0;
                if (reorderQty == 0)
                {
                    // Use EOQ or safety stock + lead time demand - available
                    reorderQty = Math.Max(safetyStock + leadTimeDemand - availableQty, 1);
                }

                // Estimate stockout date
                decimal daysUntilStockout = availableQty > 0 && avgDailyUsage > 0
                    ? availableQty / avgDailyUsage
                    : 0;
                decimal estimatedStockoutDate = daysUntilStockout;

                // Get preferred vendor
                var vendorAssignment = await _context.ItemVendorAssignments
                    .FirstOrDefaultAsync(v => v.ItemId == item.Id && v.IsPrimaryVendor, cancellationToken);

                var line = new ReorderSuggestionLine(
                    suggestion.Id,
                    item.Id,
                    stock.WarehouseId,
                    stock.OnHandQuantity,
                    stock.AllocatedQuantity,
                    availableQty,
                    reorderPoint,
                    safetyStock,
                    leadTimeDemand,
                    reorderQty,
                    estimatedStockoutDate,
                    vendorAssignment?.VendorId.ToString(),
                    vendorAssignment?.VendorCost,
                    (int)leadTimeDays,
                    availableQty <= safetyStock ? "High" : "Normal");

                suggestion.AddLine(line);
            }
        }

        if (suggestion.Lines.Count == 0)
        {
            return BadRequest(ApiResponse<ReorderSuggestionDto>.Failure(["No items require reordering at this time."]));
        }

        _context.ReorderSuggestions.Add(suggestion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(suggestion);
        return CreatedAtAction(nameof(GetById), new { id = suggestion.Id }, ApiResponse<ReorderSuggestionDto>.Success(dto));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "InventoryManager,Admin,PurchasingManager")]
    public async Task<ActionResult<ApiResponse<ReorderSuggestionDto>>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _context.ReorderSuggestions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (suggestion == null)
        {
            return NotFound(ApiResponse<ReorderSuggestionDto>.Failure(["Reorder suggestion not found."]));
        }

        if (suggestion.Status != ReorderSuggestionStatus.Draft)
        {
            return BadRequest(ApiResponse<ReorderSuggestionDto>.Failure(["Only draft suggestions can be approved."]));
        }

        suggestion.UpdateStatus(ReorderSuggestionStatus.Approved);

        foreach (var line in suggestion.Lines)
        {
            line.UpdateStatus(ReorderSuggestionLineStatus.Approved);
        }

        _context.ReorderSuggestions.Update(suggestion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReorderSuggestionDto>.Success(MapToDto(suggestion)));
    }

    [HttpPost("{id:guid}/convert-to-po")]
    [Authorize(Roles = "InventoryManager,Admin,PurchasingManager")]
    public async Task<ActionResult<ApiResponse<ConvertToPoResultDto>>> ConvertToPO(
        Guid id,
        CancellationToken cancellationToken)
    {
        var suggestion = await _context.ReorderSuggestions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (suggestion == null)
        {
            return NotFound(ApiResponse<ConvertToPoResultDto>.Failure(["Reorder suggestion not found."]));
        }

        if (suggestion.Status != ReorderSuggestionStatus.Approved)
        {
            return BadRequest(ApiResponse<ConvertToPoResultDto>.Failure(["Only approved suggestions can be converted to POs."]));
        }

        var convertedLines = new List<ConvertToPoLineResultDto>();
        int poCount = 0;

        // Group lines by vendor
        var vendorGroups = suggestion.Lines
            .Where(l => l.Status == ReorderSuggestionLineStatus.Approved && !string.IsNullOrEmpty(l.VendorId))
            .GroupBy(l => l.VendorId);

        foreach (var group in vendorGroups)
        {
            // Here we would create a PO - for now just mark lines as converted
            foreach (var line in group)
            {
                line.UpdateStatus(ReorderSuggestionLineStatus.ConvertedToPO);
                convertedLines.Add(new ConvertToPoLineResultDto
                {
                    LineId = line.Id,
                    ItemId = line.ItemId,
                    SuggestedOrderQuantity = line.SuggestedOrderQuantity,
                    VendorId = group.Key!,
                });
            }

            poCount++;
        }

        suggestion.UpdateStatus(ReorderSuggestionStatus.ConvertedToPO);
        _context.ReorderSuggestions.Update(suggestion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new ConvertToPoResultDto
        {
            ReorderSuggestionId = suggestion.Id,
            PurchaseOrdersCreated = poCount,
            LinesConverted = convertedLines.Count,
        };
        result.Lines.AddRange(convertedLines);

        return Ok(ApiResponse<ConvertToPoResultDto>.Success(result));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "InventoryManager,Admin,PurchasingManager")]
    public async Task<ActionResult<ApiResponse<ReorderSuggestionDto>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _context.ReorderSuggestions.FindAsync(new object[] { id }, cancellationToken);

        if (suggestion == null)
        {
            return NotFound(ApiResponse<ReorderSuggestionDto>.Failure(["Reorder suggestion not found."]));
        }

        if (suggestion.Status == ReorderSuggestionStatus.ConvertedToPO)
        {
            return BadRequest(ApiResponse<ReorderSuggestionDto>.Failure(["Cannot cancel a suggestion that has been converted to PO."]));
        }

        suggestion.UpdateStatus(ReorderSuggestionStatus.Cancelled);
        _context.ReorderSuggestions.Update(suggestion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReorderSuggestionDto>.Success(MapToDto(suggestion)));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<ReorderDashboardDto>>> GetDashboard(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var stocks = await _context.ItemStocks
            .Where(s => s.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var itemIds = stocks.Select(s => s.ItemId).Distinct().ToList();
        var items = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        int itemsNeedingReorder = 0;
        int criticalItems = 0;

        foreach (var stock in stocks)
        {
            var item = items.GetValueOrDefault(stock.ItemId);
            if (item == null)
            {
                continue;
            }

            decimal availableQty = stock.OnHandQuantity - stock.AllocatedQuantity;
            decimal reorderPoint = item.ReorderPoint ?? 0;
            decimal safetyStock = item.SafetyStock ?? 0;

            if (availableQty <= reorderPoint || availableQty <= safetyStock)
            {
                itemsNeedingReorder++;
            }

            if (availableQty <= safetyStock)
            {
                criticalItems++;
            }
        }

        var pendingSuggestions = await _context.ReorderSuggestions
            .Where(s => s.CompanyId == companyId && s.Status == ReorderSuggestionStatus.Draft)
            .CountAsync(cancellationToken);

        var approvedSuggestions = await _context.ReorderSuggestions
            .Where(s => s.CompanyId == companyId && s.Status == ReorderSuggestionStatus.Approved)
            .CountAsync(cancellationToken);

        return Ok(ApiResponse<ReorderDashboardDto>.Success(new ReorderDashboardDto
        {
            CompanyId = companyId,
            ItemsNeedingReorder = itemsNeedingReorder,
            CriticalItems = criticalItems,
            PendingSuggestions = pendingSuggestions,
            ApprovedSuggestions = approvedSuggestions,
            GeneratedAt = DateTime.UtcNow,
        }));
    }

    private ReorderSuggestionDto MapToDto(ReorderSuggestion suggestion)
    {
        var dto = new ReorderSuggestionDto
        {
            Id = suggestion.Id,
            CompanyId = suggestion.CompanyId,
            SuggestionNumber = suggestion.SuggestionNumber,
            SuggestionDate = suggestion.SuggestionDate,
            Notes = suggestion.Notes,
            Status = suggestion.Status.ToString(),
            CreatedAt = suggestion.CreatedOn,
            CreatedBy = suggestion.CreatedBy,
        };
        dto.Lines.AddRange(suggestion.Lines.Select(l => new ReorderSuggestionLineDto
        {
            Id = l.Id,
            ItemId = l.ItemId,
            WarehouseId = l.WarehouseId,
            CurrentOnHand = l.CurrentOnHand,
            CurrentAllocated = l.CurrentAllocated,
            AvailableQuantity = l.AvailableQuantity,
            ReorderPoint = l.ReorderPoint,
            SafetyStock = l.SafetyStock,
            LeadTimeDemand = l.LeadTimeDemand,
            SuggestedOrderQuantity = l.SuggestedOrderQuantity,
            EstimatedStockoutDate = l.EstimatedStockoutDate,
            VendorId = l.VendorId,
            VendorCost = l.VendorCost,
            LeadTimeDays = l.LeadTimeDays,
            Priority = l.Priority,
            Status = l.Status.ToString(),
        }));
        return dto;
    }
}

#pragma warning disable CA1002, CA2227
public class ReorderSuggestionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string SuggestionNumber { get; set; } = string.Empty;
    public DateTime SuggestionDate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<ReorderSuggestionLineDto> Lines { get; } = new List<ReorderSuggestionLineDto>();
}
#pragma warning restore CA1002, CA2227

public class ReorderSuggestionLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal CurrentOnHand { get; set; }
    public decimal CurrentAllocated { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal LeadTimeDemand { get; set; }
    public decimal SuggestedOrderQuantity { get; set; }
    public decimal EstimatedStockoutDate { get; set; }
    public string? VendorId { get; set; }
    public decimal? VendorCost { get; set; }
    public int? LeadTimeDays { get; set; }
    public string? Priority { get; set; }
    public string Status { get; set; } = string.Empty;
}

#pragma warning disable CA1002
public class GenerateReorderSuggestionRequest
{
    public Guid CompanyId { get; set; }
    public string SuggestionNumber { get; set; } = string.Empty;
    public DateTime SuggestionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<Guid> WarehouseIds { get; } = new List<Guid>();
    public string? ABCClass { get; set; }
}
#pragma warning restore CA1002

#pragma warning disable CA1002, CA2227
public class ConvertToPoResultDto
{
    public Guid ReorderSuggestionId { get; set; }
    public int PurchaseOrdersCreated { get; set; }
    public int LinesConverted { get; set; }
    public List<ConvertToPoLineResultDto> Lines { get; } = new List<ConvertToPoLineResultDto>();
}
#pragma warning restore CA1002, CA2227

public class ConvertToPoLineResultDto
{
    public Guid LineId { get; set; }
    public Guid ItemId { get; set; }
    public decimal SuggestedOrderQuantity { get; set; }
    public string VendorId { get; set; } = string.Empty;
}

public class ReorderDashboardDto
{
    public Guid CompanyId { get; set; }
    public int ItemsNeedingReorder { get; set; }
    public int CriticalItems { get; set; }
    public int PendingSuggestions { get; set; }
    public int ApprovedSuggestions { get; set; }
    public DateTime GeneratedAt { get; set; }
}