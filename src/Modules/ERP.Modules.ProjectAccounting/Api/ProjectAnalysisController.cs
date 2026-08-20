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
    private readonly ERP.Modules.GeneralLedger.Infrastructure.GlDbContext _glContext;

    public ProjectAnalysisController(ProjDbContext context, ERP.Modules.GeneralLedger.Infrastructure.GlDbContext glContext)
    {
        _context = context;
        _glContext = glContext;
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

    /// <summary>Budget vs. committed vs. actual three-way view per task/category (§5.7).</summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Three-way rows: budget - committed (open PO) - actual = remaining.</returns>
    [HttpGet("budget-committed-actual")]
    public async Task<ActionResult<ApiResponse<List<BudgetCommittedActualDto>>>> GetBudgetCommittedActual(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await LoadProject(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var committed = await _context.ProjectCommittedCosts
            .Where(c => c.ProjectId == projectId && !c.IsReleased)
            .GroupBy(c => new { c.TaskId, c.Category })
            .Select(g => new { g.Key.TaskId, g.Key.Category, Committed = g.Sum(c => c.Amount) })
            .ToListAsync(cancellationToken);

        var committedByKey = committed.ToDictionary(c => (c.TaskId, c.Category), c => c.Committed);

        var rows = project.BudgetLines.Select(b =>
        {
            var key = (b.TaskId, b.Category);
            var committedAmt = committedByKey.GetValueOrDefault(key, 0m);
            var remaining = b.BudgetAmount - committedAmt - b.ActualAmount;
            return new BudgetCommittedActualDto
            {
                TaskId = b.TaskId,
                Category = b.Category.ToString(),
                Description = b.Description,
                BudgetAmount = b.BudgetAmount,
                CommittedAmount = committedAmt,
                ActualAmount = b.ActualAmount,
                Remaining = remaining,
            };
        }).ToList();

        return Ok(ApiResponse<List<BudgetCommittedActualDto>>.Success(rows));
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

    /// <summary>Contract asset/liability position (ASC 606 / construction accounting): contract asset = costs incurred − billings (unbilled costs), contract liability = billings − costs (deferred revenue).</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Costs incurred, billings to date, contract asset, contract liability.</returns>
    [HttpGet("contract-position")]
    public async Task<ActionResult<ApiResponse<ContractPositionDto>>> GetContractPosition(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var costsIncurred = project.CostsToDate;
        var billingsToDate = project.RevenueToDate;
        var net = costsIncurred - billingsToDate;
        var contractAsset = net > 0 ? net : 0;      // unbilled costs -> asset
        var contractLiability = net < 0 ? -net : 0;  // billings in excess of costs -> liability

        return Ok(ApiResponse<ContractPositionDto>.Success(new ContractPositionDto
        {
            ProjectId = projectId,
            CostsIncurred = costsIncurred,
            BillingsToDate = billingsToDate,
            ContractAsset = contractAsset,
            ContractLiability = contractLiability,
        }));
    }

    /// <summary>Cost-to-cost percent-complete measurement basis: costs incurred ÷ EAC (with physical % override).</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Cost-to-cost percent complete and effective percent complete.</returns>
    [HttpGet("cost-to-cost")]
    public async Task<ActionResult<ApiResponse<CostToCostDto>>> CostToCost(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var eac = project.EstimateAtCompletion > 0 ? project.EstimateAtCompletion : project.RevisedBudget;
        var costToCost = eac > 0 ? project.CostsToDate / eac * 100 : 0;

        return Ok(ApiResponse<CostToCostDto>.Success(new CostToCostDto
        {
            ProjectId = projectId,
            CostsToDate = project.CostsToDate,
            EstimateAtCompletion = eac,
            RevisedBudget = project.RevisedBudget,
            CostToCostPercent = costToCost,
            PhysicalPercent = project.PercentComplete,
            EffectivePercent = project.PercentComplete > 0 ? project.PercentComplete : costToCost,
        }));
    }

    /// <summary>Project-to-GL reconciliation gate: project ledger cost must equal GL postings for the project (net to zero variance).</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Project ledger cost, GL net posting, variance, and balance status.</returns>
    [HttpGet("reconcile")]
    public async Task<ActionResult<ApiResponse<ReconciliationDto>>> Reconcile(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.CostTransactions)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var projectLedgerCost = project.CostTransactions
            .Where(t => t.Status == TransactionStatus.Posted)
            .Sum(t => t.Amount);

        // GL postings that reference this project via the PROJECT segment (stored in SegmentsJson).
        var projectIdStr = projectId.ToString("D");
        var glLines = await _glContext.JournalEntryLines
            .Where(l => l.SegmentsJson != null && l.SegmentsJson.Contains(projectIdStr))
            .ToListAsync(cancellationToken);

        // The job-cost side is the GL debit (project costs posted to the job-cost account).
        var glNet = glLines.Sum(l => l.Debit);

        var variance = projectLedgerCost - glNet;

        return Ok(ApiResponse<ReconciliationDto>.Success(new ReconciliationDto
        {
            ProjectId = projectId,
            ProjectLedgerCost = projectLedgerCost,
            GlNetPosting = glNet,
            Variance = variance,
            IsBalanced = variance == 0,
            GlLineCount = glLines.Count,
        }));
    }

    /// <summary>Close-out checklist: retainage released, lien waivers collected, final invoice billed, unbilled = 0, budget variance explained.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Checklist items and overall pass status.</returns>
    [HttpGet("close-out-checklist")]
    public async Task<ActionResult<ApiResponse<CloseOutChecklistDto>>> CloseOutChecklist(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.CostTransactions)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var retainageReleased = project.RetainageHeld == 0;
        var unbilledZero = project.RevenueToDate >= (project.ContractValue ?? 0);
        var finalBilled = project.ContractValue.HasValue && project.RevenueToDate >= project.ContractValue.Value;

        var items = new List<ChecklistItemDto>
        {
            new () { Item = "Retainage released", Passed = retainageReleased },
            new () { Item = "Final invoice billed (revenue >= contract value)", Passed = finalBilled },
            new () { Item = "Unbilled revenue = 0 (revenue >= contract value)", Passed = unbilledZero },
            new () { Item = "Project completed", Passed = project.Status == ProjectStatus.Completed },
            new () { Item = "Billing hold cleared", Passed = !project.BillingHold },
        };

        return Ok(ApiResponse<CloseOutChecklistDto>.Success(new CloseOutChecklistDto
        {
            ProjectId = projectId,
            Items = items,
            AllPassed = items.TrueForAll(i => i.Passed),
            IsCloseOutComplete = project.IsCloseOutComplete,
        }));
    }

    /// <summary>Retainage aging: held subcontractor retainage payable by age bucket from subcontract execution date.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Retainage held by aging bucket and per-subcontract rows.</returns>
    [HttpGet("retainage-aging")]
    public async Task<ActionResult<ApiResponse<RetainageAgingDto>>> RetainageAging(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Subcontracts)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var asOf = DateTime.UtcNow;
        var rows = new List<RetainageAgingRowDto>();
        foreach (var sc in project.Subcontracts)
        {
            if (sc.RetainageHeld == 0)
                continue;
            var ageDays = (int)(asOf - sc.SubcontractDate).TotalDays;
            var bucket = ageDays switch
            {
                <= 30 => "0-30",
                <= 60 => "31-60",
                <= 90 => "61-90",
                _ => "90+",
            };

            rows.Add(new RetainageAgingRowDto
            {
                SubcontractId = sc.Id,
                SubcontractNumber = sc.SubcontractNumber,
                VendorId = sc.VendorId,
                RetainageHeld = sc.RetainageHeld,
                AgeDays = ageDays,
                Bucket = bucket,
            });
        }

        return Ok(ApiResponse<RetainageAgingDto>.Success(new RetainageAgingDto
        {
            ProjectId = projectId,
            TotalRetainageHeld = rows.Sum(r => r.RetainageHeld),
            Bucket0To30 = rows.Where(r => r.Bucket == "0-30").Sum(r => r.RetainageHeld),
            Bucket31To60 = rows.Where(r => r.Bucket == "31-60").Sum(r => r.RetainageHeld),
            Bucket61To90 = rows.Where(r => r.Bucket == "61-90").Sum(r => r.RetainageHeld),
            Bucket90Plus = rows.Where(r => r.Bucket == "90+").Sum(r => r.RetainageHeld),
            Rows = rows,
        }));
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

public class ContractPositionDto
{
    public Guid ProjectId { get; set; }
    public decimal CostsIncurred { get; set; }
    public decimal BillingsToDate { get; set; }
    public decimal ContractAsset { get; set; }
    public decimal ContractLiability { get; set; }
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

public class BudgetCommittedActualDto
{
    public Guid TaskId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Remaining { get; set; }
}

public class CostToCostDto
{
    public Guid ProjectId { get; set; }
    public decimal CostsToDate { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal RevisedBudget { get; set; }
    public decimal CostToCostPercent { get; set; }
    public decimal PhysicalPercent { get; set; }
    public decimal EffectivePercent { get; set; }
}

public class ReconciliationDto
{
    public Guid ProjectId { get; set; }
    public decimal ProjectLedgerCost { get; set; }
    public decimal GlNetPosting { get; set; }
    public decimal Variance { get; set; }
    public bool IsBalanced { get; set; }
    public int GlLineCount { get; set; }
}

#pragma warning disable CA1002, CA2227
public class CloseOutChecklistDto
{
    public Guid ProjectId { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = [];
    public bool AllPassed { get; set; }
    public bool IsCloseOutComplete { get; set; }
}
#pragma warning restore CA1002, CA2227

public class ChecklistItemDto
{
    public string Item { get; set; } = string.Empty;
    public bool Passed { get; set; }
}

#pragma warning disable CA1002, CA2227
public class RetainageAgingDto
{
    public Guid ProjectId { get; set; }
    public decimal TotalRetainageHeld { get; set; }
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }
    public List<RetainageAgingRowDto> Rows { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class RetainageAgingRowDto
{
    public Guid SubcontractId { get; set; }
    public string SubcontractNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public decimal RetainageHeld { get; set; }
    public int AgeDays { get; set; }
    public string Bucket { get; set; } = string.Empty;
}
