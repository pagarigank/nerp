// <copyright file="ProfitFadeController.cs" company="ERP Project">
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
[Route("api/v1/projects/analysis")]
public class ProfitFadeController : ControllerBase
{
    private readonly ProjDbContext _context;

    public ProfitFadeController(ProjDbContext context)
    {
        _context = context;
    }

    /// <summary>Profit-fade / EAC trend for one project (or all scoped projects): daily snapshots of BAC, EAC and
    /// estimated margin % against the original budget baseline.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="projectId">Optional single-project filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Snapshot series ordered by project code then capture date.</returns>
    [HttpGet("eac-trend")]
    public async Task<ActionResult<ApiResponse<List<EacTrendPointDto>>>> GetEacTrend(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken = default)
    {
        var snapshotQuery = _context.ProjectEacSnapshots
            .ApplyCompanyScope(HttpContext, s => s.CompanyId)
            .AsQueryable();
        if (projectId.HasValue)
            snapshotQuery = snapshotQuery.Where(s => s.ProjectId == projectId.Value);

        var snapshots = await snapshotQuery
            .OrderBy(s => s.CapturedOn)
            .ToListAsync(cancellationToken);

        var projectIds = snapshots.Select(s => s.ProjectId).Distinct().ToList();
        var projects = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var rows = snapshots.Select(s =>
        {
            projects.TryGetValue(s.ProjectId, out var project);
            return new EacTrendPointDto
            {
                ProjectId = s.ProjectId,
                ProjectCode = project?.ProjectCode ?? string.Empty,
                ProjectName = project?.Name ?? string.Empty,
                CapturedOn = s.CapturedOn,
                OriginalBudget = project?.OriginalBudget ?? 0,
                BudgetAtCompletion = s.BudgetAtCompletion,
                EstimateAtCompletion = s.EstimateAtCompletion,
                EstimatedMarginPct = s.EstimatedMarginPct,
                PendingChangeOrderAmount = s.PendingChangeOrderAmount,
            };
        })
        .OrderBy(r => r.ProjectCode)
        .ThenBy(r => r.CapturedOn)
        .ToList();

        return Ok(ApiResponse<List<EacTrendPointDto>>.Success(rows));
    }

    /// <summary>Portfolio profit-fade variant: average estimated margin % across all captured projects grouped by UTC capture date.</summary>
    /// <param name="companyId">Optional company scope filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Average margin per capture date, oldest first.</returns>
    [HttpGet("eac-trend/portfolio")]
    public async Task<ActionResult<ApiResponse<List<EacPortfolioPointDto>>>> GetPortfolioEacTrend(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await _context.ProjectEacSnapshots
            .ApplyCompanyScope(HttpContext, s => s.CompanyId)
            .ToListAsync(cancellationToken);

        var rows = snapshots
            .GroupBy(s => s.CapturedOn.UtcDateTime.Date)
            .Select(g => new EacPortfolioPointDto
            {
                CaptureDate = g.Key,
                ProjectCount = g.Count(),
                AverageEstimatedMarginPct = decimal.Round(g.Average(s => s.EstimatedMarginPct), 2),
                AverageEstimateAtCompletion = decimal.Round(g.Average(s => s.EstimateAtCompletion), 2),
            })
            .OrderBy(r => r.CaptureDate)
            .ToList();

        return Ok(ApiResponse<List<EacPortfolioPointDto>>.Success(rows));
    }
}

#pragma warning disable CA1002, CA2227
public class EacTrendPointDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTimeOffset CapturedOn { get; set; }
    public decimal OriginalBudget { get; set; }
    public decimal BudgetAtCompletion { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal EstimatedMarginPct { get; set; }
    public decimal? PendingChangeOrderAmount { get; set; }
}

public class EacPortfolioPointDto
{
    public DateTime CaptureDate { get; set; }
    public int ProjectCount { get; set; }
    public decimal AverageEstimatedMarginPct { get; set; }
    public decimal AverageEstimateAtCompletion { get; set; }
}
#pragma warning restore CA1002, CA2227
