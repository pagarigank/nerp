// <copyright file="SalespersonRoutesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Data;
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
[Route("api/v{version:apiVersion}/om/salesperson-routes")]
public class SalespersonRoutesController : ControllerBase
{
    private readonly OmDbContext _context;

    public SalespersonRoutesController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalespersonRouteSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? salesRepId,
        [FromQuery] DayOfWeek? dayOfWeek,
        CancellationToken cancellationToken)
    {
        var q = _context.SalespersonRoutes.AsNoTracking();
        q = q.ApplyCompanyScope(HttpContext, x => x.CompanyId, companyId);

        if (salesRepId.HasValue && salesRepId.Value != Guid.Empty)
        {
            q = q.Where(x => x.SalesRepId == salesRepId.Value);
        }

        if (dayOfWeek.HasValue)
        {
            q = q.Where(x => x.DayOfWeek == dayOfWeek.Value);
        }

        var routes = await q
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.SequenceOrder)
            .ToListAsync(cancellationToken);

        var salesReps = await _context.SalesReps.AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var effectiveCompanyId = routes.FirstOrDefault()?.CompanyId ?? companyId ?? Guid.Empty;
        var customerMap = await LoadCustomerMapAsync(effectiveCompanyId, cancellationToken);

        var result = routes.Select(r =>
        {
            salesReps.TryGetValue(r.SalesRepId, out var repName);
            customerMap.TryGetValue(r.CustomerId, out var cust);

            return new SalespersonRouteSummary(
                r.Id,
                r.CompanyId,
                r.SalesRepId,
                repName ?? "Unknown Rep",
                r.CustomerId,
                cust?.Code ?? string.Empty,
                cust?.Name ?? "Unknown Customer",
                r.DayOfWeek,
                (int)r.DayOfWeek,
                r.SequenceOrder,
                r.EstimatedTime,
                r.Notes,
                cust?.Address,
                cust?.Phone,
                cust?.CreditLimit ?? 0m,
                cust?.CurrentBalance ?? 0m,
                r.IsActive);
        }).ToList();

        return Ok(ApiResponse<List<SalespersonRouteSummary>>.Success(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalespersonRouteSummary>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var r = await _context.SalespersonRoutes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (r is null)
        {
            return NotFound(ApiResponse<SalespersonRouteSummary>.Failure(new[] { $"Route {id} not found." }));
        }

        var rep = await _context.SalesReps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.SalesRepId, cancellationToken);
        var custMap = await LoadCustomerMapAsync(r.CompanyId, cancellationToken);
        custMap.TryGetValue(r.CustomerId, out var cust);

        var summary = new SalespersonRouteSummary(
            r.Id,
            r.CompanyId,
            r.SalesRepId,
            rep?.Name ?? "Unknown Rep",
            r.CustomerId,
            cust?.Code ?? string.Empty,
            cust?.Name ?? "Unknown Customer",
            r.DayOfWeek,
            (int)r.DayOfWeek,
            r.SequenceOrder,
            r.EstimatedTime,
            r.Notes,
            cust?.Address,
            cust?.Phone,
            cust?.CreditLimit ?? 0m,
            cust?.CurrentBalance ?? 0m,
            r.IsActive);

        return Ok(ApiResponse<SalespersonRouteSummary>.Success(summary));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateSalespersonRouteRequest req, CancellationToken cancellationToken)
    {
        var existing = await _context.SalespersonRoutes
            .FirstOrDefaultAsync(x => x.CompanyId == req.CompanyId &&
                                      x.SalesRepId == req.SalesRepId &&
                                      x.DayOfWeek == req.DayOfWeek &&
                                      x.CustomerId == req.CustomerId, cancellationToken);

        if (existing != null)
        {
            existing.Update(req.DayOfWeek, req.SequenceOrder, req.EstimatedTime, req.Notes, true);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<Guid>.Success(existing.Id));
        }

        var seq = req.SequenceOrder;
        if (seq <= 0)
        {
            var maxSeq = await _context.SalespersonRoutes
                .Where(x => x.CompanyId == req.CompanyId && x.SalesRepId == req.SalesRepId && x.DayOfWeek == req.DayOfWeek)
                .Select(x => (int?)x.SequenceOrder)
                .MaxAsync(cancellationToken) ?? 0;
            seq = maxSeq + 1;
        }

        var route = new SalespersonRoute(
            req.CompanyId,
            req.SalesRepId,
            req.CustomerId,
            req.DayOfWeek,
            seq,
            req.EstimatedTime,
            req.Notes);

        _context.SalespersonRoutes.Add(route);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(route.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateSalespersonRouteRequest req, CancellationToken cancellationToken)
    {
        var route = await _context.SalespersonRoutes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (route is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Route {id} not found." }));
        }

        route.Update(req.DayOfWeek, req.SequenceOrder, req.EstimatedTime, req.Notes, req.IsActive);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var route = await _context.SalespersonRoutes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (route is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Route {id} not found." }));
        }

        _context.SalespersonRoutes.Remove(route);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<string>.Success("Deleted"));
    }

    [HttpPost("bulk-assign")]
    public async Task<ActionResult<ApiResponse<int>>> BulkAssignAsync([FromBody] BulkAssignRoutesRequest req, CancellationToken cancellationToken)
    {
        if (req.CustomerIds == null || req.CustomerIds.Count == 0)
        {
            return Ok(ApiResponse<int>.Success(0));
        }

        var existing = await _context.SalespersonRoutes
            .Where(x => x.CompanyId == req.CompanyId && x.SalesRepId == req.SalesRepId && x.DayOfWeek == req.DayOfWeek)
            .ToListAsync(cancellationToken);

        var existingCustIds = existing.Select(x => x.CustomerId).ToHashSet();
        var maxSeq = existing.Select(x => (int?)x.SequenceOrder).Max() ?? 0;

        int added = 0;
        foreach (var custId in req.CustomerIds)
        {
            if (existingCustIds.Contains(custId))
            {
                continue;
            }

            maxSeq++;
            var route = new SalespersonRoute(
                req.CompanyId,
                req.SalesRepId,
                custId,
                req.DayOfWeek,
                maxSeq,
                null,
                null);

            _context.SalespersonRoutes.Add(route);
            added++;
        }

        if (added > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<int>.Success(added));
    }

    [HttpPost("reorder")]
    public async Task<ActionResult<ApiResponse<string>>> ReorderAsync([FromBody] ReorderRoutesRequest req, CancellationToken cancellationToken)
    {
        if (req.RouteIds == null || req.RouteIds.Count == 0)
        {
            return Ok(ApiResponse<string>.Success("No changes"));
        }

        var routes = await _context.SalespersonRoutes
            .Where(x => req.RouteIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var routeMap = routes.ToDictionary(r => r.Id);

        for (int i = 0; i < req.RouteIds.Count; i++)
        {
            if (routeMap.TryGetValue(req.RouteIds[i], out var route))
            {
                route.SetSequence(i + 1);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Reordered"));
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

public record CustomerBrief(Guid Id, string Code, string Name, string Address, string Phone, decimal CreditLimit, decimal CurrentBalance);

public record SalespersonRouteSummary(
    Guid Id,
    Guid CompanyId,
    Guid SalesRepId,
    string SalesRepName,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    DayOfWeek DayOfWeek,
    int DayOfWeekNumber,
    int SequenceOrder,
    string? EstimatedTime,
    string? Notes,
    string? CustomerAddress,
    string? CustomerPhone,
    decimal CreditLimit,
    decimal CurrentBalance,
    bool IsActive);

public record CreateSalespersonRouteRequest(
    Guid CompanyId,
    Guid SalesRepId,
    Guid CustomerId,
    DayOfWeek DayOfWeek,
    int SequenceOrder,
    string? EstimatedTime,
    string? Notes);

public record UpdateSalespersonRouteRequest(
    DayOfWeek DayOfWeek,
    int SequenceOrder,
    string? EstimatedTime,
    string? Notes,
    bool IsActive);

public record BulkAssignRoutesRequest(
    Guid CompanyId,
    Guid SalesRepId,
    DayOfWeek DayOfWeek,
    List<Guid> CustomerIds);

public record ReorderRoutesRequest(
    List<Guid> RouteIds);
