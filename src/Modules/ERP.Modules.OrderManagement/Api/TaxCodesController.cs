// <copyright file="TaxCodesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Domain.Services;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/tax-codes")]
public class TaxCodesController : ControllerBase
{
    private readonly OmDbContext _context;

    public TaxCodesController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TaxCodeSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var q = _context.TaxCodes.AsNoTracking();
        q = companyId is not null ? q.Where(x => x.CompanyId == companyId) : q;

        var list = await q.OrderBy(x => x.Jurisdiction).ThenBy(x => x.Code)
            .Select(x => new TaxCodeSummary(x.Id, x.Code, x.Description, x.Jurisdiction, x.Rate, x.IsTaxable, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<TaxCodeSummary>>.Success(list));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateTaxCodeRequest r, CancellationToken cancellationToken)
    {
        var e = new TaxCode(r.CompanyId, r.Code, r.Description, r.Jurisdiction, r.Rate, r.IsTaxable, r.EffectiveFrom, r.EffectiveTo);
        _context.TaxCodes.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateTaxCodeRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.TaxCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Tax code {id} not found." }));
        }

        e.Update(r.Description, r.Rate, r.IsTaxable, r.EffectiveFrom, r.EffectiveTo, r.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.TaxCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Tax code {id} not found." }));
        }

        _context.TaxCodes.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }

    /// <summary>Resolve the applicable tax for a (jurisdiction, amount, item, customer) context.</summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<TaxResult>>> CalculateAsync([FromBody] CalculateTaxRequest r, CancellationToken cancellationToken)
    {
        var codes = await _context.TaxCodes.AsNoTracking()
            .Where(x => x.CompanyId == r.CompanyId)
            .ToListAsync(cancellationToken);

        var result = TaxEngine.CalculateTax(r.TaxableAmount, r.Jurisdiction, r.ItemTaxable, r.CustomerExempt, codes, r.AsOf);
        return Ok(ApiResponse<TaxResult>.Success(result));
    }
}

public record TaxCodeSummary(Guid Id, string Code, string Description, string Jurisdiction, decimal Rate, bool IsTaxable, bool IsActive);
public record CreateTaxCodeRequest(Guid CompanyId, string Code, string Description, string Jurisdiction, decimal Rate, bool IsTaxable, DateTime? EffectiveFrom, DateTime? EffectiveTo);
public record UpdateTaxCodeRequest(string Description, decimal Rate, bool IsTaxable, DateTime? EffectiveFrom, DateTime? EffectiveTo, bool IsActive);
public record CalculateTaxRequest(Guid CompanyId, decimal TaxableAmount, string? Jurisdiction, bool ItemTaxable, bool CustomerExempt, DateTime AsOf);
