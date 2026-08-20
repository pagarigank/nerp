// <copyright file="PutAwayPickingController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

[ApiController]
[Route("api/v1/inventory/put-away-picking-rules")]
public class PutAwayPickingController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public PutAwayPickingController(InventoryDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PutAwayPickingRuleDto>>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        var query = _context.PutAwayPickingRules.AsQueryable();
        query = query.ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId);
        if (warehouseId.HasValue) query = query.Where(r => r.WarehouseId == warehouseId.Value);
        var rules = await query.OrderBy(r => r.BinId).Take(1000).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PutAwayPickingRuleDto>>.Success(rules.Select(MapToDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PutAwayPickingRuleDto>>> Create([FromBody] CreatePutAwayPickingRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = new PutAwayPickingRule(request.CompanyId, request.WarehouseId, request.BinId, request.PutAwayRank, request.PickSequence, request.PickingPolicy);
        _context.PutAwayPickingRules.Add(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), ApiResponse<PutAwayPickingRuleDto>.Success(MapToDto(rule)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PutAwayPickingRuleDto>>> Update(Guid id, [FromBody] UpdatePutAwayPickingRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _context.PutAwayPickingRules.FindAsync(new object[] { id }, cancellationToken);
        if (rule is null) return NotFound(ApiResponse<PutAwayPickingRuleDto>.Failure(["Rule not found."]));
        rule.Update(request.PutAwayRank, request.PickSequence, request.PickingPolicy);
        _context.PutAwayPickingRules.Update(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<PutAwayPickingRuleDto>.Success(MapToDto(rule)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _context.PutAwayPickingRules.FindAsync(new object[] { id }, cancellationToken);
        if (rule is null) return NotFound(ApiResponse<string>.Failure(["Rule not found."]));
        _context.PutAwayPickingRules.Remove(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("deleted"));
    }

    /// <summary>
    /// Recommend the next put-away bin (lowest rank) or pick bin (lowest sequence /
    /// FIFO-FEFO lot) for a warehouse. Used by the warehouse UX to guide operators.
    /// </summary>
    [HttpGet("recommend")]
    public async Task<ActionResult<ApiResponse<PutAwayPickingRecommendationDto>>> Recommend(
        [FromQuery] Guid companyId, [FromQuery] Guid warehouseId, [FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var effectiveMode = string.IsNullOrEmpty(mode) ? "pick" : mode;
        var rules = await _context.PutAwayPickingRules
            .Where(r => r.CompanyId == companyId && r.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
            return Ok(ApiResponse<PutAwayPickingRecommendationDto>.Success(new PutAwayPickingRecommendationDto { Mode = effectiveMode, BinId = null, Reason = "No rules configured for this warehouse." }));

        PutAwayPickingRule? chosen = effectiveMode == "putaway"
            ? rules.OrderBy(r => r.PutAwayRank).First()
            : rules.OrderBy(r => r.PickSequence).First();

        var reason = effectiveMode == "putaway"
            ? $"Bin with lowest put-away rank ({chosen!.PutAwayRank})."
            : $"Bin with lowest pick sequence ({chosen!.PickSequence}); policy {chosen.PickingPolicy}.";

        return Ok(ApiResponse<PutAwayPickingRecommendationDto>.Success(new PutAwayPickingRecommendationDto
        {
            Mode = effectiveMode,
            BinId = chosen.BinId,
            PickingPolicy = chosen.PickingPolicy.ToString(),
            Reason = reason,
        }));
    }

    private static PutAwayPickingRuleDto MapToDto(PutAwayPickingRule r) => new PutAwayPickingRuleDto
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        WarehouseId = r.WarehouseId,
        BinId = r.BinId,
        PutAwayRank = r.PutAwayRank,
        PickSequence = r.PickSequence,
        PickingPolicy = r.PickingPolicy.ToString(),
    };
}

public class PutAwayPickingRuleDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid BinId { get; set; }
    public int PutAwayRank { get; set; }
    public int PickSequence { get; set; }
    public string PickingPolicy { get; set; } = string.Empty;
}

public class CreatePutAwayPickingRuleRequest
{
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid BinId { get; set; }
    public int PutAwayRank { get; set; }
    public int PickSequence { get; set; }
    public PickingPolicy PickingPolicy { get; set; }
}

public class UpdatePutAwayPickingRuleRequest
{
    public int PutAwayRank { get; set; }
    public int PickSequence { get; set; }
    public PickingPolicy PickingPolicy { get; set; }
}

public class PutAwayPickingRecommendationDto
{
    public string Mode { get; set; } = string.Empty;
    public Guid? BinId { get; set; }
    public string? PickingPolicy { get; set; }
    public string Reason { get; set; } = string.Empty;
}
