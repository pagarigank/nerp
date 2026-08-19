// <copyright file="PricingRulesController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/om/pricing-rules")]
public class PricingRulesController : ControllerBase
{
    private readonly OmDbContext _context;

    public PricingRulesController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PricingRuleSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var q = _context.PricingRules.AsNoTracking();
        q = companyId is not null ? q.Where(x => x.CompanyId == companyId) : q;

        var list = await q.OrderBy(x => x.PrioritySequence).ThenBy(x => x.Code)
            .Select(x => new PricingRuleSummary(x.Id, x.Code, x.Description, x.Scope, x.PrioritySequence, x.DiscountPercent, x.UnitPriceOverride, x.CustomerId, x.ItemId, x.MinimumQuantity, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<PricingRuleSummary>>.Success(list));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreatePricingRuleRequest r, CancellationToken cancellationToken)
    {
        var e = new PricingRule(r.CompanyId, r.Code, r.Description, r.Scope, r.PrioritySequence, r.DiscountPercent, r.UnitPriceOverride, r.CustomerId, r.ItemId, r.MinimumQuantity, r.EffectiveFrom, r.EffectiveTo);
        _context.PricingRules.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdatePricingRuleRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.PricingRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Pricing rule {id} not found." }));
        }

        e.Update(r.Description, r.PrioritySequence, r.DiscountPercent, r.UnitPriceOverride, r.MinimumQuantity, r.EffectiveFrom, r.EffectiveTo, r.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.PricingRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Pricing rule {id} not found." }));
        }

        _context.PricingRules.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }

    /// <summary>Evaluate the winning price for a (customer, item, qty) context against active rules.</summary>
    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<PricingResult>>> EvaluateAsync([FromBody] EvaluatePriceRequest r, CancellationToken cancellationToken)
    {
        var rules = await _context.PricingRules.AsNoTracking()
            .Where(x => x.CompanyId == r.CompanyId)
            .ToListAsync(cancellationToken);

        var result = PricingEngine.CalculatePrice(r.BaseUnitPrice, r.CustomerId, r.ItemId, r.Quantity, rules, r.AsOf);
        return Ok(ApiResponse<PricingResult>.Success(result));
    }
}

public record PricingRuleSummary(Guid Id, string Code, string Description, PricingRuleScope Scope, int PrioritySequence, decimal DiscountPercent, decimal? UnitPriceOverride, Guid? CustomerId, Guid? ItemId, decimal? MinimumQuantity, bool IsActive);
public record CreatePricingRuleRequest(Guid CompanyId, string Code, string Description, PricingRuleScope Scope, int PrioritySequence, decimal DiscountPercent, decimal? UnitPriceOverride, Guid? CustomerId, Guid? ItemId, decimal? MinimumQuantity, DateTime? EffectiveFrom, DateTime? EffectiveTo);
public record UpdatePricingRuleRequest(string Description, int PrioritySequence, decimal DiscountPercent, decimal? UnitPriceOverride, decimal? MinimumQuantity, DateTime? EffectiveFrom, DateTime? EffectiveTo, bool IsActive);
public record EvaluatePriceRequest(Guid CompanyId, decimal BaseUnitPrice, Guid? CustomerId, Guid? ItemId, decimal Quantity, DateTime AsOf);
