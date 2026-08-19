// <copyright file="SalesRepsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/om/sales-reps")]
public class SalesRepsController : ControllerBase
{
    private readonly OmDbContext _context;

    public SalesRepsController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalesRepSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var q = _context.SalesReps.AsNoTracking();
        q = companyId is not null ? q.Where(x => x.CompanyId == companyId) : q;

        var list = await q.OrderBy(x => x.Code)
            .Select(x => new SalesRepSummary(x.Id, x.Code, x.Name, x.CommissionRate, x.TerritoryId, x.Email, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<SalesRepSummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesRepSummary>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.SalesReps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<SalesRepSummary>.Failure(new[] { $"Sales rep {id} not found." }));
        }

        return Ok(ApiResponse<SalesRepSummary>.Success(new SalesRepSummary(e.Id, e.Code, e.Name, e.CommissionRate, e.TerritoryId, e.Email, e.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateSalesRepRequest r, CancellationToken cancellationToken)
    {
        var e = new SalesRep(r.CompanyId, r.Code, r.Name, r.CommissionRate, r.TerritoryId, r.Email);
        _context.SalesReps.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateSalesRepRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.SalesReps.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales rep {id} not found." }));
        }

        e.Update(r.Name, r.CommissionRate, r.TerritoryId, r.IsActive, r.Email);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.SalesReps.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Sales rep {id} not found." }));
        }

        _context.SalesReps.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }
}

public record SalesRepSummary(Guid Id, string Code, string Name, decimal CommissionRate, Guid? TerritoryId, string? Email, bool IsActive);
public record CreateSalesRepRequest(Guid CompanyId, string Code, string Name, decimal CommissionRate, Guid? TerritoryId, string? Email);
public record UpdateSalesRepRequest(string Name, decimal CommissionRate, Guid? TerritoryId, bool IsActive, string? Email);
