// <copyright file="FieldServiceReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

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
