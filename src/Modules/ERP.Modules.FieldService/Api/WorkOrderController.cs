// <copyright file="WorkOrderController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1501
#pragma warning disable SA1513
#pragma warning disable CA1002

using ERP.Modules.FieldService.Application;
using ERP.Modules.FieldService.Domain.Entities;
using ERP.Modules.FieldService.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.FieldService.Api;

[ApiController]
[Route("api/v1/field-service")]
public class WorkOrderController : ControllerBase
{
    private readonly FieldServiceDbContext _context;
    private readonly IFieldServiceIntegration _integration;

    public WorkOrderController(FieldServiceDbContext context, IFieldServiceIntegration integration)
    {
        _context = context;
        _integration = integration;
    }

    // --- Service call intake ---
    [HttpPost("service-calls")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateServiceCall(
        [FromBody] CreateServiceCallRequest request, CancellationToken cancellationToken)
    {
        var entity = new ServiceCall(
            request.CompanyId,
            request.CallNumber,
            request.CustomerId,
            request.ContactName,
            request.ContactPhone,
            request.EquipmentAssetId,
            request.ServiceContractId,
            request.Priority,
            request.Description,
            request.ResponseMinutes);
        _context.ServiceCalls.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("service-calls")]
    public async Task<ActionResult<ApiResponse<List<ServiceCallDto>>>> GetServiceCalls(CancellationToken cancellationToken)
    {
        var list = await _context.ServiceCalls
            .Select(c => new ServiceCallDto
            {
                Id = c.Id,
                CallNumber = c.CallNumber,
                CustomerId = c.CustomerId,
                Priority = c.Priority.ToString(),
                Status = c.Status.ToString(),
                LoggedOn = c.LoggedOn,
                ResponseDue = c.ResponseDue,
                WorkOrderId = c.WorkOrderId,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ServiceCallDto>>.Success(list));
    }

    // --- Work order creation (from service call or standalone) ---
    [HttpPost("work-orders")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateWorkOrder(
        [FromBody] CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = new WorkOrder(
            request.CompanyId,
            request.WorkOrderNumber,
            request.ServiceCallId,
            request.EstimateId,
            request.ServiceContractId,
            request.EquipmentAssetId,
            request.CustomerId,
            request.PreventiveMaintenanceId,
            request.Type,
            request.Priority,
            request.TechnicianId,
            request.TerritoryId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.ResponseDue,
            request.ResolutionDue,
            request.WarrantyCovered,
            request.Notes);
        _context.WorkOrders.Add(entity);

        if (request.ServiceCallId.HasValue)
        {
            var call = await _context.ServiceCalls
                .FirstOrDefaultAsync(c => c.Id == request.ServiceCallId.Value && c.CompanyId == request.CompanyId, cancellationToken);
            call?.LinkWorkOrder(entity.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("work-orders")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderDto>>>> GetWorkOrders(CancellationToken cancellationToken)
    {
        var list = await _context.WorkOrders
            .Select(w => new WorkOrderDto
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                Type = w.Type.ToString(),
                Status = w.Status.ToString(),
                Priority = w.Priority.ToString(),
                TechnicianId = w.TechnicianId,
                ScheduledStart = w.ScheduledStart,
                BillableTotal = w.BillableTotal,
                BilledToAr = w.BilledToAr,
                WarrantyCovered = w.WarrantyCovered,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WorkOrderDto>>.Success(list));
    }

    [HttpGet("work-orders/{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> GetWorkOrder(
        Guid id, [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .Include(w => w.Lines)
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<WorkOrderDetailDto>.Failure(new[] { "Work order not found." }));
        }

        var dto = new WorkOrderDetailDto
        {
            Id = wo.Id,
            WorkOrderNumber = wo.WorkOrderNumber,
            Type = wo.Type.ToString(),
            Status = wo.Status.ToString(),
            Priority = wo.Priority.ToString(),
            TechnicianId = wo.TechnicianId,
            TerritoryId = wo.TerritoryId,
            ScheduledStart = wo.ScheduledStart,
            ScheduledEnd = wo.ScheduledEnd,
            ClockIn = wo.ClockIn,
            ClockOut = wo.ClockOut,
            LaborHours = wo.LaborHours,
            LaborCost = wo.LaborCost,
            PartsCost = wo.PartsCost,
            TravelCost = wo.TravelCost,
            Fees = wo.Fees,
            BillableTotal = wo.BillableTotal,
            WarrantyCovered = wo.WarrantyCovered,
            BilledToAr = wo.BilledToAr,
            Resolution = wo.Resolution,
            Lines = wo.Lines.Select(l => new WorkOrderLineDto
            {
                Id = l.Id,
                LineType = l.LineType.ToString(),
                Description = l.Description,
                Quantity = l.Quantity,
                UnitRate = l.UnitRate,
                LineTotal = l.LineTotal,
                Billable = l.Billable,
                ItemId = l.ItemId,
            }).ToList(),
        };
        return Ok(ApiResponse<WorkOrderDetailDto>.Success(dto));
    }

    // --- Dispatch ---
    [HttpPost("work-orders/{id:guid}/dispatch")]
    public async Task<ActionResult<ApiResponse<bool>>> Dispatch(
        Guid id, [FromBody] DispatchRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        wo.Dispatch(request.TechnicianId, request.TerritoryId);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpGet("dispatch/suggestions")]
    public async Task<ActionResult<ApiResponse<List<DispatchSuggestionDto>>>> DispatchSuggestions(
        [FromQuery] Guid companyId, [FromQuery] Guid workOrderId, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId && w.CompanyId == companyId, cancellationToken);
        if (wo is null) return NotFound(ApiResponse<List<DispatchSuggestionDto>>.Failure(new[] { "Work order not found." }));
        var techs = await _context.Technicians.Where(t => t.CompanyId == companyId && t.Status == TechnicianStatus.Active).ToListAsync(cancellationToken);
        var skills = await _context.TechnicianSkills.Where(s => s.CompanyId == companyId && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow)).ToListAsync(cancellationToken);
        var busyIds = await _context.WorkOrders.Where(w => w.CompanyId == companyId && w.TechnicianId.HasValue && w.Status != WorkOrderStatus.Closed && w.Status != WorkOrderStatus.Cancelled && w.Status != WorkOrderStatus.Completed && w.ScheduledStart.HasValue && w.ScheduledEnd.HasValue && wo.ScheduledStart.HasValue && wo.ScheduledEnd.HasValue && w.ScheduledStart < wo.ScheduledEnd && w.ScheduledEnd > wo.ScheduledStart).Select(w => w.TechnicianId!.Value).ToListAsync(cancellationToken);
        var list = techs.Select(t =>
        {
            var techSkills = skills.Where(s => s.TechnicianId == t.Id).ToList();
            var score = 0;
            if (t.DefaultTerritoryId.HasValue && wo.TerritoryId.HasValue && t.DefaultTerritoryId == wo.TerritoryId) score += 10;
            score += techSkills.Count * 2;
            score += techSkills.Sum(s => s.Proficiency);
            var available = !busyIds.Contains(t.Id);
            if (!available) score -= 50;
            return new DispatchSuggestionDto { TechnicianId = t.Id, Code = t.Code, Name = $"{t.FirstName} {t.LastName}", TerritoryId = t.DefaultTerritoryId, SkillCount = techSkills.Count, TotalProficiency = techSkills.Sum(s => s.Proficiency), Available = available, Score = score };
        }).OrderByDescending(x => x.Score).ToList();
        return Ok(ApiResponse<List<DispatchSuggestionDto>>.Success(list));
    }

    [HttpGet("technicians/{technicianId:guid}/availability")]
    public async Task<ActionResult<ApiResponse<TechnicianAvailabilityDto>>> TechnicianAvailability(Guid technicianId, [FromQuery] Guid companyId, [FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken cancellationToken)
    {
        var conflicts = await _context.WorkOrders.Where(w => w.CompanyId == companyId && w.TechnicianId == technicianId && w.Status != WorkOrderStatus.Closed && w.Status != WorkOrderStatus.Cancelled && w.ScheduledStart.HasValue && w.ScheduledEnd.HasValue && w.ScheduledStart < end && w.ScheduledEnd > start).Select(w => new AvailabilityConflictDto { WorkOrderId = w.Id, WorkOrderNumber = w.WorkOrderNumber, ScheduledStart = w.ScheduledStart!.Value, ScheduledEnd = w.ScheduledEnd!.Value, Status = w.Status.ToString() }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<TechnicianAvailabilityDto>.Success(new TechnicianAvailabilityDto { TechnicianId = technicianId, Available = conflicts.Count == 0, Conflicts = conflicts }));
    }

    [HttpPost("work-orders/{id:guid}/follow-up")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateFollowUp(Guid id, [FromBody] CreateFollowUpRequest request, CancellationToken cancellationToken)
    {
        var source = await _context.WorkOrders.FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (source is null) return NotFound(ApiResponse<Guid>.Failure(new[] { "Work order not found." }));
        var entity = new WorkOrder(request.CompanyId, request.WorkOrderNumber ?? $"{source.WorkOrderNumber}-FU", null, null, source.ServiceContractId, source.EquipmentAssetId, source.CustomerId, null, source.Type, source.Priority, request.TechnicianId ?? source.TechnicianId, source.TerritoryId, request.ScheduledStart, request.ScheduledEnd, null, null, false, request.Notes ?? $"Follow-up for {source.WorkOrderNumber}");
        _context.WorkOrders.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpPost("work-orders/{id:guid}/expenses")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddExpense(Guid id, [FromBody] AddExpenseRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders.FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null) return NotFound(ApiResponse<Guid>.Failure(new[] { "Work order not found." }));
        var line = new WorkOrderLine(id, WorkOrderLineType.Expense, request.Description, request.Quantity, request.UnitRate, request.Billable, null, request.TechnicianId);
        wo.AddLine(line);
        await _context.SaveChangesAsync(cancellationToken);
        if (request.TechnicianId.HasValue)
        {
            var tech = await _context.Technicians.FirstOrDefaultAsync(t => t.Id == request.TechnicianId.Value, cancellationToken);
            if (tech is not null)
                await _integration.RecordTechnicianTimeAsync(request.CompanyId, tech.EmployeeId, 0, 0, DateTime.UtcNow, cancellationToken);
        }
        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    [HttpPost("work-orders/{id:guid}/schedule")]
    public async Task<ActionResult<ApiResponse<bool>>> Schedule(
        Guid id, [FromBody] ScheduleRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        if (request.TechnicianId.HasValue)
        {
            var conflict = await _context.WorkOrders.AnyAsync(w => w.Id != id && w.CompanyId == request.CompanyId && w.TechnicianId == request.TechnicianId.Value && w.Status != WorkOrderStatus.Closed && w.Status != WorkOrderStatus.Cancelled && w.ScheduledStart.HasValue && w.ScheduledEnd.HasValue && w.ScheduledStart < request.End && w.ScheduledEnd > request.Start, cancellationToken);
            if (conflict) return BadRequest(ApiResponse<bool>.Failure(new[] { "Technician has a conflicting work order in that window." }));
        }

        wo.Schedule(request.Start, request.End, request.TechnicianId);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // --- Labor capture (clock in/out) ---
    [HttpPost("work-orders/{id:guid}/clock-in")]
    public async Task<ActionResult<ApiResponse<bool>>> ClockIn(
        Guid id, [FromBody] CompanyScopedRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        wo.ClockInTech();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("work-orders/{id:guid}/clock-out")]
    public async Task<ActionResult<ApiResponse<bool>>> ClockOut(
        Guid id, [FromBody] ClockOutRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        wo.ClockOutTech(request.LaborHours, request.LaborCost);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // --- Add lines (labor / part / travel / fee / expense) ---
    [HttpPost("work-orders/{id:guid}/lines")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddLine(
        Guid id, [FromBody] AddWorkOrderLineRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<Guid>.Failure(new[] { "Work order not found." }));
        }

        var line = new WorkOrderLine(
            id,
            request.LineType,
            request.Description,
            request.Quantity,
            request.UnitRate,
            request.Billable,
            request.ItemId,
            request.TechnicianId);
        wo.AddLine(line);

        if (request.LineType == WorkOrderLineType.Part && request.ItemId.HasValue && request.Quantity > 0)
        {
            await _integration.IssuePartsAsync(
                request.CompanyId,
                request.ItemId.Value,
                wo.TerritoryId ?? Guid.Empty,
                request.Quantity,
                wo.WorkOrderNumber,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    // --- Complete + recalc billable ---
    [HttpPost("work-orders/{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> Complete(
        Guid id, [FromBody] CompleteWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .Include(w => w.Lines)
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        if (wo.WarrantyCovered)
        {
            var equipment = wo.EquipmentAssetId.HasValue ? await _context.EquipmentAssets.FirstOrDefaultAsync(e => e.Id == wo.EquipmentAssetId.Value, cancellationToken) : null;
            var underWarranty = equipment?.UnderWarranty == true && (equipment.WarrantyEnd == null || equipment.WarrantyEnd >= DateTime.UtcNow);
            var hasContract = wo.ServiceContractId.HasValue && await _context.ServiceContracts.AnyAsync(c => c.Id == wo.ServiceContractId.Value && c.Status.ToString() == "Active", cancellationToken);
            if (!underWarranty && !hasContract)
                return BadRequest(ApiResponse<bool>.Failure(new[] { "Warranty flag is set but no active warranty or contract coverage found; clear the flag or attach coverage." }));
            var hasClaim = await _context.WarrantyClaims.AnyAsync(w => w.WorkOrderId == wo.Id, cancellationToken);
            if (!hasClaim)
            {
                var claim = new WarrantyClaim(request.CompanyId, $"WC-{wo.WorkOrderNumber}", wo.EquipmentAssetId ?? Guid.Empty, wo.Id, request.Resolution ?? "Warranty work", wo.BillableTotal);
                _context.WarrantyClaims.Add(claim);
            }
            wo.RecalculateBillable(0, 0);
            wo.Complete(request.Resolution);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<bool>.Success(true));
        }

        wo.ClockOutTech(request.LaborHours, request.LaborCost);
        wo.RecalculateBillable(request.PartsMarkupPercent, request.TripCharge);
        wo.Complete(request.Resolution);

        if (!wo.WarrantyCovered && wo.BillableTotal > 0 && wo.CustomerId.HasValue)
        {
            var arInvoiceId = await _integration.BillWorkOrderToArAsync(
                request.CompanyId,
                wo.CustomerId.Value,
                wo.WorkOrderNumber,
                wo.BillableTotal,
                request.Resolution,
                cancellationToken);
            if (arInvoiceId.HasValue)
            {
                wo.MarkBilledToAr(arInvoiceId.Value);
            }
        }

        if (wo.TechnicianId.HasValue && request.LaborHours > 0)
        {
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.Id == wo.TechnicianId.Value, cancellationToken);
            if (technician is not null)
            {
                await _integration.RecordTechnicianTimeAsync(
                    request.CompanyId,
                    technician.EmployeeId,
                    request.LaborHours,
                    request.LaborCost / Math.Max(request.LaborHours, 1m),
                    DateTime.UtcNow,
                    cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("work-orders/{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<bool>>> Close(
        Guid id, [FromBody] CompanyScopedRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        wo.Close();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("work-orders/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(
        Guid id, [FromBody] CompanyScopedRequest request, CancellationToken cancellationToken)
    {
        var wo = await _context.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == request.CompanyId, cancellationToken);
        if (wo is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Work order not found." }));
        }

        wo.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // --- Estimate ---
    [HttpPost("estimates")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEstimate(
        [FromBody] CreateEstimateRequest request, CancellationToken cancellationToken)
    {
        var entity = new Estimate(
            request.CompanyId,
            request.EstimateNumber,
            request.CustomerId,
            request.ServiceContractId,
            request.EquipmentAssetId,
            request.BillingType,
            request.LaborEstimate,
            request.PartsEstimate,
            request.TravelEstimate,
            request.TaxEstimate,
            request.Notes);
        _context.Estimates.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpPost("estimates/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveEstimate(
        Guid id, [FromBody] CompanyScopedRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.Estimates
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == request.CompanyId, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Estimate not found." }));
        }

        entity.Approve();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("estimates/{id:guid}/convert")]
    public async Task<ActionResult<ApiResponse<Guid>>> ConvertEstimateToWorkOrder(
        Guid id, [FromBody] ConvertEstimateRequest request, CancellationToken cancellationToken)
    {
        var estimate = await _context.Estimates
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == request.CompanyId, cancellationToken);
        if (estimate is null)
        {
            return NotFound(ApiResponse<Guid>.Failure(new[] { "Estimate not found." }));
        }

        var wo = new WorkOrder(
            request.CompanyId,
            request.WorkOrderNumber,
            null,
            estimate.Id,
            estimate.ServiceContractId,
            null,
            estimate.CustomerId,
            null,
            WorkOrderType.Repair,
            SlaPriority.Medium,
            request.TechnicianId,
            request.TerritoryId,
            request.ScheduledStart,
            request.ScheduledEnd,
            null,
            null,
            false,
            estimate.Notes);
        _context.WorkOrders.Add(wo);
        estimate.MarkConverted();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(wo.Id));
    }

    [HttpGet("estimates")]
    public async Task<ActionResult<ApiResponse<List<EstimateDto>>>> GetEstimates(CancellationToken cancellationToken)
    {
        var list = await _context.Estimates
            .Select(e => new EstimateDto
            {
                Id = e.Id,
                EstimateNumber = e.EstimateNumber,
                CustomerId = e.CustomerId,
                Status = e.Status.ToString(),
                BillingType = e.BillingType.ToString(),
                LaborEstimate = e.LaborEstimate,
                PartsEstimate = e.PartsEstimate,
                TravelEstimate = e.TravelEstimate,
                TaxEstimate = e.TaxEstimate,
                TotalEstimate = e.TotalEstimate,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<EstimateDto>>.Success(list));
    }
}

// --- DTOs & requests ---
public record CreateServiceCallRequest(
    Guid CompanyId, string CallNumber, Guid? CustomerId, string? ContactName, string? ContactPhone,
    Guid? EquipmentAssetId, Guid? ServiceContractId, SlaPriority Priority, string Description, int ResponseMinutes);

public record ServiceCallDto
{
    public Guid Id { get; init; }
    public string CallNumber { get; init; } = string.Empty;
    public Guid? CustomerId { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime LoggedOn { get; init; }
    public DateTime? ResponseDue { get; init; }
    public Guid? WorkOrderId { get; init; }
}

public record CreateWorkOrderRequest(
    Guid CompanyId, string WorkOrderNumber, Guid? ServiceCallId, Guid? EstimateId, Guid? ServiceContractId,
    Guid? EquipmentAssetId, Guid? CustomerId, Guid? PreventiveMaintenanceId, WorkOrderType Type,
    SlaPriority Priority, Guid? TechnicianId, Guid? TerritoryId, DateTime? ScheduledStart, DateTime? ScheduledEnd,
    DateTime? ResponseDue, DateTime? ResolutionDue, bool WarrantyCovered, string? Notes);

public record WorkOrderDto
{
    public Guid Id { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public Guid? TechnicianId { get; init; }
    public DateTime? ScheduledStart { get; init; }
    public decimal BillableTotal { get; init; }
    public bool BilledToAr { get; init; }
    public bool WarrantyCovered { get; init; }
}

public record WorkOrderDetailDto
{
    public Guid Id { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public Guid? TechnicianId { get; init; }
    public Guid? TerritoryId { get; init; }
    public DateTime? ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public DateTime? ClockIn { get; init; }
    public DateTime? ClockOut { get; init; }
    public decimal LaborHours { get; init; }
    public decimal LaborCost { get; init; }
    public decimal PartsCost { get; init; }
    public decimal TravelCost { get; init; }
    public decimal Fees { get; init; }
    public decimal BillableTotal { get; init; }
    public bool WarrantyCovered { get; init; }
    public bool BilledToAr { get; init; }
    public string? Resolution { get; init; }
    public IReadOnlyCollection<WorkOrderLineDto> Lines { get; init; } = new List<WorkOrderLineDto>();
}

public record WorkOrderLineDto
{
    public Guid Id { get; init; }
    public string LineType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitRate { get; init; }
    public decimal LineTotal { get; init; }
    public bool Billable { get; init; }
    public Guid? ItemId { get; init; }
}

public record DispatchRequest(Guid CompanyId, Guid TechnicianId, Guid? TerritoryId);
public record ScheduleRequest(Guid CompanyId, DateTime Start, DateTime End, Guid? TechnicianId);
public record ClockOutRequest(Guid CompanyId, decimal LaborHours, decimal LaborCost);
public record AddWorkOrderLineRequest(
    Guid CompanyId, WorkOrderLineType LineType, string Description, decimal Quantity, decimal UnitRate,
    bool Billable, Guid? ItemId, Guid? TechnicianId);
public record CompleteWorkOrderRequest(
    Guid CompanyId, decimal LaborHours, decimal LaborCost, decimal PartsMarkupPercent, decimal TripCharge, string? Resolution);
public record DispatchSuggestionDto { public Guid TechnicianId { get; init; } public string Code { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public Guid? TerritoryId { get; init; } public int SkillCount { get; init; } public int TotalProficiency { get; init; } public bool Available { get; init; } public int Score { get; init; } }
public record TechnicianAvailabilityDto { public Guid TechnicianId { get; init; } public bool Available { get; init; } public List<AvailabilityConflictDto> Conflicts { get; init; } = new(); }
public record AvailabilityConflictDto { public Guid WorkOrderId { get; init; } public string WorkOrderNumber { get; init; } = string.Empty; public DateTime ScheduledStart { get; init; } public DateTime ScheduledEnd { get; init; } public string Status { get; init; } = string.Empty; }
public record CreateFollowUpRequest(Guid CompanyId, string? WorkOrderNumber, Guid? TechnicianId, DateTime? ScheduledStart, DateTime? ScheduledEnd, string? Notes);
public record AddExpenseRequest(Guid CompanyId, string Description, decimal Quantity, decimal UnitRate, bool Billable, Guid? TechnicianId);
public record CreateEstimateRequest(
    Guid CompanyId, string EstimateNumber, Guid? CustomerId, Guid? ServiceContractId, Guid? EquipmentAssetId,
    BillingType BillingType, decimal LaborEstimate, decimal PartsEstimate, decimal TravelEstimate,
    decimal TaxEstimate, string? Notes);
public record ConvertEstimateRequest(
    Guid CompanyId, string WorkOrderNumber, Guid? TechnicianId, Guid? TerritoryId,
    DateTime? ScheduledStart, DateTime? ScheduledEnd);
public record EstimateDto
{
    public Guid Id { get; init; }
    public string EstimateNumber { get; init; } = string.Empty;
    public Guid? CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string BillingType { get; init; } = string.Empty;
    public decimal LaborEstimate { get; init; }
    public decimal PartsEstimate { get; init; }
    public decimal TravelEstimate { get; init; }
    public decimal TaxEstimate { get; init; }
    public decimal TotalEstimate { get; init; }
}
