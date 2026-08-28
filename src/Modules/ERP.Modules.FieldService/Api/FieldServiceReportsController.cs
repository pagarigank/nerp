// <copyright file="FieldServiceReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1408
#pragma warning disable SA1513

using ERP.Modules.FieldService.Domain.Entities;
using ERP.Modules.FieldService.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.FieldService.Api;

[ApiController]
[Route("api/v1/field-service/reports")]
public class FieldServiceReportsController : ControllerBase
{
    private readonly FieldServiceDbContext _context;

    public FieldServiceReportsController(FieldServiceDbContext context)
    {
        _context = context;
    }

    // --- SLA Compliance ---
    [HttpGet("sla-compliance")]
    public async Task<ActionResult<ApiResponse<List<SlaComplianceRow>>>> SlaCompliance(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders
            .Where(w => w.CompanyId == companyId)
            .GroupBy(w => w.Priority)
            .Select(g => new SlaComplianceRow
            {
                Priority = g.Key.ToString(),
                Total = g.Count(),
                Completed = g.Count(w => w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed),
                Breached = g.Count(w => w.ResolutionDue.HasValue && w.ClockOut.HasValue && w.ClockOut.Value > w.ResolutionDue.Value),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<SlaComplianceRow>>.Success(rows));
    }

    // --- Technician Utilization ---
    [HttpGet("technician-utilization")]
    public async Task<ActionResult<ApiResponse<List<TechnicianUtilizationRow>>>> TechnicianUtilization(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders
            .Where(w => w.CompanyId == companyId && w.TechnicianId.HasValue)
            .GroupBy(w => w.TechnicianId!.Value)
            .Select(g => new TechnicianUtilizationRow
            {
                TechnicianId = g.Key,
                WorkOrders = g.Count(),
                LaborHours = g.Sum(w => w.LaborHours),
                BillableTotal = g.Sum(w => w.BillableTotal),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TechnicianUtilizationRow>>.Success(rows));
    }

    // --- Open Work Order Aging ---
    [HttpGet("open-aging")]
    public async Task<ActionResult<ApiResponse<List<OpenWoAgingRow>>>> OpenAging(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.WorkOrders
            .Where(w => w.CompanyId == companyId && w.Status != WorkOrderStatus.Closed && w.Status != WorkOrderStatus.Cancelled)
            .Select(w => new OpenWoAgingRow
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                Status = w.Status.ToString(),
                ScheduledStart = w.ScheduledStart,
                AgeDays = w.ScheduledStart.HasValue ? (int)(now - w.ScheduledStart.Value).TotalDays : 0,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<OpenWoAgingRow>>.Success(rows));
    }

    // --- Contract Status ---
    [HttpGet("contract-status")]
    public async Task<ActionResult<ApiResponse<List<ContractStatusRow>>>> ContractStatus(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.ServiceContracts
            .Where(c => c.CompanyId == companyId)
            .Select(c => new ContractStatusRow
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Name = c.Name,
                Status = c.Status.ToString(),
                EndDate = c.EndDate,
                IsExpired = c.EndDate < now,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ContractStatusRow>>.Success(rows));
    }

    // --- Preventive Maintenance Due ---
    [HttpGet("pm-due")]
    public async Task<ActionResult<ApiResponse<List<PmDueRow>>>> PmDue(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.PreventiveMaintenances
            .Where(p => p.CompanyId == companyId && p.IsActive && p.NextDue <= now)
            .Select(p => new PmDueRow
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                EquipmentAssetId = p.EquipmentAssetId,
                NextDue = p.NextDue,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PmDueRow>>.Success(rows));
    }

    [HttpGet("first-time-fix")]
    public async Task<ActionResult<ApiResponse<FirstTimeFixRow>>> FirstTimeFix([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var total = await _context.WorkOrders.CountAsync(w => w.CompanyId == companyId && w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed, cancellationToken);
        var withFollowUp = await _context.WorkOrders.CountAsync(w => w.CompanyId == companyId && _context.WorkOrders.Any(f => f.WorkOrderNumber == w.WorkOrderNumber + "-FU"), cancellationToken);
        var rate = total == 0 ? 0 : Math.Round((decimal)(total - withFollowUp) / total * 100, 2);
        return Ok(ApiResponse<FirstTimeFixRow>.Success(new FirstTimeFixRow { TotalCompleted = total, FirstTimeFixed = total - withFollowUp, RatePercent = rate }));
    }

    [HttpGet("revenue-profitability")]
    public async Task<ActionResult<ApiResponse<List<RevenueProfitabilityRow>>>> RevenueProfitability([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders.Where(w => w.CompanyId == companyId && (w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed)).GroupBy(w => w.TechnicianId).Select(g => new RevenueProfitabilityRow { TechnicianId = g.Key, Revenue = g.Sum(w => w.BillableTotal), Cost = g.Sum(w => w.LaborCost + w.PartsCost + w.TravelCost), Profit = g.Sum(w => w.BillableTotal - (w.LaborCost + w.PartsCost + w.TravelCost)), WorkOrders = g.Count() }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<RevenueProfitabilityRow>>.Success(rows));
    }

    [HttpGet("warranty-expiration")]
    public async Task<ActionResult<ApiResponse<List<WarrantyExpirationRow>>>> WarrantyExpiration([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(90);
        var rows = await _context.EquipmentAssets.Where(e => e.CompanyId == companyId && e.UnderWarranty && e.WarrantyEnd.HasValue && e.WarrantyEnd <= cutoff).Select(e => new WarrantyExpirationRow { Id = e.Id, AssetTag = e.AssetTag, WarrantyEnd = e.WarrantyEnd!.Value, DaysRemaining = (int)(e.WarrantyEnd!.Value - DateTime.UtcNow).TotalDays }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WarrantyExpirationRow>>.Success(rows));
    }

    [HttpGet("parts-usage")]
    public async Task<ActionResult<ApiResponse<List<PartsUsageRow>>>> PartsUsage([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrderLines.Where(l => l.LineType == WorkOrderLineType.Part && _context.WorkOrders.Any(w => w.Id == l.WorkOrderId && w.CompanyId == companyId)).GroupBy(l => l.ItemId).Select(g => new PartsUsageRow { ItemId = g.Key, TotalQuantity = g.Sum(l => l.Quantity), TotalCost = g.Sum(l => l.CostAmount), UsageCount = g.Count() }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PartsUsageRow>>.Success(rows));
    }

    [HttpGet("travel-expense")]
    public async Task<ActionResult<ApiResponse<List<TravelExpenseRow>>>> TravelExpense([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders.Where(w => w.CompanyId == companyId).Select(w => new TravelExpenseRow { WorkOrderId = w.Id, WorkOrderNumber = w.WorkOrderNumber, TravelCost = w.TravelCost, ExpenseTotal = _context.WorkOrderLines.Where(l => l.WorkOrderId == w.Id && l.LineType == WorkOrderLineType.Expense).Sum(l => l.LineTotal), BillableTotal = w.BillableTotal }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TravelExpenseRow>>.Success(rows));
    }

    [HttpGet("work-order-status")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderStatusRow>>>> WorkOrderStatusReport([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders.Where(w => w.CompanyId == companyId).GroupBy(w => w.Status).Select(g => new WorkOrderStatusRow { Status = g.Key.ToString(), Count = g.Count(), TotalBillable = g.Sum(w => w.BillableTotal) }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WorkOrderStatusRow>>.Success(rows));
    }
}

public record SlaComplianceRow
{
    public string Priority { get; init; } = string.Empty;
    public int Total { get; init; }
    public int Completed { get; init; }
    public int Breached { get; init; }
}

public record TechnicianUtilizationRow
{
    public Guid TechnicianId { get; init; }
    public int WorkOrders { get; init; }
    public decimal LaborHours { get; init; }
    public decimal BillableTotal { get; init; }
}

public record OpenWoAgingRow
{
    public Guid Id { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? ScheduledStart { get; init; }
    public int AgeDays { get; init; }
}

public record ContractStatusRow
{
    public Guid Id { get; init; }
    public string ContractNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime EndDate { get; init; }
    public bool IsExpired { get; init; }
}

public record PmDueRow
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? EquipmentAssetId { get; init; }
    public DateTime? NextDue { get; init; }
}
public record FirstTimeFixRow { public int TotalCompleted { get; init; } public int FirstTimeFixed { get; init; } public decimal RatePercent { get; init; } }
public record RevenueProfitabilityRow { public Guid? TechnicianId { get; init; } public decimal Revenue { get; init; } public decimal Cost { get; init; } public decimal Profit { get; init; } public int WorkOrders { get; init; } }
public record WarrantyExpirationRow { public Guid Id { get; init; } public string AssetTag { get; init; } = string.Empty; public DateTime WarrantyEnd { get; init; } public int DaysRemaining { get; init; } }
public record PartsUsageRow { public Guid? ItemId { get; init; } public decimal TotalQuantity { get; init; } public decimal TotalCost { get; init; } public int UsageCount { get; init; } }
public record TravelExpenseRow { public Guid WorkOrderId { get; init; } public string WorkOrderNumber { get; init; } = string.Empty; public decimal TravelCost { get; init; } public decimal ExpenseTotal { get; init; } public decimal BillableTotal { get; init; } }
public record WorkOrderStatusRow { public string Status { get; init; } = string.Empty; public int Count { get; init; } public decimal TotalBillable { get; init; } }
