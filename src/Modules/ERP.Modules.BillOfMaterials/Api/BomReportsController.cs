// <copyright file="BomReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvInventoryContext = ERP.Modules.Inventory.Infrastructure.InventoryDbContext;

namespace ERP.Modules.BillOfMaterials.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/bom/reports")]
public class BomReportsController : ControllerBase
{
    private readonly BomDbContext _bomContext;
    private readonly InvInventoryContext _invContext;

    public BomReportsController(BomDbContext bomContext, InvInventoryContext invContext)
    {
        _bomContext = bomContext;
        _invContext = invContext;
    }

    /// <summary>
    /// Gets single-level BOM listing: parent item + immediate components with qty and cost.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of BOM listings.</returns>
    [HttpGet("listing")]
    public async Task<ActionResult<ApiResponse<List<BomListingDto>>>> GetListing(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _bomContext.BomHeaders
            .Include(h => h.Components)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(h => h.CompanyId == companyId.Value);
        }

        var headers = await query.OrderBy(h => h.ParentItemId).ToListAsync(cancellationToken);
        var result = headers.Select(header => new BomListingDto
        {
            BomHeaderId = header.Id,
            ParentItemId = header.ParentItemId,
            Revision = header.Revision,
            Status = header.Status.ToString(),
            YieldPercentage = header.YieldPercentage,
            Components = header.Components.Select(c => new BomListingLineDto
            {
                ComponentItemId = c.ComponentItemId,
                QuantityPerParent = c.QuantityPerParent,
                EffectiveQuantity = c.EffectiveQuantity,
                UnitOfMeasure = c.UnitOfMeasure,
                ScrapFactor = c.ScrapFactor,
                IsCritical = c.IsCritical,
            }).ToList(),
        }).ToList();

        return Ok(ApiResponse<List<BomListingDto>>.Success(result));
    }

    /// <summary>
    /// Gets build transaction history: all builds for a given parent item.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="parentItemId">Filter by parent item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of build history entries.</returns>
    [HttpGet("build-history")]
    public async Task<ActionResult<ApiResponse<List<BuildHistoryDto>>>> GetBuildHistory(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? parentItemId,
        CancellationToken cancellationToken)
    {
        var query = _bomContext.BuildOrders
            .Include(b => b.Lines)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        if (parentItemId.HasValue)
        {
            query = query.Where(b => b.ParentItemId == parentItemId.Value);
        }

        var orders = await query.OrderByDescending(b => b.BuildDate).ToListAsync(cancellationToken);
        var dtos = orders.Select(b => new BuildHistoryDto
        {
            BuildOrderId = b.Id,
            BuildNumber = b.BuildNumber,
            TransactionType = b.TransactionType.ToString(),
            ParentItemId = b.ParentItemId,
            QuantityBuilt = b.QuantityToBuild,
            ActualYield = b.ActualYield,
            YieldPercentage = b.ActualYield.HasValue && b.QuantityToBuild > 0
                ? (b.ActualYield.Value / b.QuantityToBuild) * 100
                : null,
            TotalCost = b.TotalCost,
            UnitCost = b.UnitCost,
            Status = b.Status.ToString(),
            BuildDate = b.BuildDate,
            ComponentCount = b.Lines.Count(l => !l.IsLabor && !l.IsOverhead),
            TotalScrapCost = b.Lines.Where(l => !l.IsLabor && !l.IsOverhead)
                .Sum(l => l.VarianceCost ?? 0),
        }).ToList();

        return Ok(ApiResponse<List<BuildHistoryDto>>.Success(dtos));
    }

    /// <summary>
    /// Gets BOM accuracy report: items with issues.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of BOM accuracy issues.</returns>
    [HttpGet("accuracy")]
    public async Task<ActionResult<ApiResponse<List<BomAccuracyDto>>>> GetAccuracy(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _bomContext.BomHeaders
            .Include(h => h.Components)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(h => h.CompanyId == companyId.Value);
        }

        var headers = await query.ToListAsync(cancellationToken);
        var itemIds = headers.Select(h => h.ParentItemId)
            .Concat(headers.SelectMany(h => h.Components.Select(comp => comp.ComponentItemId)))
            .Distinct()
            .ToList();

        var items = await _invContext.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var result = new List<BomAccuracyDto>();

        foreach (var header in headers)
        {
            var issues = new List<string>();

#pragma warning disable S3267 // Loop has conditional logic — cannot be simplified to Select

            // Check for inactive components
            foreach (var comp in header.Components)
            {
                if (items.TryGetValue(comp.ComponentItemId, out var compItem))
                {
                    if (compItem.Status != Inventory.Domain.Entities.ItemStatus.Active)
                    {
                        issues.Add($"Component {compItem.ItemCode} is {compItem.Status}");
                    }

                    if (compItem.StandardCost is null or 0)
                    {
                        issues.Add($"Component {compItem.ItemCode} has no standard cost");
                    }
                }
                else
                {
                    issues.Add($"Component item {comp.ComponentItemId} not found");
                }
            }
#pragma warning restore S3267

            if (issues.Count > 0)
            {
                result.Add(new BomAccuracyDto
                {
                    BomHeaderId = header.Id,
                    ParentItemId = header.ParentItemId,
                    Revision = header.Revision,
                    Status = header.Status.ToString(),
                    IssueCount = issues.Count,
                    Issues = issues,
                });
            }
        }

        return Ok(ApiResponse<List<BomAccuracyDto>>.Success(result));
    }

    /// <summary>
    /// Component Shortage Report: planned builds with missing components, qty short, impact.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of component shortage entries.</returns>
    [HttpGet("component-shortage")]
    public async Task<ActionResult<ApiResponse<List<ComponentShortageDto>>>> GetComponentShortage(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var plannedBuilds = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .Where(b => b.Status == BuildOrderStatus.Planned && (!companyId.HasValue || b.CompanyId == companyId.Value))
            .ToListAsync(cancellationToken);

        var result = new List<ComponentShortageDto>();
        foreach (var bo in plannedBuilds)
        {
            foreach (var line in bo.Lines.Where(l => !l.IsLabor && !l.IsOverhead))
            {
                var onHand = await _invContext.ItemStocks
                    .Where(s => s.ItemId == line.ComponentItemId)
                    .SumAsync(s => s.OnHandQuantity, cancellationToken);

                var required = line.QuantityRequired * bo.QuantityToBuild;
                var shortQty = required - onHand;
                if (shortQty > 0)
                {
                    result.Add(new ComponentShortageDto
                    {
                        BuildOrderId = bo.Id,
                        BuildNumber = bo.BuildNumber,
                        ParentItemId = bo.ParentItemId,
                        ComponentItemId = line.ComponentItemId,
                        RequiredQuantity = required,
                        OnHandQuantity = onHand,
                        ShortQuantity = shortQty,
                        UnitOfMeasure = line.UnitOfMeasure,
                    });
                }
            }
        }

        return Ok(ApiResponse<List<ComponentShortageDto>>.Success(result));
    }

    /// <summary>
    /// BOM Revision History Report: all revisions for an item.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of revision history entries.</returns>
    [HttpGet("revision-history")]
    public async Task<ActionResult<ApiResponse<List<RevisionHistoryDto>>>> GetRevisionHistory(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var query = _bomContext.BomRevisionHistories.AsQueryable();
        if (companyId.HasValue)
        {
            var headerIds = await _bomContext.BomHeaders
                .Where(h => h.CompanyId == companyId.Value)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(r => headerIds.Contains(r.BomHeaderId));
        }

        var rows = await query.OrderBy(r => r.BomHeaderId).ThenByDescending(r => r.CreatedOn)
            .Select(r => new RevisionHistoryDto
            {
                BomHeaderId = r.BomHeaderId,
                Revision = r.Revision,
                ChangeDescription = r.ChangeDescription,
                ReasonForChange = r.ReasonForChange,
                EffectiveDate = r.EffectiveDate,
                ChangedOn = r.CreatedOn.DateTime,
            }).ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<RevisionHistoryDto>>.Success(rows));
    }

    /// <summary>
    /// Build Variance Report: actual component consumption vs. standard, scrap %, cost variance.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of build variance entries.</returns>
    [HttpGet("build-variance")]
    public async Task<ActionResult<ApiResponse<List<BuildVarianceDto>>>> GetBuildVariance(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var builds = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .Where(b => b.Status == BuildOrderStatus.Completed && (!companyId.HasValue || b.CompanyId == companyId.Value))
            .ToListAsync(cancellationToken);

        var result = builds.Select(b => new BuildVarianceDto
        {
            BuildOrderId = b.Id,
            BuildNumber = b.BuildNumber,
            ParentItemId = b.ParentItemId,
            QuantityBuilt = b.QuantityToBuild,
            ActualYield = b.ActualYield,
            TotalStandardCost = b.Lines.Sum(l => l.ExtendedCost),
            TotalVarianceCost = b.Lines.Sum(l => l.VarianceCost ?? 0),
            ComponentLineCount = b.Lines.Count(l => !l.IsLabor && !l.IsOverhead),
        }).ToList();

        return Ok(ApiResponse<List<BuildVarianceDto>>.Success(result));
    }

    /// <summary>
    /// Work Center Utilization / Build Capacity Report.
    /// </summary>
    /// <param name="companyId">Filter by company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of work center utilization entries.</returns>
    [HttpGet("work-center-utilization")]
    public async Task<ActionResult<ApiResponse<List<WorkCenterUtilizationDto>>>> GetWorkCenterUtilization(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var wcs = await _bomContext.WorkCenters
            .Where(w => !companyId.HasValue || w.CompanyId == companyId.Value)
            .ToListAsync(cancellationToken);

        var util = new List<WorkCenterUtilizationDto>();
        foreach (var wc in wcs)
        {
            var lines = await _bomContext.BomComponentLines
                .Where(c => c.WorkCenterId == wc.Id)
                .ToListAsync(cancellationToken);

            var capacity = wc.CapacityHoursPerDay * 20m; // ~20 working days/month
            var estHours = lines.Sum(c => c.OperationSequence > 0 ? (decimal)c.OperationSequence / 10m : 1m) * wc.EfficiencyPercentage / 100m;
            var utilizationPct = capacity > 0 ? Math.Min(100m, (estHours / capacity) * 100m) : 0m;

            util.Add(new WorkCenterUtilizationDto
            {
                WorkCenterId = wc.Id,
                Code = wc.Code,
                Name = wc.Name,
                CapacityHoursPerMonth = capacity,
                PlannedHours = estHours,
                UtilizationPercentage = utilizationPct,
                ComponentCount = lines.Count,
                CostRatePerHour = wc.CostRatePerHour,
            });
        }

        return Ok(ApiResponse<List<WorkCenterUtilizationDto>>.Success(util));
    }
}

// --- DTOs ---
#pragma warning disable S6960

public class BomListingDto
{
    public Guid BomHeaderId { get; set; }
    public Guid ParentItemId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal YieldPercentage { get; set; }
#pragma warning disable CA1002, CA2227
    public List<BomListingLineDto> Components { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

public class BomListingLineDto
{
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerParent { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal ScrapFactor { get; set; }
    public bool IsCritical { get; set; }
}

public class BuildHistoryDto
{
    public Guid BuildOrderId { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public Guid ParentItemId { get; set; }
    public decimal QuantityBuilt { get; set; }
    public decimal? ActualYield { get; set; }
    public decimal? YieldPercentage { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? UnitCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime BuildDate { get; set; }
    public int ComponentCount { get; set; }
    public decimal TotalScrapCost { get; set; }
}

public class BomAccuracyDto
{
    public Guid BomHeaderId { get; set; }
    public Guid ParentItemId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int IssueCount { get; set; }
#pragma warning disable CA1002, CA2227
    public List<string> Issues { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

// --- New GAP report DTOs ---
public class ComponentShortageDto
{
    public Guid BuildOrderId { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public Guid ParentItemId { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ShortQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}

public class RevisionHistoryDto
{
    public Guid BomHeaderId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public string? ReasonForChange { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime ChangedOn { get; set; }
}

public class BuildVarianceDto
{
    public Guid BuildOrderId { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public Guid ParentItemId { get; set; }
    public decimal QuantityBuilt { get; set; }
    public decimal? ActualYield { get; set; }
    public decimal TotalStandardCost { get; set; }
    public decimal TotalVarianceCost { get; set; }
    public int ComponentLineCount { get; set; }
}

public class WorkCenterUtilizationDto
{
    public Guid WorkCenterId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CapacityHoursPerMonth { get; set; }
    public decimal PlannedHours { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public int ComponentCount { get; set; }
    public decimal CostRatePerHour { get; set; }
}
