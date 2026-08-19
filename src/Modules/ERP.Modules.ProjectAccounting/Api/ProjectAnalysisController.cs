// <copyright file="ProjectAnalysisController.cs" company="ERP Project">
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
[Route("api/v1/projects/{projectId:guid}/analysis")]
public class ProjectAnalysisController : ControllerBase
{
    private readonly ProjDbContext _context;

    public ProjectAnalysisController(ProjDbContext context)
    {
        _context = context;
    }

    /// <summary>WIP schedule: contract value, costs-to-date, earned revenue, billed, over/under billing.</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WIP schedule for the project.</returns>
    [HttpGet("wip")]
    public async Task<ActionResult<ApiResponse<ProjectWipDto>>> GetWip(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var earned = project.ContractValue.HasValue
            ? project.ContractValue.Value * (project.PercentComplete / 100m)
            : project.CostsToDate;
        var overUnder = earned - project.RevenueToDate;

        var dto = new ProjectWipDto
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            Name = project.Name,
            ContractValue = project.ContractValue ?? 0,
            CostsToDate = project.CostsToDate,
            PercentComplete = project.PercentComplete,
            EarnedRevenue = earned,
            BilledToDate = project.RevenueToDate,
            OverUnderBilling = overUnder,
            RetainageHeld = project.RetainageHeld,
        };

        return Ok(ApiResponse<ProjectWipDto>.Success(dto));
    }

    /// <summary>EAC / ETC / EVA analysis for the project.</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Forecast metrics for the project.</returns>
    [HttpGet("forecast")]
    public async Task<ActionResult<ApiResponse<ForecastDto>>> GetForecast(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var budget = project.RevisedBudget;
        if (budget <= 0)
        {
            budget = project.OriginalBudget;
        }

        if (budget <= 0)
        {
            budget = project.ContractValue ?? 0;
        }

        var eac = project.PercentComplete > 0
            ? project.CostsToDate / (project.PercentComplete / 100m)
            : budget;
        var etc = eac - project.CostsToDate;
        var variance = budget - eac;

        // Earned Value
        var ev = budget > 0 ? budget * (project.PercentComplete / 100m) : 0m;
        var ac = project.CostsToDate;
        var pv = budget > 0 ? budget * (project.PercentComplete / 100m) : 0m;
        var sv = ev - pv;
        var cv = ev - ac;
        var spi = pv > 0 ? ev / pv : 0m;
        var cpi = ac > 0 ? ev / ac : 0m;

        var dto = new ForecastDto
        {
            ProjectId = project.Id,
            BudgetAtCompletion = budget,
            EstimateAtCompletion = eac,
            EstimateToComplete = etc,
            VarianceAtCompletion = variance,
            EarnedValue = ev,
            ActualCost = ac,
            PlannedValue = pv,
            ScheduleVariance = sv,
            CostVariance = cv,
            SchedulePerformanceIndex = spi,
            CostPerformanceIndex = cpi,
            ProfitMargin = project.ProfitMargin ?? 0,
        };

        return Ok(ApiResponse<ForecastDto>.Success(dto));
    }

    /// <summary>Project profitability: revenue, costs, margin.</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Profitability metrics for the project.</returns>
    [HttpGet("profitability")]
    public async Task<ActionResult<ApiResponse<ProfitabilityDto>>> GetProfitability(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var revenue = project.RevenueToDate;
        var costs = project.CostsToDate;
        var margin = revenue - costs;
        var marginPct = revenue > 0 ? (margin / revenue) * 100 : 0m;

        var dto = new ProfitabilityDto
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            Name = project.Name,
            Revenue = revenue,
            Costs = costs,
            Margin = margin,
            MarginPercent = marginPct,
            ContractValue = project.ContractValue ?? 0,
            RetainageHeld = project.RetainageHeld,
        };

        return Ok(ApiResponse<ProfitabilityDto>.Success(dto));
    }

    /// <summary>Budget vs. actual by task and category.</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of budget vs. actual rows.</returns>
    [HttpGet("budget-vs-actual")]
    public async Task<ActionResult<ApiResponse<List<BudgetVsActualDto>>>> GetBudgetVsActual(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var rows = project.BudgetLines.Select(b => new BudgetVsActualDto
        {
            TaskId = b.TaskId,
            Category = b.Category.ToString(),
            Description = b.Description,
            BudgetAmount = b.BudgetAmount,
            ActualAmount = b.ActualAmount,
            CommittedAmount = b.CommittedAmount,
            Variance = b.Variance,
            VariancePercent = b.VariancePercent,
        }).ToList();

        return Ok(ApiResponse<List<BudgetVsActualDto>>.Success(rows));
    }

    /// <summary>Unbilled AR / revenue report (earned but not invoiced).</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unbilled amounts for the project.</returns>
    [HttpGet("unbilled")]
    public async Task<ActionResult<ApiResponse<UnbilledDto>>> GetUnbilled(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var earned = project.ContractValue.HasValue
            ? project.ContractValue.Value * (project.PercentComplete / 100m)
            : project.CostsToDate;
        var unbilled = earned - project.RevenueToDate;

        var dto = new UnbilledDto
        {
            ProjectId = project.Id,
            EarnedRevenue = earned,
            BilledRevenue = project.RevenueToDate,
            UnbilledAmount = unbilled,
            RetainageHeld = project.RetainageHeld,
        };

        return Ok(ApiResponse<UnbilledDto>.Success(dto));
    }

    /// <summary>Change order summary: original, approved, pending, revised budget.</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Change order summary for the project.</returns>
    [HttpGet("change-orders")]
    public async Task<ActionResult<ApiResponse<ChangeOrderSummaryDto>>> GetChangeOrderSummary(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var approved = project.ChangeOrders.Where(c => c.Status == ChangeOrderStatus.Approved || c.Status == ChangeOrderStatus.Executed).Sum(c => c.Amount);
        var pending = project.ChangeOrders.Where(c => c.Status == ChangeOrderStatus.Submitted || c.Status == ChangeOrderStatus.Draft).Sum(c => c.Amount);

        var dto = new ChangeOrderSummaryDto
        {
            ProjectId = project.Id,
            OriginalBudget = project.OriginalBudget,
            ApprovedChangeOrders = approved,
            PendingChangeOrders = pending,
            RevisedBudget = project.OriginalBudget + approved,
            TotalChangeOrders = project.ChangeOrders.Count,
        };

        return Ok(ApiResponse<ChangeOrderSummaryDto>.Success(dto));
    }

    /// <summary>Cost detail: all cost transactions.</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cost transactions for the project.</returns>
    [HttpGet("cost-detail")]
    public async Task<ActionResult<ApiResponse<List<CostDetailDto>>>> GetCostDetail(
        Guid projectId, CancellationToken cancellationToken)
    {
        var costs = await _context.CostTransactions
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.TransactionDate)
            .Select(c => new CostDetailDto
            {
                Id = c.Id,
                Category = c.Category.ToString(),
                TransactionType = c.TransactionType.ToString(),
                Amount = c.Amount,
                Hours = c.Hours,
                BillableAmount = c.BillableAmount,
                Description = c.Description,
                TransactionDate = c.TransactionDate,
                Status = c.Status.ToString(),
            }).ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<CostDetailDto>>.Success(costs));
    }

    private async Task<Project?> LoadProject(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .Include(p => p.BudgetLines)
            .Include(p => p.ChangeOrders)
            .Include(p => p.CostTransactions)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }
}

// --- DTOs ---
#pragma warning disable S6960

public class ProjectWipDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal ContractValue { get; set; }
    public decimal CostsToDate { get; set; }
    public decimal PercentComplete { get; set; }
    public decimal EarnedRevenue { get; set; }
    public decimal BilledToDate { get; set; }
    public decimal OverUnderBilling { get; set; }
    public decimal RetainageHeld { get; set; }
}

public class ForecastDto
{
    public Guid ProjectId { get; set; }
    public decimal BudgetAtCompletion { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal EstimateToComplete { get; set; }
    public decimal VarianceAtCompletion { get; set; }
    public decimal EarnedValue { get; set; }
    public decimal ActualCost { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal ScheduleVariance { get; set; }
    public decimal CostVariance { get; set; }
    public decimal SchedulePerformanceIndex { get; set; }
    public decimal CostPerformanceIndex { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class ProfitabilityDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Costs { get; set; }
    public decimal Margin { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal ContractValue { get; set; }
    public decimal RetainageHeld { get; set; }
}

public class BudgetVsActualDto
{
    public Guid TaskId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercent { get; set; }
}

public class UnbilledDto
{
    public Guid ProjectId { get; set; }
    public decimal EarnedRevenue { get; set; }
    public decimal BilledRevenue { get; set; }
    public decimal UnbilledAmount { get; set; }
    public decimal RetainageHeld { get; set; }
}

public class ChangeOrderSummaryDto
{
    public Guid ProjectId { get; set; }
    public decimal OriginalBudget { get; set; }
    public decimal ApprovedChangeOrders { get; set; }
    public decimal PendingChangeOrders { get; set; }
    public decimal RevisedBudget { get; set; }
    public int TotalChangeOrders { get; set; }
}

public class CostDetailDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Hours { get; set; }
    public decimal BillableAmount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
