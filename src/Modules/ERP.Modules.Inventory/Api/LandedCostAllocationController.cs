// <copyright file="LandedCostAllocationController.cs" company="ERP Project">
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
[Route("api/v1/inventory/landed-cost-allocations")]
public class LandedCostAllocationController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public LandedCostAllocationController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LandedCostAllocationDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? receiptTransactionId,
        [FromQuery] LandedCostAllocationStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.LandedCostAllocations
            .Include(a => a.Lines)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(a => a.CompanyId == companyId.Value);
        }

        if (receiptTransactionId.HasValue)
        {
            query = query.Where(a => a.ReceiptTransactionId == receiptTransactionId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.AllocationDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.AllocationDate <= endDate.Value);
        }

        var allocations = await query
            .OrderByDescending(a => a.AllocationDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = allocations.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<LandedCostAllocationDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LandedCostAllocationDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var allocation = await _context.LandedCostAllocations
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (allocation == null)
        {
            return NotFound(ApiResponse<LandedCostAllocationDto>.Failure(["Landed cost allocation not found."]));
        }

        return Ok(ApiResponse<LandedCostAllocationDto>.Success(MapToDto(allocation)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LandedCostAllocationDto>>> Create(
        [FromBody] CreateLandedCostAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var receiptTransaction = await _context.InventoryTransactions.FindAsync(new object[] { request.ReceiptTransactionId }, cancellationToken);
        if (receiptTransaction == null)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure([$"Receipt transaction {request.ReceiptTransactionId} not found"]));
        }

        if (receiptTransaction.TransactionType != TransactionType.Receipt)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["Only receipt transactions can have landed cost allocations."]));
        }

        var allocation = new LandedCostAllocation(
            request.CompanyId,
            request.ReceiptTransactionId,
            request.AllocationNumber,
            request.AllocationDate,
            request.Notes);

        if (request.Lines != null)
        {
            foreach (var line in request.Lines)
            {
                var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);
                if (item == null)
                {
                    return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure([$"Item {line.ItemId} not found"]));
                }

                var allocationLine = new LandedCostAllocationLine(
                    allocation.Id,
                    line.ItemId,
                    line.QuantityReceived,
                    line.UnitCost,
                    line.AllocationMethod,
                    line.AllocatedAmount,
                    line.LandedCostId,
                    line.Description);

                allocation.AddLine(allocationLine);
            }
        }

        _context.LandedCostAllocations.Add(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(allocation);
        return CreatedAtAction(nameof(GetById), new { id = allocation.Id }, ApiResponse<LandedCostAllocationDto>.Success(dto));
    }

    [HttpPost("{id:guid}/auto-allocate")]
    public async Task<ActionResult<ApiResponse<LandedCostAllocationDto>>> AutoAllocate(
        Guid id,
        [FromBody] AutoAllocateRequest request,
        CancellationToken cancellationToken)
    {
        var allocation = await _context.LandedCostAllocations
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (allocation == null)
        {
            return NotFound(ApiResponse<LandedCostAllocationDto>.Failure(["Landed cost allocation not found."]));
        }

        if (allocation.Status != LandedCostAllocationStatus.Draft)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["Only draft allocations can be auto-allocated."]));
        }

        // Get all items from the receipt transaction
        var receiptItems = await _context.InventoryTransactions
            .Where(t => t.Id == allocation.ReceiptTransactionId && t.TransactionType == TransactionType.Receipt && t.Quantity > 0)
            .ToListAsync(cancellationToken);

        if (receiptItems.Count == 0)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["No receipt items found for this transaction."]));
        }

        // Get available landed costs to allocate (RemainingAmount is a computed,
        // unmapped member, so filter it client-side after materialization).
        var availableLandedCosts = (await _context.LandedCosts
            .Where(lc => lc.CompanyId == allocation.CompanyId
                      && lc.Status != LandedCostStatus.Cancelled)
            .ToListAsync(cancellationToken))
            .Where(lc => lc.RemainingAmount > 0)
            .ToList();

        if (availableLandedCosts.Count == 0)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["No available landed costs to allocate."]));
        }

        decimal totalLandedCost = availableLandedCosts.Sum(lc => lc.RemainingAmount);

        // Calculate allocation based on method
        switch (request.AllocationMethod)
        {
            case LandedCostAllocationMethod.ByQuantity:
                AllocateByQuantity(allocation, receiptItems, availableLandedCosts, totalLandedCost);
                break;
            case LandedCostAllocationMethod.ByValue:
                AllocateByValue(allocation, receiptItems, availableLandedCosts, totalLandedCost);
                break;
            case LandedCostAllocationMethod.ByWeight:
            case LandedCostAllocationMethod.ByVolume:
                // For now, fall back to quantity
                AllocateByQuantity(allocation, receiptItems, availableLandedCosts, totalLandedCost);
                break;
            case LandedCostAllocationMethod.Manual:
                return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["Manual allocation not supported for auto-allocate. Use manual line entry."]));
            default:
                AllocateByQuantity(allocation, receiptItems, availableLandedCosts, totalLandedCost);
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<LandedCostAllocationDto>.Success(MapToDto(allocation)));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<LandedCostAllocationDto>>> Post(Guid id, CancellationToken cancellationToken)
    {
        var allocation = await _context.LandedCostAllocations
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (allocation == null)
        {
            return NotFound(ApiResponse<LandedCostAllocationDto>.Failure(["Landed cost allocation not found."]));
        }

        if (allocation.Status != LandedCostAllocationStatus.Draft)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["Only draft allocations can be posted."]));
        }

        if (allocation.Lines.Count == 0)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["Allocation has no lines to post."]));
        }

        // Get the receipt transaction to get warehouse
        var receiptTransaction = await _context.InventoryTransactions.FindAsync(new object[] { allocation.ReceiptTransactionId }, cancellationToken);

        // Create inventory transactions to adjust item costs
        foreach (var line in allocation.Lines)
        {
            var item = await _context.Items.FindAsync(new object[] { line.ItemId }, cancellationToken);

            var costAdjustment = new InventoryTransaction(
                allocation.CompanyId,
                line.ItemId,
                receiptTransaction!.WarehouseId,
                TransactionType.Adjustment,
                0, // Quantity is 0 for cost-only adjustments
                item?.BaseUnitOfMeasure ?? "EA",
                line.AllocatedAmount / (line.QuantityReceived == 0 ? 1 : line.QuantityReceived),
                DateTime.UtcNow,
                null,
                null,
                null,
                $"LC-{allocation.AllocationNumber}",
                null,
                $"Landed cost allocation. Allocation date: {allocation.AllocationDate:yyyy-MM-dd}. {line.Description}");

            _context.InventoryTransactions.Add(costAdjustment);
        }

        allocation.UpdateStatus(LandedCostAllocationStatus.Posted);
        _context.LandedCostAllocations.Update(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<LandedCostAllocationDto>.Success(MapToDto(allocation)));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<LandedCostAllocationDto>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var allocation = await _context.LandedCostAllocations.FindAsync(new object[] { id }, cancellationToken);

        if (allocation == null)
        {
            return NotFound(ApiResponse<LandedCostAllocationDto>.Failure(["Landed cost allocation not found."]));
        }

        if (allocation.Status == LandedCostAllocationStatus.Posted)
        {
            return BadRequest(ApiResponse<LandedCostAllocationDto>.Failure(["Cannot cancel a posted allocation."]));
        }

        allocation.UpdateStatus(LandedCostAllocationStatus.Cancelled);
        _context.LandedCostAllocations.Update(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<LandedCostAllocationDto>.Success(MapToDto(allocation)));
    }

    private static void AllocateByQuantity(
        LandedCostAllocation allocation,
        List<InventoryTransaction> receiptItems,
        List<LandedCost> availableLandedCosts,
        decimal totalLandedCost)
    {
        decimal totalQuantity = receiptItems.Sum(r => r.Quantity);

        foreach (var receiptItem in receiptItems)
        {
            var proportion = receiptItem.Quantity / totalQuantity;
            var allocatedAmount = totalLandedCost * proportion;

            // Distribute among available landed costs
            decimal remainingToAllocate = allocatedAmount;
            foreach (var landedCost in availableLandedCosts.OrderBy(lc => lc.CostCode))
            {
                if (remainingToAllocate <= 0)
                {
                    break;
                }

                var amountFromThisCost = Math.Min(landedCost.RemainingAmount, remainingToAllocate);
                if (amountFromThisCost > 0)
                {
                    var allocationLine = new LandedCostAllocationLine(
                        allocation.Id,
                        receiptItem.ItemId,
                        receiptItem.Quantity,
                        receiptItem.UnitCost,
                        LandedCostAllocationMethod.ByQuantity,
                        amountFromThisCost,
                        landedCost.Id,
                        $"Auto-allocated by quantity from {landedCost.CostCode}");

                    allocation.AddLine(allocationLine);
                    landedCost.AddAllocatedAmount(amountFromThisCost);
                    remainingToAllocate -= amountFromThisCost;
                }
            }
        }

        // Update landed cost statuses
        foreach (var landedCost in availableLandedCosts)
        {
            if (landedCost.RemainingAmount <= 0.0001m)
            {
                landedCost.UpdateStatus(LandedCostStatus.FullyAllocated);
            }
            else if (landedCost.AllocatedAmount > 0)
            {
                landedCost.UpdateStatus(LandedCostStatus.PartiallyAllocated);
            }
        }
    }

    private static void AllocateByValue(
        LandedCostAllocation allocation,
        List<InventoryTransaction> receiptItems,
        List<LandedCost> availableLandedCosts,
        decimal totalLandedCost)
    {
        decimal totalValue = receiptItems.Sum(r => r.ExtendedCost);

        foreach (var receiptItem in receiptItems)
        {
            var proportion = receiptItem.ExtendedCost / totalValue;
            var allocatedAmount = totalLandedCost * proportion;

            decimal remainingToAllocate = allocatedAmount;
            foreach (var landedCost in availableLandedCosts.OrderBy(lc => lc.CostCode))
            {
                if (remainingToAllocate <= 0)
                {
                    break;
                }

                var amountFromThisCost = Math.Min(landedCost.RemainingAmount, remainingToAllocate);
                if (amountFromThisCost > 0)
                {
                    var allocationLine = new LandedCostAllocationLine(
                        allocation.Id,
                        receiptItem.ItemId,
                        receiptItem.Quantity,
                        receiptItem.UnitCost,
                        LandedCostAllocationMethod.ByValue,
                        amountFromThisCost,
                        landedCost.Id,
                        $"Auto-allocated by value from {landedCost.CostCode}");

                    allocation.AddLine(allocationLine);
                    landedCost.AddAllocatedAmount(amountFromThisCost);
                    remainingToAllocate -= amountFromThisCost;
                }
            }
        }

        foreach (var landedCost in availableLandedCosts)
        {
            if (landedCost.RemainingAmount <= 0.0001m)
            {
                landedCost.UpdateStatus(LandedCostStatus.FullyAllocated);
            }
            else if (landedCost.AllocatedAmount > 0)
            {
                landedCost.UpdateStatus(LandedCostStatus.PartiallyAllocated);
            }
        }
    }

    private LandedCostAllocationDto MapToDto(LandedCostAllocation allocation)
    {
        var dto = new LandedCostAllocationDto
        {
            Id = allocation.Id,
            CompanyId = allocation.CompanyId,
            ReceiptTransactionId = allocation.ReceiptTransactionId,
            AllocationNumber = allocation.AllocationNumber,
            AllocationDate = allocation.AllocationDate,
            Status = allocation.Status.ToString(),
            Notes = allocation.Notes,
            TotalAllocatedCost = allocation.TotalAllocatedCost,
            CreatedAt = allocation.CreatedOn,
            CreatedBy = allocation.CreatedBy,
        };
        dto.Lines.AddRange(allocation.Lines.Select(l => new LandedCostAllocationLineDto
        {
            Id = l.Id,
            ItemId = l.ItemId,
            QuantityReceived = l.QuantityReceived,
            UnitCost = l.UnitCost,
            AllocationMethod = l.AllocationMethod.ToString(),
            AllocatedAmount = l.AllocatedAmount,
            Description = l.Description,
        }));
        return dto;
    }
}

#pragma warning disable CA1002, CA2227
public class LandedCostAllocationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ReceiptTransactionId { get; set; }
    public string AllocationNumber { get; set; } = string.Empty;
    public DateTime AllocationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TotalAllocatedCost { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<LandedCostAllocationLineDto> Lines { get; } = new List<LandedCostAllocationLineDto>();
}
#pragma warning restore CA1002, CA2227

public class LandedCostAllocationLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public string AllocationMethod { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public string? Description { get; set; }
}

#pragma warning disable CA1002
public class CreateLandedCostAllocationRequest
{
    public Guid CompanyId { get; set; }
    public Guid ReceiptTransactionId { get; set; }
    public string AllocationNumber { get; set; } = string.Empty;
    public DateTime AllocationDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateLandedCostAllocationLineRequest> Lines { get; } = new List<CreateLandedCostAllocationLineRequest>();
}
#pragma warning restore CA1002

public class CreateLandedCostAllocationLineRequest
{
    public Guid ItemId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public LandedCostAllocationMethod AllocationMethod { get; set; }
    public decimal AllocatedAmount { get; set; }
    public Guid? LandedCostId { get; set; }
    public string? Description { get; set; }
}

public class AutoAllocateRequest
{
    public LandedCostAllocationMethod AllocationMethod { get; set; } = LandedCostAllocationMethod.ByQuantity;
}