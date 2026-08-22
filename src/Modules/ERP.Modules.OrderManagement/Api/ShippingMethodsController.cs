// <copyright file="ShippingMethodsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/om/shipping-methods")]
public class ShippingMethodsController : ControllerBase
{
    private readonly OmDbContext _context;

    public ShippingMethodsController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ShippingMethodSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var q = _context.ShippingMethods.AsNoTracking();
        q = q.ApplyCompanyScope(HttpContext, x => x.CompanyId, companyId);

        var list = await q.OrderBy(x => x.Code)
            .Select(x => new ShippingMethodSummary(x.Id, x.Code, x.Description, x.Carrier, x.BaseCost, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ShippingMethodSummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShippingMethodSummary>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.ShippingMethods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<ShippingMethodSummary>.Failure(new[] { $"Shipping method {id} not found." }));
        }

        return Ok(ApiResponse<ShippingMethodSummary>.Success(new ShippingMethodSummary(e.Id, e.Code, e.Description, e.Carrier, e.BaseCost, e.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateShippingMethodRequest r, CancellationToken cancellationToken)
    {
        var e = new ShippingMethod(r.CompanyId, r.Code, r.Description, r.Carrier, r.BaseCost, r.TrackingUrlTemplate);
        _context.ShippingMethods.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateShippingMethodRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.ShippingMethods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Shipping method {id} not found." }));
        }

        e.Update(r.Description, r.Carrier, r.BaseCost, r.IsActive, r.TrackingUrlTemplate);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.ShippingMethods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Shipping method {id} not found." }));
        }

        _context.ShippingMethods.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }
}

public record ShippingMethodSummary(Guid Id, string Code, string Description, string? Carrier, decimal BaseCost, bool IsActive);
#pragma warning disable CA1054, CA1056
public record CreateShippingMethodRequest(Guid CompanyId, string Code, string Description, string? Carrier, decimal BaseCost, string? TrackingUrlTemplate);
public record UpdateShippingMethodRequest(string Description, string? Carrier, decimal BaseCost, bool IsActive, string? TrackingUrlTemplate);
#pragma warning restore CA1054, CA1056
