// <copyright file="SoDController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable S6960

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/sod")]
public class SoDController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ISodService _sodService;

    public SoDController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, ISodService sodService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _sodService = sodService ?? throw new ArgumentNullException(nameof(sodService));
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<SoDRuleDto>>> GetRules([FromQuery] string? module, CancellationToken cancellationToken)
    {
        var rules = await _sodService.GetActiveRulesAsync(module, cancellationToken);
        return Ok(rules.Select(MapRuleToDto).ToList());
    }

    [HttpGet("rules/{id:guid}")]
    public async Task<ActionResult<SoDRuleDto>> GetRuleById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.SoDRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        return Ok(MapRuleToDto(rule));
    }

    [HttpPost("rules")]
    public async Task<ActionResult<SoDRuleDto>> CreateRule([FromBody] CreateSoDRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = new SoDRule(
            request.Module,
            request.ActionA,
            request.ActionB,
            request.Description,
            request.DocumentType,
            request.ThresholdAmount);

        await _unitOfWork.SoDRules.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(SoDRule),
            rule.Id,
            "system",
            newValues: new { request.Module, request.ActionA, request.ActionB },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetRuleById), new { id = rule.Id }, MapRuleToDto(rule));
    }

    [HttpPut("rules/{id:guid}")]
    public async Task<ActionResult<SoDRuleDto>> UpdateRule(Guid id, [FromBody] UpdateSoDRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.SoDRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        var oldValues = new { rule.Module, rule.ActionA, rule.ActionB, rule.Description };

        rule.Update(request.Module, request.ActionA, request.ActionB, request.Description, request.DocumentType, request.ThresholdAmount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Updated",
            nameof(SoDRule),
            rule.Id,
            "system",
            oldValues: oldValues,
            newValues: new { request.Module, request.ActionA, request.ActionB },
            cancellationToken: cancellationToken);

        return Ok(MapRuleToDto(rule));
    }

    [HttpDelete("rules/{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.SoDRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        rule.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Deleted",
            nameof(SoDRule),
            rule.Id,
            "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("rules/{id:guid}/activate")]
    public async Task<IActionResult> ActivateRule(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.SoDRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        rule.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("rules/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateRule(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.SoDRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        rule.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("check")]
    public async Task<ActionResult<bool>> CheckConflict([FromBody] CheckConflictRequest request, CancellationToken cancellationToken)
    {
        var hasConflict = await _sodService.CheckConflictAsync(
            request.Module,
            request.DocumentType,
            request.UserId,
            request.Action,
            request.Amount,
            cancellationToken);

        return Ok(hasConflict);
    }

    [HttpGet("conflicts")]
    public async Task<ActionResult<IReadOnlyList<SoDConflictDto>>> GetConflicts(
        [FromQuery] string? userId,
        [FromQuery] bool? resolved,
        CancellationToken cancellationToken)
    {
        var conflicts = await _sodService.GetConflictsAsync(userId, resolved, cancellationToken);
        return Ok(conflicts.Select(MapConflictToDto).ToList());
    }

    [HttpGet("conflicts/{id:guid}")]
    public async Task<ActionResult<SoDConflictDto>> GetConflictById(Guid id, CancellationToken cancellationToken)
    {
        var conflict = await _unitOfWork.SoDConflicts.GetByIdAsync(id, cancellationToken);
        if (conflict == null)
            return NotFound();

        return Ok(MapConflictToDto(conflict));
    }

    [HttpPost("conflicts/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveConflict(Guid id, [FromBody] ResolveConflictRequest request, CancellationToken cancellationToken)
    {
        await _sodService.ResolveConflictAsync(id, request.Resolution, request.ResolvedBy, cancellationToken);
        return Ok();
    }

    [HttpPost("conflicts")]
    public async Task<ActionResult<SoDConflictDto>> LogConflict([FromBody] SoDConflictDto dto, CancellationToken cancellationToken)
    {
        await _sodService.LogConflictAsync(
            dto.RuleId,
            dto.UserId,
            dto.Module,
            dto.DocumentType,
            dto.DocumentId,
            dto.ConflictType,
            cancellationToken);

        return Ok();
    }

    private static SoDRuleDto MapRuleToDto(SoDRule rule)
    {
        return new SoDRuleDto(
            rule.Id,
            rule.Module,
            rule.ActionA,
            rule.ActionB,
            rule.Description,
            rule.DocumentType,
            rule.IsActive,
            rule.ThresholdAmount,
            rule.CreatedOn,
            rule.ModifiedOn);
    }

    private static SoDConflictDto MapConflictToDto(SoDConflict conflict)
    {
        return new SoDConflictDto(
            conflict.Id,
            conflict.RuleId,
            conflict.UserId,
            conflict.Module,
            conflict.DocumentType,
            conflict.DocumentId,
            conflict.ConflictType,
            conflict.DetectedOn,
            conflict.Resolved,
            conflict.Resolution,
            conflict.ResolvedBy,
            conflict.ResolvedOn);
    }
}
