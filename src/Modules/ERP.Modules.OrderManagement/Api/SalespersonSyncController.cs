// <copyright file="SalespersonSyncController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Data;
using System.Globalization;
using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om")]
public class SalespersonSyncController : ControllerBase
{
    private readonly OmDbContext _context;
    private readonly PlatformDbContext _platformContext;

    public SalespersonSyncController(OmDbContext context, PlatformDbContext platformContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
    }

    /// <summary>
    /// Downloads the full offline sync package (rep info, routes, customers, items, pricing rules)
    /// to populate the salesperson's offline SQLite database.
    /// </summary>
    [HttpGet("salesperson/sync-package")]
    public async Task<ActionResult<ApiResponse<OfflineSyncPackage>>> GetSyncPackageAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid salesRepId,
        CancellationToken cancellationToken)
    {
        var rep = await _context.SalesReps.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == salesRepId, cancellationToken);

        if (rep == null)
        {
            return NotFound(ApiResponse<OfflineSyncPackage>.Failure(new[] { $"Sales rep {salesRepId} not found." }));
        }

        // 1. Routes
        var routes = await _context.SalespersonRoutes.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.SalesRepId == salesRepId && r.IsActive)
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.SequenceOrder)
            .ToListAsync(cancellationToken);

        // 2. Customers
        var customers = await LoadCustomersForPackageAsync(companyId, cancellationToken);

        // Map customer info onto routes
        var custDict = customers.ToDictionary(c => c.Id);
        var routeDtos = routes.Select(r =>
        {
            custDict.TryGetValue(r.CustomerId, out var c);
            return new OfflineRouteDto(
                r.Id,
                r.SalesRepId,
                r.CustomerId,
                c?.CustomerCode ?? string.Empty,
                c?.CustomerName ?? "Unknown Customer",
                c?.ContactName ?? string.Empty,
                c?.Phone ?? string.Empty,
                c?.ShippingAddress ?? string.Empty,
                (int)r.DayOfWeek,
                r.SequenceOrder,
                r.EstimatedTime,
                r.Notes,
                c?.CreditLimit ?? 0m,
                c?.CurrentBalance ?? 0m);
        }).ToList();

        // 3. Products
        var products = await LoadProductsForPackageAsync(companyId, cancellationToken);

        // 4. Pricing Rules
        var rules = await _context.PricingRules.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .OrderBy(r => r.PrioritySequence)
            .Select(r => new OfflinePricingRuleDto(
                r.Id,
                r.CustomerId,
                r.ItemId,
                r.ItemCategoryId,
                r.DiscountPercent,
                r.UnitPriceOverride,
                r.MinimumQuantity,
                r.PrioritySequence))
            .ToListAsync(cancellationToken);

        var package = new OfflineSyncPackage(
            companyId,
            new OfflineRepDto(rep.Id, rep.Code, rep.Name, rep.CommissionRate, rep.Email),
            DateTime.UtcNow,
            routeDtos,
            customers,
            products,
            rules);

        return Ok(ApiResponse<OfflineSyncPackage>.Success(package));
    }

    /// <summary>
    /// Uploads batch of offline-recorded sales calls and parks them for later review.
    /// Sales Orders are NOT created here — use process-parked to selectively convert parked calls into orders.
    /// </summary>
    [HttpPost("salesperson/sync-upload")]
    public async Task<ActionResult<ApiResponse<SyncUploadResult>>> SyncUploadAsync(
        [FromBody] SyncUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Calls == null || request.Calls.Count == 0)
        {
            return Ok(ApiResponse<SyncUploadResult>.Success(new SyncUploadResult(new List<SyncCallResultItem>(), 0)));
        }

        var results = new List<SyncCallResultItem>();

        foreach (var callDto in request.Calls)
        {
            // Idempotent deduplication check via ClientCallId
            if (!string.IsNullOrWhiteSpace(callDto.ClientCallId))
            {
                var existingCall = await _context.SalesCalls
                    .Include(c => c.Lines)
                    .FirstOrDefaultAsync(c => c.CompanyId == request.CompanyId && c.ClientCallId == callDto.ClientCallId, cancellationToken);

                if (existingCall != null)
                {
                    string? existingSoNumber = null;
                    if (existingCall.SalesOrderId.HasValue)
                    {
                        existingSoNumber = await _context.SalesOrders
                            .Where(o => o.Id == existingCall.SalesOrderId.Value)
                            .Select(o => o.OrderNumber)
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    results.Add(new SyncCallResultItem(
                        callDto.ClientCallId,
                        existingCall.Id,
                        existingCall.CallNumber,
                        existingCall.SalesOrderId,
                        existingSoNumber,
                        "AlreadySynced",
                        existingCall.SyncedAt ?? DateTime.UtcNow));
                    continue;
                }
            }

            var callNumber = $"CALL-{DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{Guid.NewGuid().ToString()[..4].ToUpper(CultureInfo.InvariantCulture)}";

            var callEntity = new SalesCall(
                request.CompanyId,
                callNumber,
                request.SalesRepId,
                callDto.CustomerId,
                callDto.RouteId,
                callDto.CallDate,
                callDto.StartTime,
                callDto.Notes,
                isOfflineCreated: true,
                clientCallId: callDto.ClientCallId);

            int lineNum = 1;
            foreach (var lineDto in callDto.Lines)
            {
                var callLine = new SalesCallLine(
                    callEntity.Id,
                    lineNum++,
                    lineDto.ItemId,
                    lineDto.ItemCode,
                    lineDto.Description,
                    lineDto.Quantity,
                    lineDto.SellingPrice,
                    lineDto.UnitOfMeasure,
                    lineDto.DiscountPercent);

                callEntity.AddLine(callLine);
            }

            callEntity.CompleteCall(callDto.EndTime ?? DateTime.UtcNow, callDto.Notes);
            callEntity.MarkSynced(DateTime.UtcNow);

            _context.SalesCalls.Add(callEntity);
            await _context.SaveChangesAsync(cancellationToken);

            results.Add(new SyncCallResultItem(
                callDto.ClientCallId ?? callEntity.Id.ToString(),
                callEntity.Id,
                callEntity.CallNumber,
                null,
                null,
                "Parked",
                DateTime.UtcNow));
        }

        return Ok(ApiResponse<SyncUploadResult>.Success(new SyncUploadResult(results, results.Count)));
    }

    /// <summary>
    /// Lists completed sales calls that are parked (synced but not yet processed into Sales Orders).
    /// </summary>
    [HttpGet("salesperson/parked-calls")]
    public async Task<ActionResult<ApiResponse<List<ParkedCallSummary>>>> GetParkedCallsAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? salesRepId,
        CancellationToken cancellationToken)
    {
        var q = _context.SalesCalls.AsNoTracking()
            .Include(c => c.Lines)
            .Where(c => c.Status == SalesCallStatus.Completed && c.SalesOrderId == null);
        q = q.ApplyCompanyScope(HttpContext, x => x.CompanyId, companyId);

        if (salesRepId.HasValue && salesRepId.Value != Guid.Empty)
        {
            q = q.Where(c => c.SalesRepId == salesRepId.Value);
        }

        var calls = await q
            .OrderBy(c => c.CallDate)
            .ThenBy(c => c.StartTime)
            .Take(500)
            .ToListAsync(cancellationToken);

        var custMap = await LoadCustomerMapAsync(calls.FirstOrDefault()?.CompanyId ?? companyId ?? Guid.Empty, cancellationToken);

        var list = calls.Select(c =>
        {
            custMap.TryGetValue(c.CustomerId, out var cust);
            return new ParkedCallSummary(
                c.Id,
                c.CompanyId,
                c.CallNumber,
                c.SalesRepId,
                c.CustomerId,
                cust?.Code ?? string.Empty,
                cust?.Name ?? "Unknown Customer",
                c.CallDate,
                c.StartTime,
                c.EndTime,
                c.TotalAmount,
                c.Lines.Count,
                c.IsOfflineCreated,
                c.ClientCallId,
                c.SyncedAt);
        }).ToList();

        return Ok(ApiResponse<List<ParkedCallSummary>>.Success(list));
    }

    /// <summary>
    /// Processes selected parked sales calls into formal Sales Orders.
    /// </summary>
    [HttpPost("salesperson/process-parked")]
    public async Task<ActionResult<ApiResponse<ProcessParkedOrdersResult>>> ProcessParkedOrdersAsync(
        [FromBody] ProcessParkedOrdersRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SalesCallIds == null || request.SalesCallIds.Count == 0)
        {
            return Ok(ApiResponse<ProcessParkedOrdersResult>.Success(new ProcessParkedOrdersResult(new List<ProcessCallResultItem>(), 0)));
        }

        var results = new List<ProcessCallResultItem>();

        foreach (var callId in request.SalesCallIds.Distinct())
        {
            var call = await _context.SalesCalls
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.Id == callId && c.CompanyId == request.CompanyId, cancellationToken);

            if (call == null)
            {
                results.Add(new ProcessCallResultItem(callId, string.Empty, "NotFound", null, null, "Sales call not found."));
                continue;
            }

            if (call.SalesOrderId.HasValue)
            {
                var existingNumber = await _context.SalesOrders
                    .Where(o => o.Id == call.SalesOrderId.Value)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                results.Add(new ProcessCallResultItem(call.Id, call.CallNumber, "AlreadyProcessed", call.SalesOrderId, existingNumber, "Sales order already exists for this call."));
                continue;
            }

            if (call.Status != SalesCallStatus.Completed)
            {
                results.Add(new ProcessCallResultItem(call.Id, call.CallNumber, "SkippedNotCompleted", null, null, "Only completed calls can be processed."));
                continue;
            }

            if (call.Lines.Count == 0)
            {
                results.Add(new ProcessCallResultItem(call.Id, call.CallNumber, "SkippedNoLines", null, null, "Call has no order lines to process."));
                continue;
            }

            var sequence = await _platformContext.NumberSequences
                .FirstOrDefaultAsync(s => s.CompanyId == call.CompanyId && s.Name == "Sales Order" && s.IsActive, cancellationToken);

            string orderNumber;
            if (sequence != null)
            {
                orderNumber = sequence.GetNextNumber();
                await _platformContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                orderNumber = $"SO-{DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{Guid.NewGuid().ToString()[..4].ToUpper(CultureInfo.InvariantCulture)}";
            }

            var salesOrder = new SalesOrder(
                orderNumber,
                call.CompanyId,
                call.CustomerId,
                call.CallDate,
                salesRepId: call.SalesRepId.ToString());

            int soLineNum = 1;
            foreach (var line in call.Lines.OrderBy(l => l.LineNumber))
            {
                var soLine = new SalesOrderLine(
                    salesOrder.Id,
                    soLineNum++,
                    line.ItemId,
                    line.Description,
                    line.Quantity,
                    line.SellingPrice,
                    line.UnitOfMeasure,
                    line.DiscountPercent,
                    0m);

                salesOrder.AddLine(soLine);
            }

            _context.SalesOrders.Add(salesOrder);
            call.LinkSalesOrder(salesOrder.Id);
            await _context.SaveChangesAsync(cancellationToken);

            results.Add(new ProcessCallResultItem(call.Id, call.CallNumber, "Processed", salesOrder.Id, orderNumber, "Sales order created."));
        }

        return Ok(ApiResponse<ProcessParkedOrdersResult>.Success(
            new ProcessParkedOrdersResult(results, results.Count(r => r.Status == "Processed"))));
    }

    /// <summary>
    /// Gets sales call list with summary info and order status.
    /// </summary>
    [HttpGet("sales-calls")]
    public async Task<ActionResult<ApiResponse<List<SalesCallSummary>>>> GetSalesCallsAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? salesRepId,
        [FromQuery] Guid? customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var q = _context.SalesCalls.AsNoTracking();
        q = q.ApplyCompanyScope(HttpContext, x => x.CompanyId, companyId);

        if (salesRepId.HasValue && salesRepId.Value != Guid.Empty)
        {
            q = q.Where(x => x.SalesRepId == salesRepId.Value);
        }

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            q = q.Where(x => x.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            q = q.Where(x => x.CallDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            q = q.Where(x => x.CallDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var calls = await q
            .OrderByDescending(x => x.CallDate)
            .ThenByDescending(x => x.StartTime)
            .Take(100)
            .ToListAsync(cancellationToken);

        var repIds = calls.Select(x => x.SalesRepId).Distinct().ToList();
        var orderIds = calls.Where(x => x.SalesOrderId.HasValue).Select(x => x.SalesOrderId!.Value).Distinct().ToList();

        var repMap = await _context.SalesReps.AsNoTracking()
            .Where(r => repIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var orderMap = await _context.SalesOrders.AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, cancellationToken);

        var effectiveCompanyId = calls.FirstOrDefault()?.CompanyId ?? companyId ?? Guid.Empty;
        var custMap = await LoadCustomerMapAsync(effectiveCompanyId, cancellationToken);

        var list = calls.Select(c =>
        {
            repMap.TryGetValue(c.SalesRepId, out var repName);
            custMap.TryGetValue(c.CustomerId, out var cust);
            string? soNumber = null;
            if (c.SalesOrderId.HasValue)
            {
                orderMap.TryGetValue(c.SalesOrderId.Value, out soNumber);
            }

            return new SalesCallSummary(
                c.Id,
                c.CompanyId,
                c.CallNumber,
                c.SalesRepId,
                repName ?? "Unknown Rep",
                c.CustomerId,
                cust?.Code ?? string.Empty,
                cust?.Name ?? "Unknown Customer",
                c.CallDate,
                c.StartTime,
                c.EndTime,
                c.Status,
                c.Notes,
                c.SalesOrderId,
                soNumber,
                c.TotalAmount,
                c.IsOfflineCreated,
                c.ClientCallId,
                c.SyncedAt);
        }).ToList();

        return Ok(ApiResponse<List<SalesCallSummary>>.Success(list));
    }

    /// <summary>
    /// Gets sales call detail with lines.
    /// </summary>
    [HttpGet("sales-calls/{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesCallDetail>>> GetSalesCallByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var call = await _context.SalesCalls
            .AsNoTracking()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (call == null)
        {
            return NotFound(ApiResponse<SalesCallDetail>.Failure(new[] { $"Sales call {id} not found." }));
        }

        var rep = await _context.SalesReps.AsNoTracking().FirstOrDefaultAsync(r => r.Id == call.SalesRepId, cancellationToken);
        var custMap = await LoadCustomerMapAsync(call.CompanyId, cancellationToken);
        custMap.TryGetValue(call.CustomerId, out var cust);

        string? soNumber = null;
        if (call.SalesOrderId.HasValue)
        {
            soNumber = await _context.SalesOrders
                .Where(o => o.Id == call.SalesOrderId.Value)
                .Select(o => o.OrderNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var lines = call.Lines.OrderBy(l => l.LineNumber).Select(l => new SalesCallLineDto(
            l.Id,
            l.LineNumber,
            l.ItemId,
            l.ItemCode,
            l.Description,
            l.Quantity,
            l.SellingPrice,
            l.UnitOfMeasure,
            l.DiscountPercent,
            l.LineTotal)).ToList();

        var detail = new SalesCallDetail(
            call.Id,
            call.CompanyId,
            call.CallNumber,
            call.SalesRepId,
            rep?.Name ?? "Unknown Rep",
            call.CustomerId,
            cust?.Code ?? string.Empty,
            cust?.Name ?? "Unknown Customer",
            call.CallDate,
            call.StartTime,
            call.EndTime,
            call.Status,
            call.Notes,
            call.SalesOrderId,
            soNumber,
            call.TotalAmount,
            call.IsOfflineCreated,
            call.ClientCallId,
            call.SyncedAt,
            lines);

        return Ok(ApiResponse<SalesCallDetail>.Success(detail));
    }

    private async Task<List<OfflineCustomerDto>> LoadCustomersForPackageAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var list = new List<OfflineCustomerDto>();
        var conn = _context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CustomerId, Name, LegalName, BillingAddress, BillingCity, BillingState, BillingZipCode, ShippingAddress, ShippingCity, ShippingState, ShippingZipCode, CreditLimit, CurrentBalance FROM ar.Customers WHERE CompanyId = @cmp AND IsActive = 1";
        var p = cmd.CreateParameter();
        p.ParameterName = "@cmp";
        p.Value = companyId;
        cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var code = await reader.IsDBNullAsync(1, cancellationToken) ? string.Empty : reader.GetString(1);
            var name = await reader.IsDBNullAsync(2, cancellationToken) ? string.Empty : reader.GetString(2);
            var contact = await reader.IsDBNullAsync(3, cancellationToken) ? string.Empty : reader.GetString(3);
            var bAddr = await reader.IsDBNullAsync(4, cancellationToken) ? string.Empty : reader.GetString(4);
            var bCity = await reader.IsDBNullAsync(5, cancellationToken) ? string.Empty : reader.GetString(5);
            var bState = await reader.IsDBNullAsync(6, cancellationToken) ? string.Empty : reader.GetString(6);
            var bZip = await reader.IsDBNullAsync(7, cancellationToken) ? string.Empty : reader.GetString(7);
            var sAddr = await reader.IsDBNullAsync(8, cancellationToken) ? string.Empty : reader.GetString(8);
            var sCity = await reader.IsDBNullAsync(9, cancellationToken) ? string.Empty : reader.GetString(9);
            var sState = await reader.IsDBNullAsync(10, cancellationToken) ? string.Empty : reader.GetString(10);
            var sZip = await reader.IsDBNullAsync(11, cancellationToken) ? string.Empty : reader.GetString(11);
            var credit = await reader.IsDBNullAsync(12, cancellationToken) ? 0m : reader.GetDecimal(12);
            var bal = await reader.IsDBNullAsync(13, cancellationToken) ? 0m : reader.GetDecimal(13);

            var billingFull = string.Join(", ", new[] { bAddr, bCity, bState, bZip }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var shippingFull = string.Join(", ", new[] { sAddr, sCity, sState, sZip }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(shippingFull))
            {
                shippingFull = billingFull;
            }

            list.Add(new OfflineCustomerDto(
                id,
                code,
                name,
                contact,
                string.Empty,
                billingFull,
                shippingFull,
                credit,
                bal));
        }

        return list;
    }

    private async Task<List<OfflineProductDto>> LoadProductsForPackageAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var list = new List<OfflineProductDto>();
        var conn = _context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        // Real on-hand totals per item across all warehouses/bins/lots
        var onHandMap = new Dictionary<Guid, decimal>();
        await using (var stockCmd = conn.CreateCommand())
        {
            stockCmd.CommandText = "SELECT ItemId, SUM(OnHandQuantity) FROM inv.ItemStocks WHERE CompanyId = @cmp GROUP BY ItemId";
            var stockParam = stockCmd.CreateParameter();
            stockParam.ParameterName = "@cmp";
            stockParam.Value = companyId;
            stockCmd.Parameters.Add(stockParam);

            await using var stockReader = await stockCmd.ExecuteReaderAsync(cancellationToken);
            while (await stockReader.ReadAsync(cancellationToken))
            {
                onHandMap[stockReader.GetGuid(0)] = await stockReader.IsDBNullAsync(1, cancellationToken) ? 0m : stockReader.GetDecimal(1);
            }
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT i.Id, i.ItemCode, i.Description, i.BaseUnitOfMeasure, i.StandardCost, i.ItemCategoryId, c.CategoryName
FROM inv.Items i
LEFT JOIN inv.ItemCategories c ON c.Id = i.ItemCategoryId
WHERE i.CompanyId = @cmp AND i.Status = 1";
        var p = cmd.CreateParameter();
        p.ParameterName = "@cmp";
        p.Value = companyId;
        cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var code = await reader.IsDBNullAsync(1, cancellationToken) ? string.Empty : reader.GetString(1);
            var desc = await reader.IsDBNullAsync(2, cancellationToken) ? string.Empty : reader.GetString(2);
            var uom = await reader.IsDBNullAsync(3, cancellationToken) ? "EA" : reader.GetString(3);
            var cost = await reader.IsDBNullAsync(4, cancellationToken) ? 0m : reader.GetDecimal(4);
            var catId = await reader.IsDBNullAsync(5, cancellationToken) ? (Guid?)null : reader.GetGuid(5);
            var catName = await reader.IsDBNullAsync(6, cancellationToken) ? string.Empty : reader.GetString(6);
            var onHand = onHandMap.TryGetValue(id, out var qty) ? qty : 0m;

            // Base selling price defaults to StandardCost with standard margin, or at least StandardCost
            var basePrice = cost > 0 ? Math.Round(cost * 1.35m, 2) : 10.00m;

            list.Add(new OfflineProductDto(
                id,
                code,
                desc,
                basePrice,
                uom,
                catId,
                catName,
                onHand));
        }

        return list;
    }

    private async Task<Dictionary<Guid, CustomerBrief>> LoadCustomerMapAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, CustomerBrief>();
        if (companyId == Guid.Empty)
        {
            return map;
        }

        var conn = _context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CustomerId, Name, ShippingAddress, ShippingCity, ShippingState, ShippingZipCode, CreditLimit, CurrentBalance FROM ar.Customers WHERE CompanyId = @cmp";
        var p = cmd.CreateParameter();
        p.ParameterName = "@cmp";
        p.Value = companyId;
        cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var code = await reader.IsDBNullAsync(1, cancellationToken) ? string.Empty : reader.GetString(1);
            var name = await reader.IsDBNullAsync(2, cancellationToken) ? string.Empty : reader.GetString(2);
            var addr = await reader.IsDBNullAsync(3, cancellationToken) ? string.Empty : reader.GetString(3);
            var city = await reader.IsDBNullAsync(4, cancellationToken) ? string.Empty : reader.GetString(4);
            var state = await reader.IsDBNullAsync(5, cancellationToken) ? string.Empty : reader.GetString(5);
            var zip = await reader.IsDBNullAsync(6, cancellationToken) ? string.Empty : reader.GetString(6);
            var cred = await reader.IsDBNullAsync(7, cancellationToken) ? 0m : reader.GetDecimal(7);
            var bal = await reader.IsDBNullAsync(8, cancellationToken) ? 0m : reader.GetDecimal(8);

            var fullAddress = string.Join(", ", new[] { addr, city, state, zip }.Where(s => !string.IsNullOrWhiteSpace(s)));
            map[id] = new CustomerBrief(id, code, name, fullAddress, string.Empty, cred, bal);
        }

        return map;
    }
}

// DTOs for Sync Package and Upload
public record OfflineRepDto(Guid Id, string Code, string Name, decimal CommissionRate, string? Email);

public record OfflineRouteDto(
    Guid Id,
    Guid SalesRepId,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string ContactName,
    string Phone,
    string Address,
    int DayOfWeek,
    int SequenceOrder,
    string? EstimatedTime,
    string? Notes,
    decimal CreditLimit,
    decimal CurrentBalance);

public record OfflineCustomerDto(
    Guid Id,
    string CustomerCode,
    string CustomerName,
    string ContactName,
    string Phone,
    string BillingAddress,
    string ShippingAddress,
    decimal CreditLimit,
    decimal CurrentBalance);

public record OfflineProductDto(
    Guid Id,
    string ItemCode,
    string Description,
    decimal BasePrice,
    string UnitOfMeasure,
    Guid? ItemCategoryId,
    string CategoryName,
    decimal OnHandQty);

public record OfflinePricingRuleDto(
    Guid Id,
    Guid? CustomerId,
    Guid? ItemId,
    Guid? ItemCategoryId,
    decimal DiscountPercent,
    decimal? UnitPriceOverride,
    decimal? MinimumQuantity,
    int PrioritySequence);

public record OfflineSyncPackage(
    Guid CompanyId,
    OfflineRepDto Rep,
    DateTime ServerTime,
    List<OfflineRouteDto> Routes,
    List<OfflineCustomerDto> Customers,
    List<OfflineProductDto> Products,
    List<OfflinePricingRuleDto> PricingRules);

public record SyncUploadRequest(
    Guid CompanyId,
    Guid SalesRepId,
    List<OfflineSalesCallUploadDto> Calls);

public record OfflineSalesCallUploadDto(
    string? ClientCallId,
    Guid CustomerId,
    Guid? RouteId,
    DateTime CallDate,
    DateTime StartTime,
    DateTime? EndTime,
    string? Notes,
    List<OfflineSalesCallLineUploadDto> Lines);

public record OfflineSalesCallLineUploadDto(
    Guid ItemId,
    string ItemCode,
    string Description,
    decimal Quantity,
    decimal SellingPrice,
    string UnitOfMeasure,
    decimal DiscountPercent,
    decimal LineTotal);

public record SyncCallResultItem(
    string ClientCallId,
    Guid CallId,
    string CallNumber,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    string Status,
    DateTime SyncedAt);

public record SyncUploadResult(
    List<SyncCallResultItem> Items,
    int TotalProcessed);

public record ParkedCallSummary(
    Guid Id,
    Guid CompanyId,
    string CallNumber,
    Guid SalesRepId,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    DateTime CallDate,
    DateTime StartTime,
    DateTime? EndTime,
    decimal TotalAmount,
    int LineCount,
    bool IsOfflineCreated,
    string? ClientCallId,
    DateTime? SyncedAt);

public record ProcessParkedOrdersRequest(
    Guid CompanyId,
    List<Guid> SalesCallIds);

public record ProcessCallResultItem(
    Guid CallId,
    string CallNumber,
    string Status,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    string Message);

public record ProcessParkedOrdersResult(
    List<ProcessCallResultItem> Items,
    int TotalProcessed);

public record SalesCallSummary(
    Guid Id,
    Guid CompanyId,
    string CallNumber,
    Guid SalesRepId,
    string SalesRepName,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    DateTime CallDate,
    DateTime StartTime,
    DateTime? EndTime,
    SalesCallStatus Status,
    string? Notes,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    decimal TotalAmount,
    bool IsOfflineCreated,
    string? ClientCallId,
    DateTime? SyncedAt);

public record SalesCallLineDto(
    Guid Id,
    int LineNumber,
    Guid ItemId,
    string ItemCode,
    string Description,
    decimal Quantity,
    decimal SellingPrice,
    string UnitOfMeasure,
    decimal DiscountPercent,
    decimal LineTotal);

public record SalesCallDetail(
    Guid Id,
    Guid CompanyId,
    string CallNumber,
    Guid SalesRepId,
    string SalesRepName,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    DateTime CallDate,
    DateTime StartTime,
    DateTime? EndTime,
    SalesCallStatus Status,
    string? Notes,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    decimal TotalAmount,
    bool IsOfflineCreated,
    string? ClientCallId,
    DateTime? SyncedAt,
    List<SalesCallLineDto> Lines);
