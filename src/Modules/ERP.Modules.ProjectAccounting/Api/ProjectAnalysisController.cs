// <copyright file="ProjectAnalysisController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
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

    /// <summary>Project cash-flow forecast: expected billings, remaining cost burn, and retainage expected
    /// to release across the remaining work — feeds the Cash Management forecast.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Expected billings, remaining cost-to-complete, retainage expected to release, net cash flow.</returns>
    [HttpGet("cash-flow-forecast")]
    public async Task<ActionResult<ApiResponse<CashFlowForecastDto>>> CashFlowForecast(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Subcontracts)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var contractValue = project.ContractValue ?? 0;
        var eac = project.EstimateAtCompletion > 0 ? project.EstimateAtCompletion : project.RevisedBudget;
        var remainingCost = eac - project.CostsToDate > 0 ? eac - project.CostsToDate : 0;
        var expectedBillings = (contractValue - project.RevenueToDate) > 0 ? contractValue - project.RevenueToDate : 0;
        var retainageHeld = project.Subcontracts.Sum(s => s.RetainageHeld);
        var netCashFlow = expectedBillings - remainingCost + retainageHeld;

        return Ok(ApiResponse<CashFlowForecastDto>.Success(new CashFlowForecastDto
        {
            ProjectId = projectId,
            ContractValue = contractValue,
            RevenueToDate = project.RevenueToDate,
            CostsToDate = project.CostsToDate,
            EstimateAtCompletion = eac,
            ExpectedBillings = expectedBillings,
            RemainingCostToComplete = remainingCost,
            RetainageHeld = retainageHeld,
            NetCashFlow = netCashFlow,
        }));
    }

    /// <summary>Period-end project close checklist: all costs posted, all time approved, billing up-to-date,
    /// reconciliation complete and project reviewed for close.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Period-end close checklist items and overall pass status.</returns>
    [HttpGet("period-end-close")]
    public async Task<ActionResult<ApiResponse<CloseOutChecklistDto>>> PeriodEndClose(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.CostTransactions)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        // Cost transactions currently in Draft (not yet posted) block period-end close.
        var allCostsPosted = !project.CostTransactions.Any(t => t.Status == TransactionStatus.Draft);
        var billingUpToDate = project.RevenueToDate >= 0; // billed at least as far as recognized
        var reconcileComplete = (project.EstimateAtCompletion > 0 ? project.CostsToDate / project.EstimateAtCompletion : 0) >= 0;

        var items = new List<ChecklistItemDto>
        {
            new () { Item = "All costs posted (no Draft cost transactions)", Passed = allCostsPosted },
            new () { Item = "Billing up-to-date", Passed = billingUpToDate },
            new () { Item = "Reconciliation complete (EAC set / cost complete)", Passed = reconcileComplete },
            new () { Item = "Project reviewed for close", Passed = project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.Closed },
        };

        return Ok(ApiResponse<CloseOutChecklistDto>.Success(new CloseOutChecklistDto
        {
            ProjectId = projectId,
            Items = items,
            AllPassed = items.TrueForAll(i => i.Passed),
            IsCloseOutComplete = project.IsCloseOutComplete,
        }));
    }

    /// <summary>Employee utilization: hours by project, billable %, and utilization % against a monthly capacity (default 160h),
    /// derived from posted labor cost transactions within an optional period.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="from">Optional period start (inclusive).</param>
    /// <param name="to">Optional period end (inclusive).</param>
    /// <param name="capacityHours">Monthly capacity hours used for utilization %.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One utilization row per employee with hours by project.</returns>
    [HttpGet("~/api/v1/projects/analysis/employee-utilization")]
    public async Task<ActionResult<ApiResponse<List<EmployeeUtilizationDto>>>> EmployeeUtilization(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] decimal capacityHours = 160m,
        CancellationToken cancellationToken = default)
    {
        var labor = await LoadLaborRows(companyId, from, to, cancellationToken);
        if (labor.Count == 0)
            return Ok(ApiResponse<List<EmployeeUtilizationDto>>.Success([]));

        var projects = await LoadProjectLookup(labor.Select(l => l.ProjectId).Distinct(), cancellationToken);
        var capacity = capacityHours > 0 ? capacityHours : 160m;

        var rows = labor
            .Where(l => l.EmployeeId.HasValue)
            .GroupBy(l => l.EmployeeId!.Value)
            .Select(g =>
            {
                var totalHours = g.Sum(x => x.Hours);
                var billableHours = g.Where(x => x.IsBillable).Sum(x => x.Hours);
                return new EmployeeUtilizationDto
                {
                    EmployeeId = g.Key.ToString("D"),
                    TotalHours = totalHours,
                    BillableHours = billableHours,
                    BillablePercent = totalHours > 0 ? billableHours / totalHours * 100 : 0,
                    CapacityHours = capacity,
                    UtilizationPercent = capacity > 0 ? totalHours / capacity * 100 : 0,
                    LaborCost = g.Sum(x => x.Amount),
                    Projects = g.GroupBy(x => x.ProjectId).Select(pg =>
                    {
                        projects.TryGetValue(pg.Key, out var info);
                        return new EmployeeProjectHoursDto
                        {
                            ProjectId = pg.Key,
                            ProjectCode = info?.Code ?? string.Empty,
                            ProjectName = info?.Name ?? string.Empty,
                            Hours = pg.Sum(h => h.Hours),
                            Amount = pg.Sum(h => h.Amount),
                        };
                    }).OrderByDescending(p => p.Hours).ToList(),
                };
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        return Ok(ApiResponse<List<EmployeeUtilizationDto>>.Success(rows));
    }

    /// <summary>Employee profitability: billed amount comes from allocator-produced BillableAmount on billed cost rows
    /// (plus unbilled billable shown separately), cost is the labor transaction amount; margin = billed + unbilled − cost.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="from">Optional period start (inclusive).</param>
    /// <param name="to">Optional period end (inclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One profitability row per employee.</returns>
    [HttpGet("~/api/v1/projects/analysis/employee-profitability")]
    public async Task<ActionResult<ApiResponse<List<EmployeeProfitabilityDto>>>> EmployeeProfitability(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var labor = await LoadLaborRows(companyId, from, to, cancellationToken);

        var rows = labor
            .Where(l => l.EmployeeId.HasValue)
            .GroupBy(l => l.EmployeeId!.Value)
            .Select(g =>
            {
                var cost = g.Sum(x => x.Amount);
                var billed = g.Where(x => x.IsBilled).Sum(x => x.BillableAmount);
                var unbilled = g.Where(x => !x.IsBilled).Sum(x => x.BillableAmount);
                var revenue = billed + unbilled;
                var margin = revenue - cost;
                return new EmployeeProfitabilityDto
                {
                    EmployeeId = g.Key.ToString("D"),
                    BilledAmount = billed,
                    UnbilledBillableAmount = unbilled,
                    CostAmount = cost,
                    Margin = margin,
                    MarginPercent = revenue > 0 ? margin / revenue * 100 : 0,
                };
            })
            .OrderByDescending(r => r.Margin)
            .ToList();

        return Ok(ApiResponse<List<EmployeeProfitabilityDto>>.Success(rows));
    }

    /// <summary>Subcontract status per subcontract: vendor, revised amount (contract + approved COs), invoiced-to-date,
    /// retainage held, and remaining balance.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="projectId">Optional parent project filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One status row per subcontract.</returns>
    [HttpGet("~/api/v1/projects/analysis/subcontract-status")]
    public async Task<ActionResult<ApiResponse<List<SubcontractStatusRowDto>>>> GetSubcontractStatus(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Subcontracts.ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId);
        if (projectId.HasValue)
            query = query.Where(s => s.ProjectId == projectId.Value);

        var subcontracts = await query
            .Include(s => s.ChangeOrders)
            .ToListAsync(cancellationToken);
        var projects = await LoadProjectLookup(subcontracts.Select(s => s.ProjectId).Distinct(), cancellationToken);

        var rows = subcontracts.Select(s =>
        {
            var approvedCos = s.ChangeOrders.Where(c => c.Status == SubcontractCoStatus.Approved).Sum(c => c.Amount);
            projects.TryGetValue(s.ProjectId, out var info);
            var revised = s.ContractAmount + approvedCos;
            return new SubcontractStatusRowDto
            {
                SubcontractId = s.Id,
                SubcontractNumber = s.SubcontractNumber,
                VendorId = s.VendorId,
                ProjectId = s.ProjectId,
                ProjectCode = info?.Code ?? string.Empty,
                ProjectName = info?.Name ?? string.Empty,
                Status = s.Status.ToString(),
                ContractAmount = s.ContractAmount,
                ApprovedChangeOrders = approvedCos,
                RevisedAmount = revised,
                InvoicedToDate = s.BilledToDate,
                RetainageHeld = s.RetainageHeld,
                Remaining = revised - s.BilledToDate,
            };
        }).OrderBy(r => r.ProjectCode).ThenBy(r => r.SubcontractNumber).ToList();

        return Ok(ApiResponse<List<SubcontractStatusRowDto>>.Success(rows));
    }

    /// <summary>Open subcontracts grouped by project with remaining commitment compared to the project budget remaining.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One commitment row per project with open subcontracts.</returns>
    [HttpGet("~/api/v1/projects/analysis/subcontract-commitment")]
    public async Task<ActionResult<ApiResponse<List<SubcontractCommitmentRowDto>>>> SubcontractCommitment(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var subcontracts = await _context.Subcontracts
            .ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId)
            .Include(s => s.ChangeOrders)
            .ToListAsync(cancellationToken);

        var openByProject = subcontracts
            .Where(s => s.Status == SubcontractStatus.Active && !s.IsClosed)
            .GroupBy(s => s.ProjectId)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                Committed = g.Sum(s => s.ContractAmount) + g.Sum(s => s.ChangeOrders.Where(c => c.Status == SubcontractCoStatus.Approved).Sum(c => c.Amount)),
                Invoiced = g.Sum(s => s.BilledToDate),
            });

        var projectIds = openByProject.Keys.ToList();
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var rows = openByProject.Keys.Select(pid =>
        {
            var open = openByProject[pid];
            projects.TryGetValue(pid, out var project);
            var budget = BudgetOf(project);
            return new SubcontractCommitmentRowDto
            {
                ProjectId = pid,
                ProjectCode = project?.ProjectCode ?? string.Empty,
                ProjectName = project?.Name ?? string.Empty,
                OpenSubcontractCount = open.Count,
                CommittedTotal = open.Committed,
                InvoicedAgainstCommitted = open.Invoiced,
                RemainingCommitment = open.Committed - open.Invoiced,
                ProjectBudgetRemaining = budget - (project?.CostsToDate ?? 0),
            };
        }).OrderBy(r => r.ProjectCode).ToList();

        return Ok(ApiResponse<List<SubcontractCommitmentRowDto>>.Success(rows));
    }

    /// <summary>Certified payroll (WH-347 style) rows from posted labor cost transactions grouped by employee + project for a period.
    /// Classification falls back to "Labor" when no assignment role exists; prevailing-rate compliance columns are omitted because
    /// no prevailing wage rate is stored in this module.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="from">Optional period start (defaults to first of month of <paramref name="to"/>).</param>
    /// <param name="to">Optional period end (defaults to now).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One payroll row per employee + project for the period.</returns>
    [HttpGet("~/api/v1/projects/analysis/certified-payroll")]
    public async Task<ActionResult<ApiResponse<List<CertifiedPayrollRowDto>>>> CertifiedPayroll(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var periodEnd = to ?? DateTime.UtcNow;
        var periodStart = from ?? new DateTime(periodEnd.Year, periodEnd.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var labor = await LoadLaborRows(companyId, periodStart, periodEnd, cancellationToken);
        if (labor.Count == 0)
            return Ok(ApiResponse<List<CertifiedPayrollRowDto>>.Success([]));

        var assignments = await _context.EmployeeProjectAssignments
            .ApplyCompanyScope(HttpContext, a => a.CompanyId, companyId)
            .ToListAsync(cancellationToken);
        var roleByKey = assignments
            .Where(a => a.IsActive && a.EffectiveFrom <= periodEnd && (a.EffectiveTo is null || a.EffectiveTo >= periodStart))
            .GroupBy(a => $"{a.EmployeeId}|{a.ProjectId}")
            .ToDictionary(g => g.Key, g => g.First().RoleName, StringComparer.OrdinalIgnoreCase);

        var projects = await LoadProjectLookup(labor.Select(l => l.ProjectId).Distinct(), cancellationToken);

        var rows = labor
            .Where(l => l.EmployeeId.HasValue)
            .GroupBy(l => (l.EmployeeId!.Value, l.ProjectId))
            .Select(g =>
            {
                var hours = g.Sum(x => x.Hours);
                var wages = g.Sum(x => x.Amount);
                roleByKey.TryGetValue($"{g.Key.Item1}|{g.Key.ProjectId}", out var role);
                projects.TryGetValue(g.Key.ProjectId, out var info);
                return new CertifiedPayrollRowDto
                {
                    EmployeeId = g.Key.Item1.ToString("D"),
                    ProjectId = g.Key.ProjectId,
                    ProjectCode = info?.Code ?? string.Empty,
                    Classification = role ?? "Labor",
                    Hours = hours,
                    WageAmount = wages,
                    HourlyRate = hours > 0 ? wages / hours : 0,
                };
            })
            .OrderBy(r => r.EmployeeId).ThenBy(r => r.ProjectCode)
            .ToList();

        return Ok(ApiResponse<List<CertifiedPayrollRowDto>>.Success(rows));
    }

    /// <summary>Portfolio dashboard for active projects: margin %, effective % complete (cost-to-cost basis), forecast EAC
    /// (same calculation as the forecast endpoint) and derived risk status.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One dashboard row per active or on-hold project.</returns>
    [HttpGet("~/api/v1/projects/analysis/portfolio-dashboard")]
    public async Task<ActionResult<ApiResponse<List<PortfolioDashboardRowDto>>>> PortfolioDashboard(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .Where(p => p.Status == ProjectStatus.Active || p.Status == ProjectStatus.OnHold)
            .ToListAsync(cancellationToken);

        var rows = projects.Select(p =>
        {
            var budget = BudgetOf(p);
            var eac = EstimateAtCompletionOf(p, budget);
            var marginPct = p.RevenueToDate > 0 ? (p.RevenueToDate - p.CostsToDate) / p.RevenueToDate * 100 : 0;
            var percentComplete = EffectivePercentOf(p, eac);
            var riskStatus = ClassifyRisk(marginPct, budget, eac);
            return new PortfolioDashboardRowDto
            {
                ProjectId = p.Id,
                ProjectCode = p.ProjectCode,
                Name = p.Name,
                ProjectManager = p.ProjectManager ?? string.Empty,
                ContractValue = p.ContractValue ?? 0,
                Revenue = p.RevenueToDate,
                Costs = p.CostsToDate,
                MarginPercent = marginPct,
                PercentComplete = percentComplete,
                Budget = budget,
                EstimateAtCompletion = eac,
                RiskStatus = riskStatus,
            };
        }).OrderByDescending(r => r.Costs).ToList();

        return Ok(ApiResponse<List<PortfolioDashboardRowDto>>.Success(rows));
    }

    /// <summary>Project aging: non-closed projects bucketed by status group with over-one-year flag and actionable flag
    /// (active AND over budget or negative margin).</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One aging row per non-closed project.</returns>
    [HttpGet("~/api/v1/projects/analysis/project-aging")]
    public async Task<ActionResult<ApiResponse<List<ProjectAgingRowDto>>>> ProjectAging(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .Where(p => p.Status != ProjectStatus.Closed)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var rows = projects.Select(p =>
        {
            var budget = BudgetOf(p);
            var eac = EstimateAtCompletionOf(p, budget);
            var overBudget = budget > 0 && eac > budget;
            var negativeMargin = p.RevenueToDate > 0 && p.CostsToDate > p.RevenueToDate;
            var anchor = p.PlannedStartDate ?? p.ActualStartDate ?? p.CreatedOn.UtcDateTime;
            var ageDays = Math.Max(0, (int)(now - new DateTimeOffset(anchor)).TotalDays);
            var statusGroup = p.Status switch
            {
                ProjectStatus.Planning => "Planning",
                ProjectStatus.OnHold => "On Hold",
                ProjectStatus.Completed => "Completed",
                _ => "Active",
            };
            return new ProjectAgingRowDto
            {
                ProjectId = p.Id,
                ProjectCode = p.ProjectCode,
                Name = p.Name,
                StatusGroup = statusGroup,
                AgeDays = ageDays,
                OverOneYear = ageDays > 365,
                OverBudget = overBudget,
                NegativeMargin = negativeMargin,
                Actionable = p.Status == ProjectStatus.Active && (overBudget || negativeMargin),
            };
        }).OrderByDescending(r => r.Actionable).ThenByDescending(r => r.AgeDays).ToList();

        return Ok(ApiResponse<List<ProjectAgingRowDto>>.Success(rows));
    }

    /// <summary>Contract value analysis grouped by project contract type: project count, total contract value and average
    /// realized margin % (averaged across projects that have recognized revenue).</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One analysis row per contract type.</returns>
    [HttpGet("~/api/v1/projects/analysis/contract-value-analysis")]
    public async Task<ActionResult<ApiResponse<List<ContractValueAnalysisRowDto>>>> ContractValueAnalysis(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .ToListAsync(cancellationToken);

        var rows = projects
            .GroupBy(p => p.ProjectType)
            .Select(g =>
            {
                var marginPcts = g.Where(p => p.RevenueToDate > 0)
                    .Select(p => (p.RevenueToDate - p.CostsToDate) / p.RevenueToDate * 100)
                    .ToList();
                return new ContractValueAnalysisRowDto
                {
                    ContractType = g.Key.ToString(),
                    ProjectCount = g.Count(),
                    TotalContractValue = g.Sum(p => p.ContractValue ?? 0),
                    AverageMarginPercent = marginPcts.Count > 0 ? marginPcts.Average() : 0,
                };
            })
            .OrderByDescending(r => r.TotalContractValue)
            .ToList();

        return Ok(ApiResponse<List<ContractValueAnalysisRowDto>>.Success(rows));
    }

    /// <summary>PM performance grouped by project manager: project counts, average margin %, on-budget % (EAC &lt;= budget)
    /// and on-time completion proxy (actual end within planned end where both dates exist).</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One performance row per project manager.</returns>
    [HttpGet("~/api/v1/projects/analysis/pm-performance")]
    public async Task<ActionResult<ApiResponse<List<PmPerformanceRowDto>>>> PmPerformance(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .ToListAsync(cancellationToken);

        var rows = projects
            .Where(p => !string.IsNullOrWhiteSpace(p.ProjectManager))
            .GroupBy(p => p.ProjectManager!)
            .Select(g =>
            {
                var onBudgetCount = g.Count(IsOnBudget);
                var completedWithSchedule = g.Count(p => p.ActualEndDate.HasValue && p.PlannedEndDate.HasValue);
                var onTimeCount = g.Count(p => p.ActualEndDate.HasValue && p.PlannedEndDate.HasValue && p.ActualEndDate <= p.PlannedEndDate);
                var marginPcts = g.Where(p => p.RevenueToDate > 0)
                    .Select(p => (p.RevenueToDate - p.CostsToDate) / p.RevenueToDate * 100)
                    .ToList();
                return new PmPerformanceRowDto
                {
                    ProjectManager = g.Key,
                    ProjectCount = g.Count(),
                    ActiveCount = g.Count(p => p.Status == ProjectStatus.Active),
                    CompletedOnTimeCount = onTimeCount,
                    CompletedWithScheduleCount = completedWithSchedule,
                    AverageMarginPercent = marginPcts.Count > 0 ? marginPcts.Average() : 0,
                    OnBudgetPercent = g.Any() ? onBudgetCount / (decimal)g.Count() * 100 : 0,
                };
            })
            .OrderByDescending(r => r.ProjectCount)
            .ToList();

        return Ok(ApiResponse<List<PmPerformanceRowDto>>.Success(rows));
    }

    /// <summary>Earned value per project: BCWS straight-line from planned dates (falls back to earned % of BAC when no schedule),
    /// BCWP = % complete × BAC, ACWP = actual costs, SV/CV/SPI/CPI and EAC = BAC ÷ CPI (division guarded).</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One earned-value row per project.</returns>
    [HttpGet("~/api/v1/projects/analysis/earned-value")]
    public async Task<ActionResult<ApiResponse<List<EarnedValueRowDto>>>> EarnedValue(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var rows = projects.Select(p =>
        {
            var bac = BudgetOf(p);
            var eac = EstimateAtCompletionOf(p, bac);
            var percentComplete = EffectivePercentOf(p, eac);
            var bcwp = bac * (percentComplete / 100m);
            decimal bcws;
            if (p.PlannedStartDate.HasValue && p.PlannedEndDate.HasValue && p.PlannedEndDate > p.PlannedStartDate)
            {
                var start = new DateTimeOffset(p.PlannedStartDate.Value);
                var end = new DateTimeOffset(p.PlannedEndDate.Value);
                var elapsed = (now - start).TotalDays;
                var span = (end - start).TotalDays;
                decimal fraction;
                if (elapsed <= 0)
                    fraction = 0;
                else if (elapsed >= span)
                    fraction = 1;
                else
                    fraction = (decimal)(elapsed / span);
                bcws = bac * fraction;
            }
            else
            {
                bcws = bcwp;
            }

            var acwp = p.CostsToDate;
            var sv = bcwp - bcws;
            var cv = bcwp - acwp;
            var spi = bcws > 0 ? bcwp / bcws : 0;
            var cpi = acwp > 0 ? bcwp / acwp : 0;
            return new EarnedValueRowDto
            {
                ProjectId = p.Id,
                ProjectCode = p.ProjectCode,
                Bac = bac,
                Bcws = bcws,
                Bcwp = bcwp,
                Acwp = acwp,
                Sv = sv,
                Cv = cv,
                Spi = spi,
                Cpi = cpi,
                Eac = cpi > 0 ? bac / cpi : bac,
            };
        }).OrderBy(r => r.ProjectCode).ToList();

        return Ok(ApiResponse<List<EarnedValueRowDto>>.Success(rows));
    }

    /// <summary>Pending change order impact per project: approved vs pending CO value and the effect on contract value and
    /// projected margin if the pending COs were approved (projection assumes the pending value converts to revenue at the
    /// current cost baseline — GAAP view shows both excluding and including).</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One impact row per project.</returns>
    [HttpGet("~/api/v1/projects/analysis/pending-co-impact")]
    public async Task<ActionResult<ApiResponse<List<PendingCoImpactRowDto>>>> PendingCoImpact(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .Include(p => p.ChangeOrders)
            .ToListAsync(cancellationToken);

        var rows = projects.Select(p =>
        {
            var approved = p.ChangeOrders.Where(c => c.Status == ChangeOrderStatus.Approved || c.Status == ChangeOrderStatus.Executed).Sum(c => c.Amount);
            var pending = p.ChangeOrders.Where(c => c.Status == ChangeOrderStatus.Draft || c.Status == ChangeOrderStatus.Submitted).Sum(c => c.Amount);
            var contractValue = p.ContractValue ?? 0;
            var budget = BudgetOf(p);
            var eac = EstimateAtCompletionOf(p, budget);
            var projectedRevenue = p.RevenueToDate + pending;
            var projectedMargin = projectedRevenue - eac;
            return new PendingCoImpactRowDto
            {
                ProjectId = p.Id,
                ProjectCode = p.ProjectCode,
                ContractValueExcludingPending = contractValue,
                ApprovedChangeOrders = approved,
                PendingChangeOrders = pending,
                ContractValueIncludingPending = contractValue + pending,
                EstimateAtCompletion = eac,
                ProjectedRevenueIncludingPending = projectedRevenue,
                ProjectedMargin = projectedMargin,
                ProjectedMarginPercent = projectedRevenue > 0 ? projectedMargin / projectedRevenue * 100 : 0,
            };
        }).OrderByDescending(r => r.PendingChangeOrders).ToList();

        return Ok(ApiResponse<List<PendingCoImpactRowDto>>.Success(rows));
    }

    /// <summary>Lien waiver register: every waiver joined to its subcontract (vendor, invoiced-to-date) and parent project.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One register row per lien waiver.</returns>
    [HttpGet("~/api/v1/projects/analysis/lien-waiver-register")]
    public async Task<ActionResult<ApiResponse<List<LienWaiverRegisterRowDto>>>> LienWaiverRegister(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var subcontracts = await _context.Subcontracts
            .ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId)
            .Include(s => s.LienWaivers)
            .ToListAsync(cancellationToken);
        var projects = await LoadProjectLookup(subcontracts.Select(s => s.ProjectId).Distinct(), cancellationToken);

        var rows = subcontracts
            .SelectMany(s => s.LienWaivers.Select(w =>
            {
                projects.TryGetValue(s.ProjectId, out var info);
                return new LienWaiverRegisterRowDto
                {
                    WaiverId = w.Id,
                    SubcontractId = s.Id,
                    SubcontractNumber = s.SubcontractNumber,
                    VendorId = s.VendorId,
                    ProjectId = s.ProjectId,
                    ProjectCode = info?.Code ?? string.Empty,
                    ProjectName = info?.Name ?? string.Empty,
                    WaiverType = w.WaiverType,
                    IsFinal = w.IsFinal,
                    EffectiveDate = w.EffectiveDate,
                    Amount = w.Amount,
                    Description = w.Description,
                };
            }))
            .OrderByDescending(r => r.EffectiveDate)
            .ToList();

        return Ok(ApiResponse<List<LienWaiverRegisterRowDto>>.Success(rows));
    }

    /// <summary>Contract asset/liability per project: unbilled revenue (earned − billed, same basis as the unbilled endpoint)
    /// versus billings in excess of earned revenue, classified as Asset, Liability or Settled.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One classification row per project.</returns>
    [HttpGet("~/api/v1/projects/analysis/contract-asset-liability")]
    public async Task<ActionResult<ApiResponse<List<ContractAssetLiabilityRowDto>>>> ContractAssetLiability(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId)
            .ToListAsync(cancellationToken);

        var rows = projects.Select(p =>
        {
            var earned = EarnedRevenueOf(p);
            var net = earned - p.RevenueToDate;
            var asset = net > 0 ? net : 0;
            var liability = net < 0 ? -net : 0;
            string classification;
            if (asset > 0)
                classification = "Asset";
            else if (liability > 0)
                classification = "Liability";
            else
                classification = "Settled";
            return new ContractAssetLiabilityRowDto
            {
                ProjectId = p.Id,
                ProjectCode = p.ProjectCode,
                Name = p.Name,
                EarnedRevenue = earned,
                BilledRevenue = p.RevenueToDate,
                ContractAsset = asset,
                ContractLiability = liability,
                Classification = classification,
            };
        }).OrderBy(r => r.ProjectCode).ToList();

        return Ok(ApiResponse<List<ContractAssetLiabilityRowDto>>.Success(rows));
    }

    private static decimal BudgetOf(Project? project)
    {
        if (project is null)
            return 0;
        if (project.RevisedBudget > 0)
            return project.RevisedBudget;
        if (project.OriginalBudget > 0)
            return project.OriginalBudget;
        return project.ContractValue ?? 0;
    }

    private static decimal EstimateAtCompletionOf(Project project, decimal budget)
    {
        if (project.EstimateAtCompletion > 0)
            return project.EstimateAtCompletion;
        if (project.PercentComplete > 0)
            return project.CostsToDate / (project.PercentComplete / 100m);
        return budget;
    }

    private static decimal EffectivePercentOf(Project project, decimal eac)
    {
        if (project.PercentComplete > 0)
            return project.PercentComplete;
        return eac > 0 ? project.CostsToDate / eac * 100 : 0;
    }

    private static decimal EarnedRevenueOf(Project project)
        => project.ContractValue.HasValue
            ? project.ContractValue.Value * (project.PercentComplete / 100m)
            : project.CostsToDate;

    private static string ClassifyRisk(decimal marginPercent, decimal budget, decimal estimateAtCompletion)
    {
        if (marginPercent < 0)
            return "Negative Margin";
        if (budget > 0 && estimateAtCompletion > budget * 1.1m)
            return "Over Budget Risk";
        return "On Track";
    }

    private static bool IsOnBudget(Project project)
    {
        var budget = BudgetOf(project);
        return !(budget > 0 && EstimateAtCompletionOf(project, budget) > budget);
    }

    private async Task<List<LaborRow>> LoadLaborRows(Guid? companyId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var query = _context.CostTransactions
            .ApplyCompanyScope(HttpContext, t => t.CompanyId, companyId)
            .Where(t => t.Category == CostCategory.Labor && t.Status == TransactionStatus.Posted);
        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value);

        return await query
            .Select(t => new LaborRow(t.EmployeeId, t.ProjectId, t.Hours, t.Amount, t.IsBillable, t.IsBilled, t.BillableAmount))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, ProjectInfo>> LoadProjectLookup(IEnumerable<Guid> projectIds, CancellationToken cancellationToken)
    {
        var ids = projectIds.ToList();
        return await _context.Projects
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProjectInfo(p.Id, p.ProjectCode, p.Name))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }
}

internal sealed record LaborRow(
    Guid? EmployeeId,
    Guid ProjectId,
    decimal Hours,
    decimal Amount,
    bool IsBillable,
    bool IsBilled,
    decimal BillableAmount);

internal sealed record ProjectInfo(Guid Id, string Code, string Name);

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

public class CashFlowForecastDto
{
    public Guid ProjectId { get; set; }
    public decimal ContractValue { get; set; }
    public decimal RevenueToDate { get; set; }
    public decimal CostsToDate { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal ExpectedBillings { get; set; }
    public decimal RemainingCostToComplete { get; set; }
    public decimal RetainageHeld { get; set; }
    public decimal NetCashFlow { get; set; }
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

#pragma warning disable CA1002, CA2227
public class EmployeeUtilizationDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal BillableHours { get; set; }
    public decimal BillablePercent { get; set; }
    public decimal CapacityHours { get; set; }
    public decimal UtilizationPercent { get; set; }
    public decimal LaborCost { get; set; }
    public List<EmployeeProjectHoursDto> Projects { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class EmployeeProjectHoursDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Amount { get; set; }
}

public class EmployeeProfitabilityDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public decimal BilledAmount { get; set; }
    public decimal UnbilledBillableAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal Margin { get; set; }
    public decimal MarginPercent { get; set; }
}

public class SubcontractStatusRowDto
{
    public Guid SubcontractId { get; set; }
    public string SubcontractNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ContractAmount { get; set; }
    public decimal ApprovedChangeOrders { get; set; }
    public decimal RevisedAmount { get; set; }
    public decimal InvoicedToDate { get; set; }
    public decimal RetainageHeld { get; set; }
    public decimal Remaining { get; set; }
}

public class SubcontractCommitmentRowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int OpenSubcontractCount { get; set; }
    public decimal CommittedTotal { get; set; }
    public decimal InvoicedAgainstCommitted { get; set; }
    public decimal RemainingCommitment { get; set; }
    public decimal ProjectBudgetRemaining { get; set; }
}

public class CertifiedPayrollRowDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal WageAmount { get; set; }
    public decimal HourlyRate { get; set; }
}

public class PortfolioDashboardRowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProjectManager { get; set; } = string.Empty;
    public decimal ContractValue { get; set; }
    public decimal Revenue { get; set; }
    public decimal Costs { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal PercentComplete { get; set; }
    public decimal Budget { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public string RiskStatus { get; set; } = string.Empty;
}

public class ProjectAgingRowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StatusGroup { get; set; } = string.Empty;
    public int AgeDays { get; set; }
    public bool OverOneYear { get; set; }
    public bool OverBudget { get; set; }
    public bool NegativeMargin { get; set; }
    public bool Actionable { get; set; }
}

public class ContractValueAnalysisRowDto
{
    public string ContractType { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public decimal TotalContractValue { get; set; }
    public decimal AverageMarginPercent { get; set; }
}

public class PmPerformanceRowDto
{
    public string ProjectManager { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedOnTimeCount { get; set; }
    public int CompletedWithScheduleCount { get; set; }
    public decimal AverageMarginPercent { get; set; }
    public decimal OnBudgetPercent { get; set; }
}

public class EarnedValueRowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public decimal Bac { get; set; }
    public decimal Bcws { get; set; }
    public decimal Bcwp { get; set; }
    public decimal Acwp { get; set; }
    public decimal Sv { get; set; }
    public decimal Cv { get; set; }
    public decimal Spi { get; set; }
    public decimal Cpi { get; set; }
    public decimal Eac { get; set; }
}

public class PendingCoImpactRowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public decimal ContractValueExcludingPending { get; set; }
    public decimal ApprovedChangeOrders { get; set; }
    public decimal PendingChangeOrders { get; set; }
    public decimal ContractValueIncludingPending { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal ProjectedRevenueIncludingPending { get; set; }
    public decimal ProjectedMargin { get; set; }
    public decimal ProjectedMarginPercent { get; set; }
}

public class LienWaiverRegisterRowDto
{
    public Guid WaiverId { get; set; }
    public Guid SubcontractId { get; set; }
    public string SubcontractNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string WaiverType { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class ContractAssetLiabilityRowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal EarnedRevenue { get; set; }
    public decimal BilledRevenue { get; set; }
    public decimal ContractAsset { get; set; }
    public decimal ContractLiability { get; set; }
    public string Classification { get; set; } = string.Empty;
}
