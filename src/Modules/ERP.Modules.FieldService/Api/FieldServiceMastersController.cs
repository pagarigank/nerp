// <copyright file="FieldServiceMastersController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.FieldService.Domain.Entities;
using ERP.Modules.FieldService.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.FieldService.Api;

[ApiController]
[Route("api/v1/field-service")]
public class FieldServiceMastersController : ControllerBase
{
    private readonly FieldServiceDbContext _context;

    public FieldServiceMastersController(FieldServiceDbContext context)
    {
        _context = context;
    }

    // --- Service Contract ---
    [HttpPost("service-contracts")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateServiceContract(
        [FromBody] CreateServiceContractRequest request, CancellationToken cancellationToken)
    {
        var entity = new ServiceContract(
            request.CompanyId,
            request.ContractNumber,
            request.Name,
            request.CustomerId,
            request.StartDate,
            request.EndDate,
            request.BillingType,
            request.ContractValue,
            request.IncludesWarranty,
            request.WarrantyMonths,
            request.Notes);
        _context.ServiceContracts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("service-contracts")]
    public async Task<ActionResult<ApiResponse<List<ServiceContractDto>>>> GetServiceContracts(CancellationToken cancellationToken)
    {
        var list = await _context.ServiceContracts
            .Select(c => new ServiceContractDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Name = c.Name,
                CustomerId = c.CustomerId,
                Status = c.Status.ToString(),
                BillingType = c.BillingType.ToString(),
                ContractValue = c.ContractValue,
                EndDate = c.EndDate,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ServiceContractDto>>.Success(list));
    }

    [HttpPost("service-contracts/{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> ActivateServiceContract(
        Guid id, [FromBody] CompanyScopedRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.ServiceContracts
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == request.CompanyId, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Service contract not found." }));
        }

        entity.Activate();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // --- Equipment Asset ---
    [HttpPost("equipment")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEquipment(
        [FromBody] CreateEquipmentRequest request, CancellationToken cancellationToken)
    {
        var entity = new EquipmentAsset(
            request.CompanyId,
            request.AssetTag,
            request.SerialNumber,
            request.Description,
            request.ItemId,
            request.CustomerId,
            request.LocationId,
            request.Ownership,
            request.InstallDate,
            request.WarrantyStart,
            request.WarrantyEnd,
            request.UnderWarranty,
            request.Notes);
        _context.EquipmentAssets.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("equipment")]
    public async Task<ActionResult<ApiResponse<List<EquipmentAssetDto>>>> GetEquipment(CancellationToken cancellationToken)
    {
        var list = await _context.EquipmentAssets
            .Select(e => new EquipmentAssetDto
            {
                Id = e.Id,
                AssetTag = e.AssetTag,
                SerialNumber = e.SerialNumber,
                Description = e.Description,
                CustomerId = e.CustomerId,
                Ownership = e.Ownership.ToString(),
                UnderWarranty = e.UnderWarranty,
                WarrantyEnd = e.WarrantyEnd,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<EquipmentAssetDto>>.Success(list));
    }

    [HttpPost("equipment/{id:guid}/warranty")]
    public async Task<ActionResult<ApiResponse<bool>>> SetWarranty(
        Guid id, [FromBody] SetWarrantyRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.EquipmentAssets
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == request.CompanyId, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Equipment asset not found." }));
        }

        entity.MarkWarranty(request.UnderWarranty);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // --- Technician ---
    [HttpPost("technicians")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTechnician(
        [FromBody] CreateTechnicianRequest request, CancellationToken cancellationToken)
    {
        var entity = new Technician(
            request.CompanyId,
            request.EmployeeId,
            request.Code,
            request.FirstName,
            request.LastName,
            request.DefaultTerritoryId,
            request.HomeLocationId,
            request.Status,
            request.Email,
            request.Phone,
            request.HourlyRate);
        _context.Technicians.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("technicians")]
    public async Task<ActionResult<ApiResponse<List<TechnicianDto>>>> GetTechnicians(CancellationToken cancellationToken)
    {
        var list = await _context.Technicians
            .ApplyCompanyScope(HttpContext, t => t.CompanyId)
            .Select(t => new TechnicianDto
            {
                Id = t.Id,
                Code = t.Code,
                FirstName = t.FirstName,
                LastName = t.LastName,
                Status = t.Status.ToString(),
                DefaultTerritoryId = t.DefaultTerritoryId,
                HourlyRate = t.HourlyRate,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TechnicianDto>>.Success(list));
    }

    // --- Skill / Certification ---
    [HttpPost("skills")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateSkill(
        [FromBody] CreateSkillRequest request, CancellationToken cancellationToken)
    {
        var entity = new SkillCertification(
            request.CompanyId,
            request.Code,
            request.Name,
            request.Category,
            request.Description);
        _context.SkillCertifications.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpPost("technicians/{technicianId:guid}/skills")]
    public async Task<ActionResult<ApiResponse<Guid>>> AssignSkill(
        Guid technicianId, [FromBody] AssignSkillRequest request, CancellationToken cancellationToken)
    {
        var entity = new TechnicianSkill(
            request.CompanyId,
            technicianId,
            request.SkillCertificationId,
            request.Proficiency,
            request.CertifiedDate,
            request.ExpirationDate);
        _context.TechnicianSkills.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("skills")]
    public async Task<ActionResult<ApiResponse<List<SkillCertificationDto>>>> GetSkills(CancellationToken cancellationToken)
    {
        var list = await _context.SkillCertifications
            .Select(s => new SkillCertificationDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Category = s.Category,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<SkillCertificationDto>>.Success(list));
    }

    // --- SLA / Priority ---
    [HttpPost("slas")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateSla(
        [FromBody] CreateSlaRequest request, CancellationToken cancellationToken)
    {
        var entity = new SlaDefinition(
            request.CompanyId,
            request.Name,
            request.Priority,
            request.ResponseMinutes,
            request.ResolutionMinutes,
            request.Escalate,
            request.EscalationTo);
        _context.SlaDefinitions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("slas")]
    public async Task<ActionResult<ApiResponse<List<SlaDefinitionDto>>>> GetSlas(CancellationToken cancellationToken)
    {
        var list = await _context.SlaDefinitions
            .Select(s => new SlaDefinitionDto
            {
                Id = s.Id,
                Name = s.Name,
                Priority = s.Priority.ToString(),
                ResponseMinutes = s.ResponseMinutes,
                ResolutionMinutes = s.ResolutionMinutes,
                Escalate = s.Escalate,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<SlaDefinitionDto>>.Success(list));
    }

    // --- Service Territory ---
    [HttpPost("territories")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTerritory(
        [FromBody] CreateTerritoryRequest request, CancellationToken cancellationToken)
    {
        var entity = new ServiceTerritory(
            request.CompanyId,
            request.Code,
            request.Name,
            request.Region,
            request.ZipCoverage,
            request.DefaultTechnicianId,
            request.TravelCostPerMile);
        _context.ServiceTerritories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("territories")]
    public async Task<ActionResult<ApiResponse<List<ServiceTerritoryDto>>>> GetTerritories(CancellationToken cancellationToken)
    {
        var list = await _context.ServiceTerritories
            .Select(t => new ServiceTerritoryDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Region = t.Region,
                DefaultTechnicianId = t.DefaultTechnicianId,
                TravelCostPerMile = t.TravelCostPerMile,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ServiceTerritoryDto>>.Success(list));
    }

    // --- Service Rate Card ---
    [HttpPost("rate-cards")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRateCard(
        [FromBody] CreateRateCardRequest request, CancellationToken cancellationToken)
    {
        var entity = new ServiceRateCard(
            request.CompanyId,
            request.Name,
            request.EffectiveDate,
            request.ExpirationDate,
            request.IsActive,
            request.LaborRatePerHour,
            request.OvertimeRatePerHour,
            request.TripCharge,
            request.PartsMarkupPercent);
        _context.ServiceRateCards.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("rate-cards")]
    public async Task<ActionResult<ApiResponse<List<ServiceRateCardDto>>>> GetRateCards(CancellationToken cancellationToken)
    {
        var list = await _context.ServiceRateCards
            .Select(r => new ServiceRateCardDto
            {
                Id = r.Id,
                Name = r.Name,
                EffectiveDate = r.EffectiveDate,
                LaborRatePerHour = r.LaborRatePerHour,
                OvertimeRatePerHour = r.OvertimeRatePerHour,
                TripCharge = r.TripCharge,
                PartsMarkupPercent = r.PartsMarkupPercent,
                IsActive = r.IsActive,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ServiceRateCardDto>>.Success(list));
    }

    // --- Preventive Maintenance ---
    [HttpPost("preventive-maintenance")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePm(
        [FromBody] CreatePmRequest request, CancellationToken cancellationToken)
    {
        var entity = new PreventiveMaintenance(
            request.CompanyId,
            request.Code,
            request.Description,
            request.EquipmentAssetId,
            request.ServiceContractId,
            request.DefaultTechnicianId,
            request.Frequency,
            request.IntervalMonths,
            request.LastGenerated,
            request.NextDue,
            request.Checklist,
            request.IsActive);
        _context.PreventiveMaintenances.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("preventive-maintenance")]
    public async Task<ActionResult<ApiResponse<List<PreventiveMaintenanceDto>>>> GetPm(CancellationToken cancellationToken)
    {
        var list = await _context.PreventiveMaintenances
            .Select(p => new PreventiveMaintenanceDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                EquipmentAssetId = p.EquipmentAssetId,
                Frequency = p.Frequency.ToString(),
                IntervalMonths = p.IntervalMonths,
                NextDue = p.NextDue,
                IsActive = p.IsActive,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PreventiveMaintenanceDto>>.Success(list));
    }

    // --- Van Stock ---
    [HttpPost("van-stock")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateVanStock(
        [FromBody] CreateVanStockRequest request, CancellationToken cancellationToken)
    {
        var entity = new VanStock(
            request.CompanyId,
            request.TechnicianId,
            request.ItemId,
            request.WarehouseId,
            request.QuantityOnHand,
            request.ReorderPoint);
        _context.VanStocks.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("van-stock")]
    public async Task<ActionResult<ApiResponse<List<VanStockDto>>>> GetVanStock(CancellationToken cancellationToken)
    {
        var list = await _context.VanStocks
            .Select(v => new VanStockDto
            {
                Id = v.Id,
                TechnicianId = v.TechnicianId,
                ItemId = v.ItemId,
                WarehouseId = v.WarehouseId,
                QuantityOnHand = v.QuantityOnHand,
                ReorderPoint = v.ReorderPoint,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<VanStockDto>>.Success(list));
    }

    [HttpPost("van-stock/{id:guid}/issue")]
    public async Task<ActionResult<ApiResponse<bool>>> IssueVanStock(
        Guid id, [FromBody] VanStockQtyRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.VanStocks
            .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == request.CompanyId, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Van stock not found." }));
        }

        entity.IssueParts(request.Quantity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("van-stock/{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<bool>>> ReceiveVanStock(
        Guid id, [FromBody] VanStockQtyRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.VanStocks
            .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == request.CompanyId, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<bool>.Failure(new[] { "Van stock not found." }));
        }

        entity.ReceiveParts(request.Quantity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // --- Warranty Claim ---
    [HttpPost("warranty-claims")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateWarrantyClaim(
        [FromBody] CreateWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        var entity = new WarrantyClaim(
            request.CompanyId,
            request.ClaimNumber,
            request.EquipmentAssetId,
            request.WorkOrderId,
            request.Description,
            request.ClaimAmount);
        _context.WarrantyClaims.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(entity.Id));
    }

    [HttpGet("warranty-claims")]
    public async Task<ActionResult<ApiResponse<List<WarrantyClaimDto>>>> GetWarrantyClaims(CancellationToken cancellationToken)
    {
        var list = await _context.WarrantyClaims
            .Select(w => new WarrantyClaimDto
            {
                Id = w.Id,
                ClaimNumber = w.ClaimNumber,
                EquipmentAssetId = w.EquipmentAssetId,
                WorkOrderId = w.WorkOrderId,
                ClaimAmount = w.ClaimAmount,
                Status = w.Status,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WarrantyClaimDto>>.Success(list));
    }
}

// --- DTOs & requests ---
public record CompanyScopedRequest(Guid CompanyId);

public record CreateServiceContractRequest(
    Guid CompanyId, string ContractNumber, string Name, Guid CustomerId,
    DateTime StartDate, DateTime EndDate, BillingType BillingType, decimal? ContractValue,
    bool IncludesWarranty, int? WarrantyMonths, string? Notes);

public record ServiceContractDto
{
    public Guid Id { get; init; }
    public string ContractNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string BillingType { get; init; } = string.Empty;
    public decimal? ContractValue { get; init; }
    public DateTime EndDate { get; init; }
}

public record CreateEquipmentRequest(
    Guid CompanyId, string AssetTag, string SerialNumber, string Description,
    Guid? ItemId, Guid? CustomerId, Guid? LocationId, EquipmentOwnership Ownership,
    DateTime? InstallDate, DateTime? WarrantyStart, DateTime? WarrantyEnd, bool UnderWarranty, string? Notes);

public record EquipmentAssetDto
{
    public Guid Id { get; init; }
    public string AssetTag { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? CustomerId { get; init; }
    public string Ownership { get; init; } = string.Empty;
    public bool UnderWarranty { get; init; }
    public DateTime? WarrantyEnd { get; init; }
}

public record SetWarrantyRequest(Guid CompanyId, bool UnderWarranty);

public record CreateTechnicianRequest(
    Guid CompanyId, Guid EmployeeId, string Code, string FirstName, string LastName,
    Guid? DefaultTerritoryId, Guid? HomeLocationId, TechnicianStatus Status,
    string? Email, string? Phone, decimal HourlyRate);

public record TechnicianDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? DefaultTerritoryId { get; init; }
    public decimal HourlyRate { get; init; }
}

public record CreateSkillRequest(Guid CompanyId, string Code, string Name, string? Category, string? Description);
public record AssignSkillRequest(Guid CompanyId, Guid SkillCertificationId, int Proficiency, DateTime? CertifiedDate, DateTime? ExpirationDate);
public record SkillCertificationDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Category { get; init; }
}

public record CreateSlaRequest(Guid CompanyId, string Name, SlaPriority Priority, int ResponseMinutes, int ResolutionMinutes, bool Escalate, string? EscalationTo);
public record SlaDefinitionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public int ResponseMinutes { get; init; }
    public int ResolutionMinutes { get; init; }
    public bool Escalate { get; init; }
}

public record CreateTerritoryRequest(Guid CompanyId, string Code, string Name, string? Region, string? ZipCoverage, Guid? DefaultTechnicianId, decimal TravelCostPerMile);
public record ServiceTerritoryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Region { get; init; }
    public Guid? DefaultTechnicianId { get; init; }
    public decimal TravelCostPerMile { get; init; }
}

public record CreateRateCardRequest(Guid CompanyId, string Name, DateTime EffectiveDate, DateTime? ExpirationDate, bool IsActive, decimal LaborRatePerHour, decimal OvertimeRatePerHour, decimal TripCharge, decimal PartsMarkupPercent);
public record ServiceRateCardDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime EffectiveDate { get; init; }
    public decimal LaborRatePerHour { get; init; }
    public decimal OvertimeRatePerHour { get; init; }
    public decimal TripCharge { get; init; }
    public decimal PartsMarkupPercent { get; init; }
    public bool IsActive { get; init; }
}

public record CreatePmRequest(Guid CompanyId, string Code, string Description, Guid? EquipmentAssetId, Guid? ServiceContractId, Guid? DefaultTechnicianId, PmFrequency Frequency, int IntervalMonths, DateTime? LastGenerated, DateTime? NextDue, string? Checklist, bool IsActive);
public record PreventiveMaintenanceDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? EquipmentAssetId { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public int IntervalMonths { get; init; }
    public DateTime? NextDue { get; init; }
    public bool IsActive { get; init; }
}

public record CreateVanStockRequest(Guid CompanyId, Guid TechnicianId, Guid ItemId, Guid WarehouseId, decimal QuantityOnHand, decimal ReorderPoint);
public record VanStockQtyRequest(Guid CompanyId, decimal Quantity);
public record VanStockDto
{
    public Guid Id { get; init; }
    public Guid TechnicianId { get; init; }
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal QuantityOnHand { get; init; }
    public decimal ReorderPoint { get; init; }
}

public record CreateWarrantyClaimRequest(Guid CompanyId, string ClaimNumber, Guid EquipmentAssetId, Guid? WorkOrderId, string Description, decimal ClaimAmount);
public record WarrantyClaimDto
{
    public Guid Id { get; init; }
    public string ClaimNumber { get; init; } = string.Empty;
    public Guid EquipmentAssetId { get; init; }
    public Guid? WorkOrderId { get; init; }
    public decimal ClaimAmount { get; init; }
    public string Status { get; init; } = string.Empty;
}
