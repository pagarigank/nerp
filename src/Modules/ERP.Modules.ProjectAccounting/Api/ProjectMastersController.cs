// <copyright file="ProjectMastersController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/project-accounting")]
public class ProjectMastersController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;

    public ProjectMastersController(ProjDbContext context, IProjUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    // ---- Role rates (737) ----
    [HttpGet("role-rates")]
    public async Task<ActionResult<ApiResponse<List<ProjectRoleRate>>>> GetRoleRates([FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<ProjectRoleRate>>.Success(await _context.ProjectRoleRates.Where(r => r.CompanyId == companyId).ToListAsync(ct)));

    [HttpPost("role-rates")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRoleRate([FromBody] RoleRateRequest r, CancellationToken ct)
    {
        var e = new ProjectRoleRate(r.CompanyId, r.RoleName, r.CostRate, r.BillingRate, r.Description);
        _context.ProjectRoleRates.Add(e);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("role-rates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateRoleRate(Guid id, [FromBody] RoleRateUpdateRequest r, CancellationToken ct)
    {
        var e = await _context.ProjectRoleRates.FindAsync(new object[] { id }, ct);
        if (e is null)
            return NotFound(ApiResponse.Failure(new[] { "Role rate not found." }, 404));
        e.Update(r.CostRate, r.BillingRate, r.Description, r.IsActive);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // ---- Budget templates (736) ----
    [HttpGet("budget-templates")]
    public async Task<ActionResult<ApiResponse<List<BudgetTemplate>>>> GetBudgetTemplates([FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<BudgetTemplate>>.Success(await _context.BudgetTemplates.Include(t => t.Lines).Where(t => t.CompanyId == companyId).ToListAsync(ct)));

    [HttpPost("budget-templates")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBudgetTemplate([FromBody] BudgetTemplateRequest r, CancellationToken ct)
    {
        var e = new BudgetTemplate(r.CompanyId, r.Name, r.ProjectType, r.Description);
        foreach (var l in r.Lines)
            e.AddLine(Enum.Parse<CostCategory>(l.Category, true), l.BudgetAmount, l.BudgetedHours, l.Description);
        _context.BudgetTemplates.Add(e);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    // ---- Employee-project assignments (740) ----
    [HttpGet("employee-assignments")]
    public async Task<ActionResult<ApiResponse<List<EmployeeProjectAssignment>>>> GetAssignments([FromQuery] Guid projectId, CancellationToken ct)
        => Ok(ApiResponse<List<EmployeeProjectAssignment>>.Success(await _context.EmployeeProjectAssignments.Where(a => a.ProjectId == projectId).ToListAsync(ct)));

    [HttpPost("employee-assignments")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAssignment([FromBody] AssignmentRequest r, CancellationToken ct)
    {
        var e = new EmployeeProjectAssignment(r.CompanyId, r.ProjectId, r.TaskId, r.EmployeeId, r.RoleName, r.AllocationPercentage, r.EffectiveFrom, r.EffectiveTo);
        _context.EmployeeProjectAssignments.Add(e);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    // ---- Allocation rules (existing entity) ----
    [HttpGet("allocation-rules")]
    public async Task<ActionResult<ApiResponse<List<ProjectAllocationRule>>>> GetAllocationRules([FromQuery] Guid projectId, CancellationToken ct)
        => Ok(ApiResponse<List<ProjectAllocationRule>>.Success(await _context.ProjectAllocationRules.Where(r => r.ProjectId == projectId).ToListAsync(ct)));

    [HttpPost("allocation-rules")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAllocationRule([FromBody] AllocationRuleRequest r, CancellationToken ct)
    {
        var e = new ProjectAllocationRule(r.ProjectId, Enum.Parse<CostCategory>(r.Category, true), r.MarkupPercentage, r.OverheadPercentage, r.Description, r.Priority);
        _context.ProjectAllocationRules.Add(e);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    // ---- Committed cost (759) ----
    [HttpGet("committed-costs")]
    public async Task<ActionResult<ApiResponse<List<ProjectCommittedCost>>>> GetCommittedCosts([FromQuery] Guid projectId, CancellationToken ct)
        => Ok(ApiResponse<List<ProjectCommittedCost>>.Success(await _context.ProjectCommittedCosts.Where(c => c.ProjectId == projectId).ToListAsync(ct)));

    [HttpPost("committed-costs")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCommittedCost([FromBody] CommittedCostRequest r, CancellationToken ct)
    {
        var e = new ProjectCommittedCost(r.CompanyId, r.ProjectId, r.TaskId, Enum.Parse<CostCategory>(r.Category, true), r.Amount, r.SourceType, r.SourceReference, r.ExpectedDate);
        _context.ProjectCommittedCosts.Add(e);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }
}

// --- Request DTOs ---
public class RoleRateRequest
{
    public Guid CompanyId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal CostRate { get; set; }
    public decimal BillingRate { get; set; }
    public string? Description { get; set; }
}

public class RoleRateUpdateRequest
{
    public decimal? CostRate { get; set; }
    public decimal? BillingRate { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class BudgetTemplateRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectType { get; set; }
    public string? Description { get; set; }
#pragma warning disable CA1002, CA2227
    public List<BudgetTemplateLineRequest> Lines { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

public class BudgetTemplateLineRequest
{
    public string Category { get; set; } = "Labor";
    public decimal BudgetAmount { get; set; }
    public decimal? BudgetedHours { get; set; }
    public string? Description { get; set; }
}

public class AssignmentRequest
{
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class AllocationRuleRequest
{
    public Guid ProjectId { get; set; }
    public string Category { get; set; } = "Labor";
    public decimal MarkupPercentage { get; set; }
    public decimal? OverheadPercentage { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; } = 100;
}

public class CommittedCostRequest
{
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string Category { get; set; } = "Materials";
    public decimal Amount { get; set; }
    public string SourceType { get; set; } = "OpenPO";
    public string? SourceReference { get; set; }
    public DateTime? ExpectedDate { get; set; }
}
