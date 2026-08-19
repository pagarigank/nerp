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
