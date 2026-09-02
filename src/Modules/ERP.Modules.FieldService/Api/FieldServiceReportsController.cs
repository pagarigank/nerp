// <copyright file="FieldServiceReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1408
#pragma warning disable SA1513

using System.Globalization;
using ERP.Modules.FieldService.Domain.Entities;
using ERP.Modules.FieldService.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.FieldService.Api;

[ApiController]
[Route("api/v1/field-service/reports")]
public class FieldServiceReportsController : ControllerBase
{
    private readonly FieldServiceDbContext _context;

    public FieldServiceReportsController(FieldServiceDbContext context)
    {
        _context = context;
    }

    // --- SLA Compliance ---
    [HttpGet("sla-compliance")]
    public async Task<ActionResult<ApiResponse<List<SlaComplianceRow>>>> SlaCompliance(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders
            .Where(w => w.CompanyId == companyId)
            .GroupBy(w => w.Priority)
            .Select(g => new SlaComplianceRow
            {
                Priority = g.Key.ToString(),
                Total = g.Count(),
                Completed = g.Count(w => w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed),
                Breached = g.Count(w => w.ResolutionDue.HasValue && w.ClockOut.HasValue && w.ClockOut.Value > w.ResolutionDue.Value),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<SlaComplianceRow>>.Success(rows));
    }

    // --- Technician Utilization ---
    [HttpGet("technician-utilization")]
    public async Task<ActionResult<ApiResponse<List<TechnicianUtilizationRow>>>> TechnicianUtilization(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders
            .Where(w => w.CompanyId == companyId && w.TechnicianId.HasValue)
            .GroupBy(w => w.TechnicianId!.Value)
            .Select(g => new TechnicianUtilizationRow
            {
                TechnicianId = g.Key,
                WorkOrders = g.Count(),
                LaborHours = g.Sum(w => w.LaborHours),
                BillableTotal = g.Sum(w => w.BillableTotal),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TechnicianUtilizationRow>>.Success(rows));
    }

    // --- Open Work Order Aging ---
    [HttpGet("open-aging")]
    public async Task<ActionResult<ApiResponse<List<OpenWoAgingRow>>>> OpenAging(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.WorkOrders
            .Where(w => w.CompanyId == companyId && w.Status != WorkOrderStatus.Closed && w.Status != WorkOrderStatus.Cancelled)
            .Select(w => new OpenWoAgingRow
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                Status = w.Status.ToString(),
                ScheduledStart = w.ScheduledStart,
                AgeDays = w.ScheduledStart.HasValue ? (int)(now - w.ScheduledStart.Value).TotalDays : 0,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<OpenWoAgingRow>>.Success(rows));
    }

    // --- Contract Status ---
    [HttpGet("contract-status")]
    public async Task<ActionResult<ApiResponse<List<ContractStatusRow>>>> ContractStatus(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.ServiceContracts
            .Where(c => c.CompanyId == companyId)
            .Select(c => new ContractStatusRow
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Name = c.Name,
                Status = c.Status.ToString(),
                EndDate = c.EndDate,
                IsExpired = c.EndDate < now,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ContractStatusRow>>.Success(rows));
    }

    // --- Preventive Maintenance Due ---
    [HttpGet("pm-due")]
    public async Task<ActionResult<ApiResponse<List<PmDueRow>>>> PmDue(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.PreventiveMaintenances
            .Where(p => p.CompanyId == companyId && p.IsActive && p.NextDue <= now)
            .Select(p => new PmDueRow
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                EquipmentAssetId = p.EquipmentAssetId,
                NextDue = p.NextDue,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PmDueRow>>.Success(rows));
    }

    [HttpGet("first-time-fix")]
    public async Task<ActionResult<ApiResponse<FirstTimeFixRow>>> FirstTimeFix([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var total = await _context.WorkOrders.CountAsync(w => w.CompanyId == companyId && w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed, cancellationToken);
        var withFollowUp = await _context.WorkOrders.CountAsync(w => w.CompanyId == companyId && _context.WorkOrders.Any(f => f.WorkOrderNumber == w.WorkOrderNumber + "-FU"), cancellationToken);
        var rate = total == 0 ? 0 : Math.Round((decimal)(total - withFollowUp) / total * 100, 2);
        return Ok(ApiResponse<FirstTimeFixRow>.Success(new FirstTimeFixRow { TotalCompleted = total, FirstTimeFixed = total - withFollowUp, RatePercent = rate }));
    }

    [HttpGet("revenue-profitability")]
    public async Task<ActionResult<ApiResponse<List<RevenueProfitabilityRow>>>> RevenueProfitability([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders.Where(w => w.CompanyId == companyId && (w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Closed)).GroupBy(w => w.TechnicianId).Select(g => new RevenueProfitabilityRow { TechnicianId = g.Key, Revenue = g.Sum(w => w.BillableTotal), Cost = g.Sum(w => w.LaborCost + w.PartsCost + w.TravelCost), Profit = g.Sum(w => w.BillableTotal - (w.LaborCost + w.PartsCost + w.TravelCost)), WorkOrders = g.Count() }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<RevenueProfitabilityRow>>.Success(rows));
    }

    [HttpGet("warranty-expiration")]
    public async Task<ActionResult<ApiResponse<List<WarrantyExpirationRow>>>> WarrantyExpiration([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(90);
        var rows = await _context.EquipmentAssets.Where(e => e.CompanyId == companyId && e.UnderWarranty && e.WarrantyEnd.HasValue && e.WarrantyEnd <= cutoff).Select(e => new WarrantyExpirationRow { Id = e.Id, AssetTag = e.AssetTag, WarrantyEnd = e.WarrantyEnd!.Value, DaysRemaining = (int)(e.WarrantyEnd!.Value - DateTime.UtcNow).TotalDays }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WarrantyExpirationRow>>.Success(rows));
    }

    [HttpGet("parts-usage")]
    public async Task<ActionResult<ApiResponse<List<PartsUsageRow>>>> PartsUsage([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrderLines.Where(l => l.LineType == WorkOrderLineType.Part && _context.WorkOrders.Any(w => w.Id == l.WorkOrderId && w.CompanyId == companyId)).GroupBy(l => l.ItemId).Select(g => new PartsUsageRow { ItemId = g.Key, TotalQuantity = g.Sum(l => l.Quantity), TotalCost = g.Sum(l => l.CostAmount), UsageCount = g.Count() }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PartsUsageRow>>.Success(rows));
    }

    [HttpGet("travel-expense")]
    public async Task<ActionResult<ApiResponse<List<TravelExpenseRow>>>> TravelExpense([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders.Where(w => w.CompanyId == companyId).Select(w => new TravelExpenseRow { WorkOrderId = w.Id, WorkOrderNumber = w.WorkOrderNumber, TravelCost = w.TravelCost, ExpenseTotal = _context.WorkOrderLines.Where(l => l.WorkOrderId == w.Id && l.LineType == WorkOrderLineType.Expense).Sum(l => l.LineTotal), BillableTotal = w.BillableTotal }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TravelExpenseRow>>.Success(rows));
    }

    [HttpGet("work-order-status")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderStatusRow>>>> WorkOrderStatusReport([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkOrders.Where(w => w.CompanyId == companyId).GroupBy(w => w.Status).Select(g => new WorkOrderStatusRow { Status = g.Key.ToString(), Count = g.Count(), TotalBillable = g.Sum(w => w.BillableTotal) }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WorkOrderStatusRow>>.Success(rows));
    }

    // ---- PDF endpoints ----

    [HttpGet("sla-compliance/pdf")]
    public async Task<ActionResult> SlaCompliancePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await SlaCompliance(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<SlaComplianceRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "SLA Compliance", "SLA", DateTimeOffset.UtcNow);
        var rows = data.Select(r => new[]
        {
            r.Priority, r.Total.ToString(CultureInfo.InvariantCulture),
            r.Completed.ToString(CultureInfo.InvariantCulture),
            r.Breached.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "SLA Compliance", null,
            new (string[], string[][])[] { (new[] { "Priority", "Total", "Completed", "Breached" }, rows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SlaCompliance.pdf");
    }

    [HttpGet("technician-utilization/pdf")]
    public async Task<ActionResult> TechnicianUtilizationPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await TechnicianUtilization(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<TechnicianUtilizationRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Technician Utilization", "Util", DateTimeOffset.UtcNow);
        var rows = data.Select(r => new[]
        {
            r.TechnicianId.ToString().Substring(0, 8),
            r.WorkOrders.ToString(CultureInfo.InvariantCulture),
            r.LaborHours.ToString(CultureInfo.InvariantCulture),
            r.BillableTotal.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Technician Utilization", null,
            new (string[], string[][])[] { (new[] { "Technician", "Work Orders", "Labor Hours", "Billable" }, rows) },
            data.Sum(r => r.BillableTotal), "Total Billable");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "TechnicianUtilization.pdf");
    }

    [HttpGet("open-aging/pdf")]
    public async Task<ActionResult> OpenAgingPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await OpenAging(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<OpenWoAgingRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Open WO Aging", "Aging", DateTimeOffset.UtcNow);
        var rows = data.Select(r => new[]
        {
            r.WorkOrderNumber, r.Status,
            r.AgeDays.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Open Work Order Aging", $"{data.Count} open work orders",
            new (string[], string[][])[] { (new[] { "WO #", "Status", "Age Days" }, rows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "OpenWoAging.pdf");
    }

    [HttpGet("contract-status/pdf")]
    public async Task<ActionResult> ContractStatusPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await ContractStatus(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<ContractStatusRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Contract Status", "Contracts", DateTimeOffset.UtcNow);
        var rows = data.Select(c => new[]
        {
            c.ContractNumber, c.Name, c.Status,
            c.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            c.IsExpired ? "Expired" : "Active",
        }).ToArray();
        var doc = new ReportDocument(header, "Contract Status", $"{data.Count} contracts",
            new (string[], string[][])[] { (new[] { "Contract #", "Name", "Status", "End Date", "Expired" }, rows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ContractStatus.pdf");
    }

    [HttpGet("pm-due/pdf")]
    public async Task<ActionResult> PmDuePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await PmDue(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<PmDueRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "PM Due", "PM", DateTimeOffset.UtcNow);
        var rows = data.Select(p => new[]
        {
            p.Code, p.Description,
            p.NextDue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—",
        }).ToArray();
        var doc = new ReportDocument(header, "Preventive Maintenance Due", $"{data.Count} items due",
            new (string[], string[][])[] { (new[] { "Code", "Description", "Next Due" }, rows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "PmDue.pdf");
    }

    [HttpGet("revenue-profitability/pdf")]
    public async Task<ActionResult> RevenueProfitabilityPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await RevenueProfitability(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<RevenueProfitabilityRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Revenue / Profitability", "Rev", DateTimeOffset.UtcNow);
        var rows = data.Select(r => new[]
        {
            r.TechnicianId?.ToString().Substring(0, 8) ?? "—",
            r.Revenue.ToString("C", CultureInfo.InvariantCulture),
            r.Cost.ToString("C", CultureInfo.InvariantCulture),
            r.Profit.ToString("C", CultureInfo.InvariantCulture),
            r.WorkOrders.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Revenue / Profitability", null,
            new (string[], string[][])[] { (new[] { "Technician", "Revenue", "Cost", "Profit", "WOs" }, rows) },
            data.Sum(r => r.Profit), "Total Profit");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "RevenueProfitability.pdf");
    }

    [HttpGet("warranty-expiration/pdf")]
    public async Task<ActionResult> WarrantyExpirationPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await WarrantyExpiration(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<WarrantyExpirationRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Warranty Expiration", "Warranty", DateTimeOffset.UtcNow);
        var rows = data.Select(w => new[]
        {
            w.AssetTag, w.WarrantyEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            w.DaysRemaining.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Warranty Expiration (90 days)", $"{data.Count} assets",
            new (string[], string[][])[] { (new[] { "Asset Tag", "Expires", "Days Left" }, rows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "WarrantyExpiration.pdf");
    }

    [HttpGet("parts-usage/pdf")]
    public async Task<ActionResult> PartsUsagePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await PartsUsage(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<PartsUsageRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Parts Usage", "Parts", DateTimeOffset.UtcNow);
        var rows = data.Select(p => new[]
        {
            p.ItemId?.ToString().Substring(0, 8) ?? "—",
            p.TotalQuantity.ToString(CultureInfo.InvariantCulture),
            p.TotalCost.ToString("C", CultureInfo.InvariantCulture),
            p.UsageCount.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Parts Usage", null,
            new (string[], string[][])[] { (new[] { "Item", "Quantity", "Cost", "Usage Count" }, rows) },
            data.Sum(p => p.TotalCost), "Total Cost");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "PartsUsage.pdf");
    }

    [HttpGet("travel-expense/pdf")]
    public async Task<ActionResult> TravelExpensePdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await TravelExpense(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<TravelExpenseRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Travel / Expense", "Travel", DateTimeOffset.UtcNow);
        var rows = data.Select(t => new[]
        {
            t.WorkOrderNumber,
            t.TravelCost.ToString("C", CultureInfo.InvariantCulture),
            t.ExpenseTotal.ToString("C", CultureInfo.InvariantCulture),
            t.BillableTotal.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Travel / Expense", null,
            new (string[], string[][])[] { (new[] { "WO #", "Travel", "Expenses", "Billable" }, rows) },
            data.Sum(t => t.ExpenseTotal), "Total Expenses");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "TravelExpense.pdf");
    }

    [HttpGet("work-order-status/pdf")]
    public async Task<ActionResult> WorkOrderStatusPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await WorkOrderStatusReport(companyId, ct);
        var ok = result.Result as OkObjectResult;
        var resp = ok?.Value as ApiResponse<List<WorkOrderStatusRow>>;
        var data = resp?.Data;
        if (data == null) return NotFound();
        var header = new CompanyDocumentHeader("Field Service", null, null, null, "USD", null, null, null, "Work Order Status", "WO Status", DateTimeOffset.UtcNow);
        var rows = data.Select(r => new[]
        {
            r.Status, r.Count.ToString(CultureInfo.InvariantCulture),
            r.TotalBillable.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Work Order Status", null,
            new (string[], string[][])[] { (new[] { "Status", "Count", "Total Billable" }, rows) },
            data.Sum(r => r.TotalBillable), "Total Billable");
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "WorkOrderStatus.pdf");
    }
}

public record SlaComplianceRow
{
    public string Priority { get; init; } = string.Empty;
    public int Total { get; init; }
    public int Completed { get; init; }
    public int Breached { get; init; }
}

public record TechnicianUtilizationRow
{
    public Guid TechnicianId { get; init; }
    public int WorkOrders { get; init; }
    public decimal LaborHours { get; init; }
    public decimal BillableTotal { get; init; }
}

public record OpenWoAgingRow
{
    public Guid Id { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? ScheduledStart { get; init; }
    public int AgeDays { get; init; }
}

public record ContractStatusRow
{
    public Guid Id { get; init; }
    public string ContractNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime EndDate { get; init; }
    public bool IsExpired { get; init; }
}

public record PmDueRow
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? EquipmentAssetId { get; init; }
    public DateTime? NextDue { get; init; }
}
public record FirstTimeFixRow { public int TotalCompleted { get; init; } public int FirstTimeFixed { get; init; } public decimal RatePercent { get; init; } }
public record RevenueProfitabilityRow { public Guid? TechnicianId { get; init; } public decimal Revenue { get; init; } public decimal Cost { get; init; } public decimal Profit { get; init; } public int WorkOrders { get; init; } }
public record WarrantyExpirationRow { public Guid Id { get; init; } public string AssetTag { get; init; } = string.Empty; public DateTime WarrantyEnd { get; init; } public int DaysRemaining { get; init; } }
public record PartsUsageRow { public Guid? ItemId { get; init; } public decimal TotalQuantity { get; init; } public decimal TotalCost { get; init; } public int UsageCount { get; init; } }
public record TravelExpenseRow { public Guid WorkOrderId { get; init; } public string WorkOrderNumber { get; init; } = string.Empty; public decimal TravelCost { get; init; } public decimal ExpenseTotal { get; init; } public decimal BillableTotal { get; init; } }
public record WorkOrderStatusRow { public string Status { get; init; } = string.Empty; public int Count { get; init; } public decimal TotalBillable { get; init; } }
