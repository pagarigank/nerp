// <copyright file="SalesOrderTypesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/sales-order-types")]
public class SalesOrderTypesController : ControllerBase
{
    private readonly OmDbContext _context;

    public SalesOrderTypesController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalesOrderTypeSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var q = _context.SalesOrderTypes.AsNoTracking();
        q = companyId is not null ? q.Where(x => x.CompanyId == companyId) : q;

        var list = await q.OrderBy(x => x.Code)
            .Select(x => new SalesOrderTypeSummary(x.Id, x.Code, x.Description, x.TypeCode, x.RevenueAccountId, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<SalesOrderTypeSummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesOrderTypeSummary>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.SalesOrderTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<SalesOrderTypeSummary>.Failure(new[] { $"Order type {id} not found." }));
        }

        return Ok(ApiResponse<SalesOrderTypeSummary>.Success(new SalesOrderTypeSummary(e.Id, e.Code, e.Description, e.TypeCode, e.RevenueAccountId, e.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateSalesOrderTypeRequest r, CancellationToken cancellationToken)
    {
        var e = new SalesOrderType(r.CompanyId, r.Code, r.Description, r.TypeCode, r.RevenueAccountId);
        _context.SalesOrderTypes.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateSalesOrderTypeRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.SalesOrderTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Order type {id} not found." }));
        }

        e.Update(r.Description, r.TypeCode, r.RevenueAccountId, r.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.SalesOrderTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Order type {id} not found." }));
        }

        _context.SalesOrderTypes.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }
}

public record SalesOrderTypeSummary(Guid Id, string Code, string Description, SalesOrderTypeCode TypeCode, Guid? RevenueAccountId, bool IsActive);
public record CreateSalesOrderTypeRequest(Guid CompanyId, string Code, string Description, SalesOrderTypeCode TypeCode, Guid? RevenueAccountId);
public record UpdateSalesOrderTypeRequest(string Description, SalesOrderTypeCode TypeCode, Guid? RevenueAccountId, bool IsActive);
