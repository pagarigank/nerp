// <copyright file="SalesTerritoriesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

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
[Route("api/v{version:apiVersion}/om/sales-territories")]
public class SalesTerritoriesController : ControllerBase
{
    private readonly OmDbContext _context;

    public SalesTerritoriesController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalesTerritorySummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var q = _context.SalesTerritories.AsNoTracking();
        q = q.ApplyCompanyScope(HttpContext, x => x.CompanyId, companyId);

        var list = await q.OrderBy(x => x.Code)
            .Select(x => new SalesTerritorySummary(x.Id, x.Code, x.Name, x.Region, x.DefaultCommissionRate, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<SalesTerritorySummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesTerritorySummary>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.SalesTerritories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<SalesTerritorySummary>.Failure(new[] { $"Territory {id} not found." }));
        }

        return Ok(ApiResponse<SalesTerritorySummary>.Success(new SalesTerritorySummary(e.Id, e.Code, e.Name, e.Region, e.DefaultCommissionRate, e.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateSalesTerritoryRequest r, CancellationToken cancellationToken)
    {
        var e = new SalesTerritory(r.CompanyId, r.Code, r.Name, r.Region, r.DefaultCommissionRate);
        _context.SalesTerritories.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateSalesTerritoryRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.SalesTerritories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Territory {id} not found." }));
        }

        e.Update(r.Name, r.Region, r.DefaultCommissionRate, r.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.SalesTerritories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Territory {id} not found." }));
        }

        _context.SalesTerritories.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }
}

public record SalesTerritorySummary(Guid Id, string Code, string Name, string? Region, decimal DefaultCommissionRate, bool IsActive);
public record CreateSalesTerritoryRequest(Guid CompanyId, string Code, string Name, string? Region, decimal DefaultCommissionRate);
public record UpdateSalesTerritoryRequest(string Name, string? Region, decimal DefaultCommissionRate, bool IsActive);
