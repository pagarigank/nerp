// <copyright file="BudgetController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/projects/{projectId:guid}/budget")]
public class BudgetController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;

    public BudgetController(ProjDbContext context, IProjUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BudgetLineDto>>>> GetBudgetLines(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var lines = await _context.BudgetLines
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.Category)
            .ToListAsync(cancellationToken);

        var dtos = lines.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<BudgetLineDto>>.Success(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> AddBudgetLine(
        Guid projectId,
        [FromBody] AddBudgetLineRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var category = Enum.TryParse<CostCategory>(request.Category, true, out var cat) ? cat : CostCategory.Labor;

        var line = project.AddBudgetLine(
            request.TaskId,
            category,
            request.BudgetAmount,
            request.BudgetedHours,
            request.Description);

        project.RecalculateBudget();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    [HttpPut("{lineId:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateBudgetLine(
        Guid projectId,
        Guid lineId,
        [FromBody] UpdateBudgetLineRequest request,
        CancellationToken cancellationToken)
    {
        var line = await _context.BudgetLines
            .FirstOrDefaultAsync(b => b.Id == lineId && b.ProjectId == projectId, cancellationToken);

        if (line is null)
            return NotFound(ApiResponse.Failure(new[] { "Budget line not found." }, 404));

        line.Update(request.BudgetAmount, request.BudgetedHours, request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Success());
    }

    [HttpPost("{lineId:guid}/revise")]
    public async Task<ActionResult<ApiResponse>> ReviseBudgetLine(
        Guid projectId,
        Guid lineId,
        [FromBody] ReviseBudgetLineRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.BudgetLines)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var originalLine = project.BudgetLines.FirstOrDefault(b => b.Id == lineId);
        if (originalLine is null)
            return NotFound(ApiResponse.Failure(new[] { "Budget line not found." }, 404));

        // Mark original as revised
        originalLine.Update(request.NewAmount, null, originalLine.Description + " (revised)");

        project.RecalculateBudget();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{lineId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteBudgetLine(
        Guid projectId,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        var line = await _context.BudgetLines
            .FirstOrDefaultAsync(b => b.Id == lineId && b.ProjectId == projectId, cancellationToken);

        if (line is null)
            return NotFound(ApiResponse.Failure(new[] { "Budget line not found." }, 404));

        _context.BudgetLines.Remove(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Success());
    }

    // --- WIP Schedule ---
    [HttpGet("wip")]
    public async Task<ActionResult<ApiResponse<WipScheduleDto>>> GetWipSchedule(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.BudgetLines)
            .Include(p => p.ContractLines)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var totalBudget = project.BudgetLines.Sum(b => b.BudgetAmount);
        var costsToDate = project.CostsToDate;
        var earnedRevenue = totalBudget > 0 ? (costsToDate / totalBudget) * (project.ContractValue ?? 0) : 0;
        var billedToDate = project.ContractLines?.Sum(c => c.BilledAmount) ?? 0;

        var dto = new WipScheduleDto
        {
            ProjectId = projectId,
            ContractValue = project.ContractValue ?? 0,
            TotalBudget = totalBudget,
            CostsToDate = costsToDate,
            PercentComplete = project.PercentComplete,
            EarnedRevenue = earnedRevenue,
            BilledToDate = billedToDate,
            OverUnderBilling = earnedRevenue - billedToDate,
            RetainageHeld = project.RetainageHeld,
        };

        return Ok(ApiResponse<WipScheduleDto>.Success(dto));
    }

    private static BudgetLineDto MapToDto(BudgetLine b) => new ()
    {
        Id = b.Id,
        ProjectId = b.ProjectId,
        TaskId = b.TaskId,
        Category = b.Category.ToString(),
        BudgetAmount = b.BudgetAmount,
        BudgetedHours = b.BudgetedHours,
        ActualAmount = b.ActualAmount,
        ActualHours = b.ActualHours,
        CommittedAmount = b.CommittedAmount,
        Variance = b.Variance,
        Description = b.Description,
        IsRevised = b.IsRevised,
    };
}

public class BudgetLineDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid TaskId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal BudgetedHours { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ActualHours { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal Variance { get; set; }
    public string? Description { get; set; }
    public bool IsRevised { get; set; }
}

public class WipScheduleDto
{
    public Guid ProjectId { get; set; }
    public decimal ContractValue { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal CostsToDate { get; set; }
    public decimal PercentComplete { get; set; }
    public decimal EarnedRevenue { get; set; }
    public decimal BilledToDate { get; set; }
    public decimal OverUnderBilling { get; set; }
    public decimal RetainageHeld { get; set; }
}

public class AddBudgetLineRequest
{
    public Guid TaskId { get; set; }
    public string Category { get; set; } = "Labor";
    public decimal BudgetAmount { get; set; }
    public decimal? BudgetedHours { get; set; }
    public string? Description { get; set; }
}

public class UpdateBudgetLineRequest
{
    public decimal? BudgetAmount { get; set; }
    public decimal? BudgetedHours { get; set; }
    public string? Description { get; set; }
}

public class ReviseBudgetLineRequest
{
    public decimal NewAmount { get; set; }
    public string? Reason { get; set; }
}
