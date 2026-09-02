// <copyright file="PurchasingReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/reports")]
public class PurchasingReportsController : ControllerBase
{
    private readonly PurchasingDbContext _context;
    private readonly ICustomerReadService _customerRead;
    private readonly IVendorReadService _vendorRead;

    public PurchasingReportsController(
        PurchasingDbContext context,
        ICustomerReadService customerRead,
        IVendorReadService vendorRead)
    {
        _context = context;
        _customerRead = customerRead;
        _vendorRead = vendorRead;
    }

    [HttpGet("open-po")]
    public async Task<ActionResult<ApiResponse<List<OpenPOReportDto>>>> GetOpenPOReport(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? vendorId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved);

        query = query.ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId);

        if (vendorId.HasValue)
            query = query.Where(p => p.VendorId == vendorId.Value);

        var pos = await query.ToListAsync(cancellationToken);

        var report = pos.Select(p => new OpenPOReportDto
        {
            PONumber = p.PONumber,
            VendorId = p.VendorId,
            OrderDate = p.OrderDate,
            Status = p.Status.ToString(),
            TotalAmount = p.GetTotalAmount(),
            ReceivedAmount = p.Lines.Sum(l => l.QuantityReceived * l.UnitPrice),
            RemainingAmount = p.GetRemainingAmount(),
            DaysOpen = (DateTime.UtcNow - p.OrderDate).Days,
        }).ToList();

        return Ok(ApiResponse<List<OpenPOReportDto>>.Success(report));
    }

    [HttpGet("requisition-status")]
    public async Task<ActionResult<ApiResponse<List<RequisitionStatusReportDto>>>> GetRequisitionStatusReport(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.Requisitions.AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId);

        if (fromDate.HasValue)
            query = query.Where(r => r.RequestDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.RequestDate <= toDate.Value);

        var reqs = await query.ToListAsync(cancellationToken);

        var report = reqs.GroupBy(r => r.Status)
            .Select(g => new RequisitionStatusReportDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(r => r.GetTotalAmount()),
                AverageDaysToApproval = g.Where(r => r.ApprovedDate.HasValue)
                    .Average(r => (r.ApprovedDate!.Value - r.RequestDate).TotalDays),
            }).ToList();

        return Ok(ApiResponse<List<RequisitionStatusReportDto>>.Success(report));
    }

    [HttpGet("receiving-report")]
    public async Task<ActionResult<ApiResponse<List<ReceivingReportDto>>>> GetReceivingReport(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.Receipts
            .Include(r => r.Lines)
            .Where(r => r.Status == ReceiptStatus.Posted);

        query = query.ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId);

        if (fromDate.HasValue)
            query = query.Where(r => r.ReceivedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ReceivedDate <= toDate.Value);

        var receipts = await query.ToListAsync(cancellationToken);

        var report = receipts.Select(r => new ReceivingReportDto
        {
            ReceiptNumber = r.ReceiptNumber,
            ReceivedDate = r.ReceivedDate,
            VendorId = r.VendorId,
            PurchaseOrderId = r.PurchaseOrderId,
            ReceivedBy = r.ReceivedBy,
            LineCount = r.Lines.Count,
            TotalQuantity = r.Lines.Sum(l => l.QuantityReceived),
        }).ToList();

        return Ok(ApiResponse<List<ReceivingReportDto>>.Success(report));
    }

    [HttpGet("committed-cost")]
    public async Task<ActionResult<ApiResponse<CommittedCostReportDto>>> GetCommittedCostReport(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrderLines
            .Where(l => !l.IsCancelled);

        if (projectId.HasValue)
            query = query.Where(l => l.ProjectId == projectId.Value);

        if (accountId.HasValue)
            query = query.Where(l => l.AccountId == accountId.Value);

        var lines = await query.ToListAsync(cancellationToken);

        var report = new CommittedCostReportDto
        {
            ProjectId = projectId,
            AccountId = accountId,
            TotalCommitted = lines.Sum(l => l.Quantity * l.UnitPrice),
            TotalReceived = lines.Sum(l => l.QuantityReceived * l.UnitPrice),
            RemainingCommitment = lines.Sum(l => (l.Quantity - l.QuantityReceived) * l.UnitPrice),
            POCount = lines.Select(l => l.PurchaseOrderId).Distinct().Count(),
        };

        return Ok(ApiResponse<CommittedCostReportDto>.Success(report));
    }

    [HttpGet("vendor-performance")]
    public async Task<ActionResult<ApiResponse<List<VendorPerformanceReportDto>>>> GetVendorPerformanceReport(
        [FromQuery] Guid? vendorId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved);

        if (vendorId.HasValue)
            query = query.Where(p => p.VendorId == vendorId.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.OrderDate <= toDate.Value);

        var pos = await query.ToListAsync(cancellationToken);

        var report = pos.GroupBy(p => p.VendorId)
            .Select(g => new VendorPerformanceReportDto
            {
                VendorId = g.Key,
                TotalPOs = g.Count(),
                TotalAmount = g.Sum(p => p.GetTotalAmount()),
                OnTimePOs = g.Count(p => p.Lines.All(l => !l.NeedByDate.HasValue || l.QuantityReceived >= l.Quantity)),
                OnTimePercentage = g.Any() ? (decimal)g.Count(p => p.Lines.All(l => !l.NeedByDate.HasValue || l.QuantityReceived >= l.Quantity)) / g.Count() * 100 : 0,
            }).ToList();

        return Ok(ApiResponse<List<VendorPerformanceReportDto>>.Success(report));
    }

    [HttpGet("po-status")]
    public async Task<ActionResult<ApiResponse<List<POStatusReportDto>>>> GetPOStatusReport(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders.AsQueryable();
        query = query.ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId);

        var pos = await query.ToListAsync(cancellationToken);

        var report = pos
            .GroupBy(p => p.Status)
            .Select(g => new POStatusReportDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.GetTotalAmount()),
                AverageDaysInStatus = g.Any()
                    ? Math.Round(g.Average(p => (DateTime.UtcNow - p.OrderDate).TotalDays), 1)
                    : 0,
            }).ToList();

        return Ok(ApiResponse<List<POStatusReportDto>>.Success(report));
    }

    [HttpGet("purchase-analysis")]
    public async Task<ActionResult<ApiResponse<List<PurchaseAnalysisReportDto>>>> GetPurchaseAnalysisReport(
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved || p.Status == PurchaseOrderStatus.Closed || p.Status == PurchaseOrderStatus.Cancelled);

        query = query.ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId);
        if (fromDate.HasValue)
            query = query.Where(p => p.OrderDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.OrderDate <= toDate.Value);

        var pos = await query.ToListAsync(cancellationToken);

        var report = pos
            .GroupBy(p => new { p.VendorId, BuyerId = p.BuyerId ?? Guid.Empty })
            .Select(g => new PurchaseAnalysisReportDto
            {
                VendorId = g.Key.VendorId,
                BuyerId = g.Key.BuyerId,
                POCount = g.Count(),
                TotalSpend = g.Sum(p => p.GetTotalAmount()),
                TotalTax = g.Sum(p => p.GetTaxTotal()),
                TotalFreight = g.Sum(p => p.FreightAmount),
                LineCount = g.Sum(p => p.Lines.Count),
            }).ToList();

        return Ok(ApiResponse<List<PurchaseAnalysisReportDto>>.Success(report));
    }

    [HttpGet("price-variance")]
    public async Task<ActionResult<ApiResponse<List<PriceVarianceReportDto>>>> GetPriceVarianceReport(
        [FromQuery] Guid? companyId,
        [FromQuery] decimal threshold,
        CancellationToken cancellationToken)
    {
        if (threshold <= 0)
            threshold = 0.05m;
        var lines = await _context.PurchaseOrderLines
            .Where(l => !l.IsCancelled && l.ItemId != null)
            .ToListAsync(cancellationToken);

        var poIds = lines.Select(l => l.PurchaseOrderId).Distinct().ToList();
        var pos = await _context.PurchaseOrders
            .Where(p => poIds.Contains(p.Id) && (!companyId.HasValue || p.CompanyId == companyId.Value))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var vendorItemCosts = await _context.VendorItems.ToListAsync(cancellationToken);

        var report = new List<PriceVarianceReportDto>();
        foreach (var l in lines)
        {
            if (!pos.TryGetValue(l.PurchaseOrderId, out var po))
                continue;
            var vi = vendorItemCosts.FirstOrDefault(v => v.VendorId == po.VendorId && v.ItemId == l.ItemId);
            if (vi == null || vi.Cost == 0)
                continue;
            var variance = (l.UnitPrice - vi.Cost) / vi.Cost;
            if (Math.Abs(variance) <= threshold)
                continue;
            report.Add(new PriceVarianceReportDto
            {
                PurchaseOrderId = l.PurchaseOrderId,
                PONumber = po.PONumber,
                ItemId = l.ItemId,
                Description = l.Description,
                VendorStandardCost = vi.Cost,
                POUnitPrice = l.UnitPrice,
                VariancePercent = Math.Round(variance * 100, 2),
                ExtendedVariance = Math.Round((l.UnitPrice - vi.Cost) * l.Quantity, 2, MidpointRounding.AwayFromZero),
            });
        }

        return Ok(ApiResponse<List<PriceVarianceReportDto>>.Success(report));
    }

    [HttpGet("over-receipt-exceptions")]
    public async Task<ActionResult<ApiResponse<List<OverReceiptExceptionReportDto>>>> GetOverReceiptExceptionReport(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var receipts = await _context.Receipts
            .Include(r => r.Lines)
            .Where(r => r.Status == ReceiptStatus.Posted && !r.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var poLineIds = receipts
            .SelectMany(r => r.Lines)
            .Where(l => l.PurchaseOrderLineId.HasValue)
            .Select(l => l.PurchaseOrderLineId!.Value)
            .Distinct()
            .ToList();

        var poLines = await _context.PurchaseOrderLines
            .Where(l => poLineIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l, cancellationToken);

        var report = new List<OverReceiptExceptionReportDto>();
        foreach (var r in receipts)
        {
            foreach (var rl in r.Lines)
            {
                if (!rl.PurchaseOrderLineId.HasValue || !poLines.TryGetValue(rl.PurchaseOrderLineId.Value, out var poLine))
                    continue;
                if (poLine.Quantity <= 0)
                    continue;
                var pct = (rl.QuantityReceived - poLine.Quantity) / poLine.Quantity;
                if (pct <= 0)
                    continue;
                report.Add(new OverReceiptExceptionReportDto
                {
                    ReceiptId = r.Id,
                    ReceiptNumber = r.ReceiptNumber,
                    ReceivedDate = r.ReceivedDate,
                    VendorId = r.VendorId,
                    PurchaseOrderLineId = rl.PurchaseOrderLineId.Value,
                    Description = rl.Description,
                    OrderedQuantity = poLine.Quantity,
                    ReceivedQuantity = rl.QuantityReceived,
                    OverReceiptPercent = Math.Round(pct * 100, 2),
                    BuyerId = poLine.PurchaseOrderId,
                });
            }
        }

        return Ok(ApiResponse<List<OverReceiptExceptionReportDto>>.Success(report));
    }

    // ---- Private helpers for PDF endpoints ----

    private async Task<List<OpenPOReportDto>> GetOpenPOReportData(Guid companyId, Guid? vendorId, CancellationToken ct)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved);
        query = query.ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId);
        if (vendorId.HasValue)
        {
            query = query.Where(p => p.VendorId == vendorId.Value);
        }

        var pos = await query.ToListAsync(ct);
        return pos.Select(p => new OpenPOReportDto
        {
            PONumber = p.PONumber,
            VendorId = p.VendorId,
            OrderDate = p.OrderDate,
            Status = p.Status.ToString(),
            TotalAmount = p.GetTotalAmount(),
            ReceivedAmount = p.Lines.Sum(l => l.QuantityReceived * l.UnitPrice),
            RemainingAmount = p.GetRemainingAmount(),
            DaysOpen = (DateTime.UtcNow - p.OrderDate).Days,
        }).ToList();
    }

    private async Task<List<RequisitionStatusReportDto>> GetRequisitionStatusData(Guid companyId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Requisitions.AsQueryable();
        query = query.ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId);
        if (fromDate.HasValue)
        {
            query = query.Where(r => r.RequestDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.RequestDate <= toDate.Value);
        }

        var reqs = await query.ToListAsync(ct);
        return reqs.GroupBy(r => r.Status)
            .Select(g => new RequisitionStatusReportDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(r => r.GetTotalAmount()),
                AverageDaysToApproval = g.Where(r => r.ApprovedDate.HasValue)
                    .Average(r => (r.ApprovedDate!.Value - r.RequestDate).TotalDays),
            }).ToList();
    }

    private async Task<List<ReceivingReportDto>> GetReceivingReportData(Guid companyId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Receipts
            .Include(r => r.Lines)
            .Where(r => r.Status == ReceiptStatus.Posted);
        query = query.ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId);
        if (fromDate.HasValue)
        {
            query = query.Where(r => r.ReceivedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.ReceivedDate <= toDate.Value);
        }

        var receipts = await query.ToListAsync(ct);
        return receipts.Select(r => new ReceivingReportDto
        {
            ReceiptNumber = r.ReceiptNumber,
            ReceivedDate = r.ReceivedDate,
            VendorId = r.VendorId,
            PurchaseOrderId = r.PurchaseOrderId,
            ReceivedBy = r.ReceivedBy,
            LineCount = r.Lines.Count,
            TotalQuantity = r.Lines.Sum(l => l.QuantityReceived),
        }).ToList();
    }

    private async Task<CommittedCostReportDto> GetCommittedCostData(Guid? projectId, Guid? accountId, CancellationToken ct)
    {
        var query = _context.PurchaseOrderLines.Where(l => !l.IsCancelled);
        if (projectId.HasValue)
        {
            query = query.Where(l => l.ProjectId == projectId.Value);
        }

        if (accountId.HasValue)
        {
            query = query.Where(l => l.AccountId == accountId.Value);
        }

        var lines = await query.ToListAsync(ct);
        return new CommittedCostReportDto
        {
            ProjectId = projectId,
            AccountId = accountId,
            TotalCommitted = lines.Sum(l => l.Quantity * l.UnitPrice),
            TotalReceived = lines.Sum(l => l.QuantityReceived * l.UnitPrice),
            RemainingCommitment = lines.Sum(l => (l.Quantity - l.QuantityReceived) * l.UnitPrice),
            POCount = lines.Select(l => l.PurchaseOrderId).Distinct().Count(),
        };
    }

    private async Task<List<POStatusReportDto>> GetPOStatusData(Guid companyId, CancellationToken ct)
    {
        var query = _context.PurchaseOrders.AsQueryable();
        query = query.ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId);
        var pos = await query.ToListAsync(ct);
        return pos
            .GroupBy(p => p.Status)
            .Select(g => new POStatusReportDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.GetTotalAmount()),
                AverageDaysInStatus = g.Any()
                    ? Math.Round(g.Average(p => (DateTime.UtcNow - p.OrderDate).TotalDays), 1)
                    : 0,
            }).ToList();
    }

    private async Task<List<VendorPerformanceReportDto>> GetVendorPerformanceData(Guid? vendorId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved);
        if (vendorId.HasValue)
        {
            query = query.Where(p => p.VendorId == vendorId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.OrderDate <= toDate.Value);
        }

        var pos = await query.ToListAsync(ct);
        return pos.GroupBy(p => p.VendorId)
            .Select(g => new VendorPerformanceReportDto
            {
                VendorId = g.Key,
                TotalPOs = g.Count(),
                TotalAmount = g.Sum(p => p.GetTotalAmount()),
                OnTimePOs = g.Count(p => p.Lines.All(l => !l.NeedByDate.HasValue || l.QuantityReceived >= l.Quantity)),
                OnTimePercentage = g.Any() ? (decimal)g.Count(p => p.Lines.All(l => !l.NeedByDate.HasValue || l.QuantityReceived >= l.Quantity)) / g.Count() * 100 : 0,
            }).ToList();
    }

    private async Task<List<PurchaseAnalysisReportDto>> GetPurchaseAnalysisData(Guid companyId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved || p.Status == PurchaseOrderStatus.Closed || p.Status == PurchaseOrderStatus.Cancelled);
        query = query.ApplyCompanyScope(HttpContext, p => p.CompanyId, companyId);
        if (fromDate.HasValue)
        {
            query = query.Where(p => p.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.OrderDate <= toDate.Value);
        }

        var pos = await query.ToListAsync(ct);
        return pos
            .GroupBy(p => new { p.VendorId, BuyerId = p.BuyerId ?? Guid.Empty })
            .Select(g => new PurchaseAnalysisReportDto
            {
                VendorId = g.Key.VendorId,
                BuyerId = g.Key.BuyerId,
                POCount = g.Count(),
                TotalSpend = g.Sum(p => p.GetTotalAmount()),
                TotalTax = g.Sum(p => p.GetTaxTotal()),
                TotalFreight = g.Sum(p => p.FreightAmount),
                LineCount = g.Sum(p => p.Lines.Count),
            }).ToList();
    }

    private async Task<List<PriceVarianceReportDto>> GetPriceVarianceData(Guid companyId, decimal threshold, CancellationToken ct)
    {
        if (threshold <= 0)
        {
            threshold = 0.05m;
        }

        var lines = await _context.PurchaseOrderLines
            .Where(l => !l.IsCancelled && l.ItemId != null)
            .ToListAsync(ct);
        var poIds = lines.Select(l => l.PurchaseOrderId).Distinct().ToList();
        var pos = await _context.PurchaseOrders
            .Where(p => poIds.Contains(p.Id) && p.CompanyId == companyId)
            .ToDictionaryAsync(p => p.Id, p => p, ct);
        var vendorItemCosts = await _context.VendorItems.ToListAsync(ct);
        var report = new List<PriceVarianceReportDto>();
        foreach (var l in lines)
        {
            if (!pos.TryGetValue(l.PurchaseOrderId, out var po))
            {
                continue;
            }

            var vi = vendorItemCosts.FirstOrDefault(v => v.VendorId == po.VendorId && v.ItemId == l.ItemId);
            if (vi == null || vi.Cost == 0)
            {
                continue;
            }

            var variance = (l.UnitPrice - vi.Cost) / vi.Cost;
            if (Math.Abs(variance) <= threshold)
            {
                continue;
            }

            report.Add(new PriceVarianceReportDto
            {
                PurchaseOrderId = l.PurchaseOrderId,
                PONumber = po.PONumber,
                ItemId = l.ItemId,
                Description = l.Description,
                VendorStandardCost = vi.Cost,
                POUnitPrice = l.UnitPrice,
                VariancePercent = Math.Round(variance * 100, 2),
                ExtendedVariance = Math.Round((l.UnitPrice - vi.Cost) * l.Quantity, 2, MidpointRounding.AwayFromZero),
            });
        }

        return report;
    }

    private async Task<List<OverReceiptExceptionReportDto>> GetOverReceiptExceptionData(Guid companyId, CancellationToken ct)
    {
        var receipts = await _context.Receipts
            .Include(r => r.Lines)
            .Where(r => r.Status == ReceiptStatus.Posted && !r.DeletedOn.HasValue)
            .ToListAsync(ct);
        var poLineIds = receipts
            .SelectMany(r => r.Lines)
            .Where(l => l.PurchaseOrderLineId.HasValue)
            .Select(l => l.PurchaseOrderLineId!.Value)
            .Distinct()
            .ToList();
        var poLines = await _context.PurchaseOrderLines
            .Where(l => poLineIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l, ct);
        var report = new List<OverReceiptExceptionReportDto>();
        foreach (var r in receipts)
        {
            foreach (var rl in r.Lines)
            {
                if (!rl.PurchaseOrderLineId.HasValue || !poLines.TryGetValue(rl.PurchaseOrderLineId.Value, out var poLine))
                {
                    continue;
                }

                if (poLine.Quantity <= 0)
                {
                    continue;
                }

                var pctVal = (rl.QuantityReceived - poLine.Quantity) / poLine.Quantity;
                if (pctVal <= 0)
                {
                    continue;
                }

                report.Add(new OverReceiptExceptionReportDto
                {
                    ReceiptId = r.Id,
                    ReceiptNumber = r.ReceiptNumber,
                    ReceivedDate = r.ReceivedDate,
                    VendorId = r.VendorId,
                    PurchaseOrderLineId = rl.PurchaseOrderLineId.Value,
                    Description = rl.Description,
                    OrderedQuantity = poLine.Quantity,
                    ReceivedQuantity = rl.QuantityReceived,
                    OverReceiptPercent = Math.Round(pctVal * 100, 2),
                    BuyerId = poLine.PurchaseOrderId,
                });
            }
        }

        return report;
    }

    private async Task<CompanyDocumentHeader> GetHeaderAsync(Guid companyId, string title, string number, CancellationToken ct)
    {
        return await _customerRead.GetDocumentHeaderAsync(companyId, null, title, number, null, ct);
    }

    private static string VName(Dictionary<Guid, string> map, Guid id)
    {
        return map.TryGetValue(id, out var name) ? name : id.ToString().Substring(0, 8);
    }

    private async Task<Dictionary<Guid, string>> ResolveVendorNamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var distinct = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var names = await _vendorRead.GetVendorNamesAsync(distinct, ct);
        return names.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    [HttpGet("open-po/pdf")]
    public async Task<ActionResult> OpenPOPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetOpenPOReportData(companyId.Value, null, ct);
        var header = await GetHeaderAsync(companyId.Value, "Open PO Report", "Open POs", ct);
        var vendorNames = await ResolveVendorNamesAsync(rows.Select(r => r.VendorId), ct);
        var tableRows = rows.Select(r => new[]
        {
            r.PONumber,
            VName(vendorNames, r.VendorId),
            r.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Status,
            r.TotalAmount.ToString("C", CultureInfo.InvariantCulture),
            r.ReceivedAmount.ToString("C", CultureInfo.InvariantCulture),
            r.RemainingAmount.ToString("C", CultureInfo.InvariantCulture),
            r.DaysOpen.ToString(),
        }).ToArray();
        var doc = new ReportDocument(header, "Open PO Report", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "PO #", "Vendor", "Date", "Status", "Total", "Received", "Remaining", "Days Open" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "OpenPOReport.pdf");
    }

    [HttpGet("requisition-status/pdf")]
    public async Task<ActionResult> RequisitionStatusPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetRequisitionStatusData(companyId.Value, null, null, ct);
        var header = await GetHeaderAsync(companyId.Value, "Requisition Status Report", "Requisition Status", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.Status,
            r.Count.ToString(),
            r.TotalAmount.ToString("C", CultureInfo.InvariantCulture),
            r.AverageDaysToApproval?.ToString("F1") ?? "—",
        }).ToArray();
        var doc = new ReportDocument(header, "Requisition Status", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Status", "Count", "Total Amount", "Avg Days" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "RequisitionStatus.pdf");
    }

    [HttpGet("receiving-report/pdf")]
    public async Task<ActionResult> ReceivingReportPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetReceivingReportData(companyId.Value, null, null, ct);
        var header = await GetHeaderAsync(companyId.Value, "Receiving Report", "Receiving", ct);
        var vendorNames = await ResolveVendorNamesAsync(rows.Where(r => r.VendorId.HasValue).Select(r => r.VendorId!.Value), ct);
        var tableRows = rows.Select(r => new[]
        {
            r.ReceiptNumber,
            r.VendorId.HasValue ? VName(vendorNames, r.VendorId.Value) : "—",
            r.ReceivedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.LineCount.ToString(),
            r.TotalQuantity.ToString("N0", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Receiving Report", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Receipt #", "Vendor", "Date", "Lines", "Total Qty" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ReceivingReport.pdf");
    }

    [HttpGet("committed-cost/pdf")]
    public async Task<ActionResult> CommittedCostPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetCommittedCostData(null, null, ct);
        var header = await GetHeaderAsync(companyId.Value, "Committed Cost Report", "Committed Cost", ct);
        var tableRows = new[] { new[]
        {
            rows.TotalCommitted.ToString("C", CultureInfo.InvariantCulture),
            rows.TotalReceived.ToString("C", CultureInfo.InvariantCulture),
            rows.RemainingCommitment.ToString("C", CultureInfo.InvariantCulture),
            rows.POCount.ToString(),
        } };
        var doc = new ReportDocument(header, "Committed Cost", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Total Committed", "Total Received", "Remaining", "PO Count" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CommittedCost.pdf");
    }

    [HttpGet("po-status/pdf")]
    public async Task<ActionResult> POStatusPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetPOStatusData(companyId.Value, ct);
        var header = await GetHeaderAsync(companyId.Value, "PO Status Report", "PO Status", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.Status,
            r.Count.ToString(),
            r.TotalAmount.ToString("C", CultureInfo.InvariantCulture),
            r.AverageDaysInStatus.ToString("F1"),
        }).ToArray();
        var doc = new ReportDocument(header, "PO Status", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Status", "Count", "Total Amount", "Avg Days" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "POStatus.pdf");
    }

    [HttpGet("vendor-performance/pdf")]
    public async Task<ActionResult> VendorPerformancePdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetVendorPerformanceData(null, null, null, ct);
        var header = await GetHeaderAsync(companyId.Value, "Vendor Performance Report", "Vendor Performance", ct);
        var vendorNames = await ResolveVendorNamesAsync(rows.Select(r => r.VendorId), ct);
        var tableRows = rows.Select(r => new[]
        {
            VName(vendorNames, r.VendorId),
            r.TotalPOs.ToString(),
            r.TotalAmount.ToString("C", CultureInfo.InvariantCulture),
            r.OnTimePOs.ToString(),
            r.OnTimePercentage.ToString("F1") + "%",
        }).ToArray();
        var doc = new ReportDocument(header, "Vendor Performance", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Vendor", "POs", "Total Spend", "On-Time", "On-Time %" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "VendorPerformance.pdf");
    }

    [HttpGet("purchase-analysis/pdf")]
    public async Task<ActionResult> PurchaseAnalysisPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetPurchaseAnalysisData(companyId.Value, null, null, ct);
        var header = await GetHeaderAsync(companyId.Value, "Purchase Analysis Report", "Purchase Analysis", ct);
        var vendorNames = await ResolveVendorNamesAsync(rows.Select(r => r.VendorId), ct);
        var tableRows = rows.Select(r => new[]
        {
            VName(vendorNames, r.VendorId),
            r.BuyerId.ToString().Substring(0, 8),
            r.POCount.ToString(),
            r.TotalSpend.ToString("C", CultureInfo.InvariantCulture),
            r.TotalTax.ToString("C", CultureInfo.InvariantCulture),
            r.TotalFreight.ToString("C", CultureInfo.InvariantCulture),
            r.LineCount.ToString(),
        }).ToArray();
        var doc = new ReportDocument(header, "Purchase Analysis", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Vendor", "Buyer", "POs", "Spend", "Tax", "Freight", "Lines" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "PurchaseAnalysis.pdf");
    }

    [HttpGet("price-variance/pdf")]
    public async Task<ActionResult> PriceVariancePdfAsync([FromQuery] Guid? companyId, [FromQuery] decimal threshold, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetPriceVarianceData(companyId.Value, threshold, ct);
        var header = await GetHeaderAsync(companyId.Value, "Price Variance Report", "Price Variance", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.PONumber,
            r.Description,
            r.VendorStandardCost.ToString("C", CultureInfo.InvariantCulture),
            r.POUnitPrice.ToString("C", CultureInfo.InvariantCulture),
            r.VariancePercent.ToString("F2") + "%",
            r.ExtendedVariance.ToString("C", CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Price Variance", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "PO #", "Description", "Std Cost", "PO Price", "Variance %", "Ext Variance" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "PriceVariance.pdf");
    }

    [HttpGet("over-receipt-exceptions/pdf")]
    public async Task<ActionResult> OverReceiptExceptionPdfAsync([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (companyId is null)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { "companyId is required." }));
        }

        var rows = await GetOverReceiptExceptionData(companyId.Value, ct);
        var header = await GetHeaderAsync(companyId.Value, "Over-Receipt Exception Report", "Over-Receipt", ct);
        var tableRows = rows.Select(r => new[]
        {
            r.ReceiptNumber,
            r.ReceivedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.Description,
            r.OrderedQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.ReceivedQuantity.ToString("N0", CultureInfo.InvariantCulture),
            r.OverReceiptPercent.ToString("F2") + "%",
        }).ToArray();
        var doc = new ReportDocument(header, "Over-Receipt Exceptions", $"As of {DateTimeOffset.Now:yyyy-MM-dd}",
            new[] { (new[] { "Receipt #", "Date", "Description", "Ordered", "Received", "Over %" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "OverReceiptExceptions.pdf");
    }
}

public class OpenPOReportDto
{
    public string PONumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int DaysOpen { get; set; }
}

public class RequisitionStatusReportDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public double? AverageDaysToApproval { get; set; }
}

public class ReceivingReportDto
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? ReceivedBy { get; set; }
    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class CommittedCostReportDto
{
    public Guid? ProjectId { get; set; }
    public Guid? AccountId { get; set; }
    public decimal TotalCommitted { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal RemainingCommitment { get; set; }
    public int POCount { get; set; }
}

public class VendorPerformanceReportDto
{
    public Guid VendorId { get; set; }
    public int TotalPOs { get; set; }
    public decimal TotalAmount { get; set; }
    public int OnTimePOs { get; set; }
    public decimal OnTimePercentage { get; set; }
}

public class POStatusReportDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public double AverageDaysInStatus { get; set; }
}

public class PurchaseAnalysisReportDto
{
    public Guid VendorId { get; set; }
    public Guid BuyerId { get; set; }
    public int POCount { get; set; }
    public decimal TotalSpend { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalFreight { get; set; }
    public int LineCount { get; set; }
}

public class PriceVarianceReportDto
{
    public Guid PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal VendorStandardCost { get; set; }
    public decimal POUnitPrice { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal ExtendedVariance { get; set; }
}

public class OverReceiptExceptionReportDto
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public Guid? VendorId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal OverReceiptPercent { get; set; }
    public Guid BuyerId { get; set; }
}
