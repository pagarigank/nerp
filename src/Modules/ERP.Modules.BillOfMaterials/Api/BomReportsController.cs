// <copyright file="BomReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
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
        _bomContext = bomContext ?? throw new ArgumentNullException(nameof(bomContext));
        _invContext = invContext ?? throw new ArgumentNullException(nameof(invContext));
    }

    // ---- Data-fetching helpers (shared by JSON + PDF endpoints) ----

    private async Task<List<BomListingDto>> GetListingDataAsync(Guid? companyId, CancellationToken ct)
    {
        var query = _bomContext.BomHeaders
            .Include(h => h.Components)
            .AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, h => h.CompanyId, companyId);

        var headers = await query.OrderBy(h => h.ParentItemId).ToListAsync(ct);
        return headers.Select(header => new BomListingDto
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
    }

    private async Task<List<BuildHistoryDto>> GetBuildHistoryDataAsync(Guid? companyId, Guid? parentItemId, CancellationToken ct)
    {
        var query = _bomContext.BuildOrders
            .Include(b => b.Lines)
            .AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, b => b.CompanyId, companyId);

        if (parentItemId.HasValue)
        {
            query = query.Where(b => b.ParentItemId == parentItemId.Value);
        }

        var orders = await query.OrderByDescending(b => b.BuildDate).ToListAsync(ct);
        return orders.Select(b => new BuildHistoryDto
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
    }

    private async Task<List<BomAccuracyDto>> GetAccuracyDataAsync(Guid? companyId, CancellationToken ct)
    {
        var query = _bomContext.BomHeaders
            .Include(h => h.Components)
            .AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, h => h.CompanyId, companyId);

        var headers = await query.ToListAsync(ct);
        var itemIds = headers.Select(h => h.ParentItemId)
            .Concat(headers.SelectMany(h => h.Components.Select(comp => comp.ComponentItemId)))
            .Distinct()
            .ToList();

        var items = await _invContext.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

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

        return result;
    }

    private async Task<List<ComponentShortageDto>> GetComponentShortageDataAsync(Guid? companyId, CancellationToken ct)
    {
        var plannedBuilds = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .Where(b => b.Status == BuildOrderStatus.Planned && (!companyId.HasValue || b.CompanyId == companyId.Value))
            .ToListAsync(ct);

        var result = new List<ComponentShortageDto>();
        foreach (var bo in plannedBuilds)
        {
            foreach (var line in bo.Lines.Where(l => !l.IsLabor && !l.IsOverhead))
            {
                var onHand = await _invContext.ItemStocks
                    .Where(s => s.ItemId == line.ComponentItemId)
                    .SumAsync(s => s.OnHandQuantity, ct);

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

        return result;
    }

    private async Task<List<RevisionHistoryDto>> GetRevisionHistoryDataAsync(Guid? companyId, CancellationToken ct)
    {
        var query = _bomContext.BomRevisionHistories.AsQueryable();
        if (companyId.HasValue)
        {
            var headerIds = await _bomContext.BomHeaders
                .ApplyCompanyScope(HttpContext, h => h.CompanyId, companyId)
                .Select(h => h.Id)
                .ToListAsync(ct);
            query = query.Where(r => headerIds.Contains(r.BomHeaderId));
        }

        return await query.OrderBy(r => r.BomHeaderId).ThenByDescending(r => r.CreatedOn)
            .Select(r => new RevisionHistoryDto
            {
                BomHeaderId = r.BomHeaderId,
                Revision = r.Revision,
                ChangeDescription = r.ChangeDescription,
                ReasonForChange = r.ReasonForChange,
                EffectiveDate = r.EffectiveDate,
                ChangedOn = r.CreatedOn.DateTime,
            }).ToListAsync(ct);
    }

    private async Task<List<BuildVarianceDto>> GetBuildVarianceDataAsync(Guid? companyId, CancellationToken ct)
    {
        var builds = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .Where(b => b.Status == BuildOrderStatus.Completed && (!companyId.HasValue || b.CompanyId == companyId.Value))
            .ToListAsync(ct);

        return builds.Select(b => new BuildVarianceDto
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
    }

    private async Task<List<WorkCenterUtilizationDto>> GetWorkCenterUtilizationDataAsync(Guid? companyId, CancellationToken ct)
    {
        var wcs = await _bomContext.WorkCenters
            .Where(w => !companyId.HasValue || w.CompanyId == companyId.Value)
            .ToListAsync(ct);

        var util = new List<WorkCenterUtilizationDto>();
        foreach (var wc in wcs)
        {
            var lines = await _bomContext.BomComponentLines
                .Where(c => c.WorkCenterId == wc.Id)
                .ToListAsync(ct);

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

        return util;
    }

    // ---- JSON endpoints ----

    [HttpGet("listing")]
    public async Task<ActionResult<ApiResponse<List<BomListingDto>>>> GetListing(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var data = await GetListingDataAsync(companyId, cancellationToken);
        return Ok(ApiResponse<List<BomListingDto>>.Success(data));
    }

    [HttpGet("build-history")]
    public async Task<ActionResult<ApiResponse<List<BuildHistoryDto>>>> GetBuildHistory(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? parentItemId,
        CancellationToken cancellationToken)
    {
        var data = await GetBuildHistoryDataAsync(companyId, parentItemId, cancellationToken);
        return Ok(ApiResponse<List<BuildHistoryDto>>.Success(data));
    }

    [HttpGet("accuracy")]
    public async Task<ActionResult<ApiResponse<List<BomAccuracyDto>>>> GetAccuracy(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var data = await GetAccuracyDataAsync(companyId, cancellationToken);
        return Ok(ApiResponse<List<BomAccuracyDto>>.Success(data));
    }

    [HttpGet("component-shortage")]
    public async Task<ActionResult<ApiResponse<List<ComponentShortageDto>>>> GetComponentShortage(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var data = await GetComponentShortageDataAsync(companyId, cancellationToken);
        return Ok(ApiResponse<List<ComponentShortageDto>>.Success(data));
    }

    [HttpGet("revision-history")]
    public async Task<ActionResult<ApiResponse<List<RevisionHistoryDto>>>> GetRevisionHistory(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var data = await GetRevisionHistoryDataAsync(companyId, cancellationToken);
        return Ok(ApiResponse<List<RevisionHistoryDto>>.Success(data));
    }

    [HttpGet("build-variance")]
    public async Task<ActionResult<ApiResponse<List<BuildVarianceDto>>>> GetBuildVariance(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var data = await GetBuildVarianceDataAsync(companyId, cancellationToken);
        return Ok(ApiResponse<List<BuildVarianceDto>>.Success(data));
    }

    [HttpGet("work-center-utilization")]
    public async Task<ActionResult<ApiResponse<List<WorkCenterUtilizationDto>>>> GetWorkCenterUtilization(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var data = await GetWorkCenterUtilizationDataAsync(companyId, cancellationToken);
        return Ok(ApiResponse<List<WorkCenterUtilizationDto>>.Success(data));
    }

    // ---- PDF endpoints ----

    [HttpGet("listing/pdf")]
    public async Task<ActionResult> ListingPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var data = await GetListingDataAsync(companyId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "BOM Listing", "Listing", DateTimeOffset.UtcNow);
        var tableRows = data.Select(b => new[]
        {
            b.ParentItemId.ToString().Substring(0, 8), b.Revision, b.Status,
            b.Components.Count.ToString(CultureInfo.InvariantCulture), $"{b.YieldPercentage}%",
        }).ToArray();
        var doc = new ReportDocument(header, "BOM Listing", $"{data.Count} BOMs",
            new (string[], string[][])[] { (new[] { "Parent", "Rev", "Status", "Components", "Yield %" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BomListing.pdf");
    }

    [HttpGet("build-history/pdf")]
    public async Task<ActionResult> BuildHistoryPdfAsync([FromQuery] Guid? companyId, [FromQuery] Guid? parentItemId, CancellationToken ct)
    {
        var data = await GetBuildHistoryDataAsync(companyId, parentItemId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "Build History", "History", DateTimeOffset.UtcNow);
        var tableRows = data.Select(b => new[]
        {
            b.BuildNumber, b.TransactionType, b.ParentItemId.ToString().Substring(0, 8),
            b.QuantityBuilt.ToString(CultureInfo.InvariantCulture), b.Status, b.BuildDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Build History", $"{data.Count} builds",
            new (string[], string[][])[] { (new[] { "Build #", "Type", "Parent", "Qty", "Status", "Date" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BuildHistory.pdf");
    }

    [HttpGet("accuracy/pdf")]
    public async Task<ActionResult> AccuracyPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var data = await GetAccuracyDataAsync(companyId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "BOM Accuracy", "Accuracy", DateTimeOffset.UtcNow);
        var tableRows = data.Select(b => new[]
        {
            b.ParentItemId.ToString().Substring(0, 8), b.Revision, b.Status,
            b.IssueCount.ToString(CultureInfo.InvariantCulture), string.Join("; ", b.Issues),
        }).ToArray();
        var doc = new ReportDocument(header, "BOM Accuracy", $"{data.Count} BOMs with issues",
            new (string[], string[][])[] { (new[] { "Parent", "Rev", "Status", "Issues", "Details" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BomAccuracy.pdf");
    }

    [HttpGet("component-shortage/pdf")]
    public async Task<ActionResult> ComponentShortagePdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var data = await GetComponentShortageDataAsync(companyId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "Component Shortage", "Shortage", DateTimeOffset.UtcNow);
        var tableRows = data.Select(c => new[]
        {
            c.BuildNumber, c.ComponentItemId.ToString().Substring(0, 8),
            c.RequiredQuantity.ToString(CultureInfo.InvariantCulture), c.OnHandQuantity.ToString(CultureInfo.InvariantCulture), c.ShortQuantity.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Component Shortage", $"{data.Count} shortages",
            new (string[], string[][])[] { (new[] { "Build #", "Component", "Required", "On Hand", "Short" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ComponentShortage.pdf");
    }

    [HttpGet("revision-history/pdf")]
    public async Task<ActionResult> RevisionHistoryPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var data = await GetRevisionHistoryDataAsync(companyId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "Revision History", "Revisions", DateTimeOffset.UtcNow);
        var tableRows = data.Select(r => new[]
        {
            r.BomHeaderId.ToString().Substring(0, 8), r.Revision, r.ChangeDescription,
            r.EffectiveDate?.ToString("yyyy-MM-dd") ?? "—",
            r.ChangedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Revision History", $"{data.Count} revisions",
            new (string[], string[][])[] { (new[] { "BOM", "Rev", "Description", "Effective", "Changed" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BomRevisionHistory.pdf");
    }

    [HttpGet("build-variance/pdf")]
    public async Task<ActionResult> BuildVariancePdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var data = await GetBuildVarianceDataAsync(companyId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "Build Variance", "Variance", DateTimeOffset.UtcNow);
        var tableRows = data.Select(v => new[]
        {
            v.BuildNumber, v.ParentItemId.ToString().Substring(0, 8),
            v.QuantityBuilt.ToString(CultureInfo.InvariantCulture), v.ActualYield?.ToString() ?? "—",
            v.TotalStandardCost.ToString("C", CultureInfo.InvariantCulture), v.TotalVarianceCost.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Build Variance", $"{data.Count} builds",
            new (string[], string[][])[] { (new[] { "Build #", "Parent", "Qty Built", "Actual Yield", "Std Cost", "Variance" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "BuildVariance.pdf");
    }

    [HttpGet("work-center-utilization/pdf")]
    public async Task<ActionResult> WorkCenterUtilizationPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var data = await GetWorkCenterUtilizationDataAsync(companyId, ct);
        if (data.Count == 0) return NotFound();
        var header = new CompanyDocumentHeader("BOM Reports", null, null, null, "USD", null, null, null, "WC Utilization", "Utilization", DateTimeOffset.UtcNow);
        var tableRows = data.Select(w => new[]
        {
            w.Code, w.Name, w.CapacityHoursPerMonth.ToString(CultureInfo.InvariantCulture),
            w.PlannedHours.ToString(CultureInfo.InvariantCulture), $"{w.UtilizationPercentage}%",
            w.CostRatePerHour.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Work Center Utilization", $"{data.Count} work centers",
            new (string[], string[][])[] { (new[] { "Code", "Name", "Capacity Hrs", "Planned Hrs", "Utilization", "Rate/Hr" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "WcUtilization.pdf");
    }
}

// --- DTOs --
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
