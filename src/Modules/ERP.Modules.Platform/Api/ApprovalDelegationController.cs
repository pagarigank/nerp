// <copyright file="ApprovalDelegationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/approval-delegations")]
public class ApprovalDelegationController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public ApprovalDelegationController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalDelegationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var delegations = await _unitOfWork.ApprovalDelegations.GetAllAsync(cancellationToken);
        return Ok(delegations.OrderByDescending(d => d.StartsOn).Select(MapToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ApprovalDelegationDto>> Create([FromBody] CreateApprovalDelegationRequest request, CancellationToken cancellationToken)
    {
        var delegation = new ApprovalDelegation(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.StartsOn,
            request.EndsOn,
            request.Module,
            request.DocumentType,
            request.WorkflowId);

        await _unitOfWork.ApprovalDelegations.AddAsync(delegation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(ApprovalDelegation),
            delegation.Id,
            "system",
            newValues: new { request.DelegatorUserId, request.DelegateUserId },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = delegation.Id }, MapToDto(delegation));
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var delegation = await _unitOfWork.ApprovalDelegations.GetByIdAsync(id, cancellationToken);
        if (delegation == null)
            return NotFound();
        delegation.Revoke();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var delegation = await _unitOfWork.ApprovalDelegations.GetByIdAsync(id, cancellationToken);
        if (delegation == null)
            return NotFound();
        delegation.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ApprovalDelegationDto MapToDto(ApprovalDelegation d) => new(
        d.Id, d.DelegatorUserId, d.DelegateUserId, d.Module, d.DocumentType, d.WorkflowId,
        d.StartsOn, d.EndsOn, d.IsActive);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/approval-escalations")]
public class ApprovalEscalationController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ApprovalEscalationController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalEscalationPolicyDto>>> GetAll([FromQuery] Guid? workflowId, CancellationToken cancellationToken)
    {
        var policies = workflowId.HasValue
            ? await _unitOfWork.ApprovalEscalationPolicies.FindAsync(p => p.WorkflowId == workflowId.Value, cancellationToken)
            : await _unitOfWork.ApprovalEscalationPolicies.GetAllAsync(cancellationToken);
        return Ok(policies.OrderBy(p => p.WorkflowId).ThenBy(p => p.StepOrder).Select(MapToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ApprovalEscalationPolicyDto>> Create([FromBody] CreateApprovalEscalationRequest request, CancellationToken cancellationToken)
    {
        var policy = new ApprovalEscalationPolicy(
            request.WorkflowId,
            request.StepOrder,
            request.SlaMinutes,
            request.EscalateToRoleId,
            request.EscalateToUserId,
            request.NotifyOnEscalation);

        await _unitOfWork.ApprovalEscalationPolicies.AddAsync(policy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = policy.Id }, MapToDto(policy));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApprovalEscalationPolicyDto>> Update(Guid id, [FromBody] UpdateApprovalEscalationRequest request, CancellationToken cancellationToken)
    {
        var policy = await _unitOfWork.ApprovalEscalationPolicies.GetByIdAsync(id, cancellationToken);
        if (policy == null)
            return NotFound();
        policy.Update(request.SlaMinutes, request.EscalateToRoleId, request.EscalateToUserId, request.NotifyOnEscalation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(policy));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _unitOfWork.ApprovalEscalationPolicies.GetByIdAsync(id, cancellationToken);
        if (policy == null)
            return NotFound();
        policy.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ApprovalEscalationPolicyDto MapToDto(ApprovalEscalationPolicy p) => new(
        p.Id, p.WorkflowId, p.StepOrder, p.SlaMinutes, p.EscalateToRoleId, p.EscalateToUserId, p.NotifyOnEscalation, p.IsActive);
}

public record ApprovalDelegationDto(
    Guid Id, Guid DelegatorUserId, Guid DelegateUserId, string? Module, string? DocumentType,
    Guid? WorkflowId, DateTimeOffset StartsOn, DateTimeOffset EndsOn, bool IsActive);

public record CreateApprovalDelegationRequest(
    Guid DelegatorUserId, Guid DelegateUserId, DateTimeOffset StartsOn, DateTimeOffset EndsOn,
    string? Module, string? DocumentType, Guid? WorkflowId);

public record ApprovalEscalationPolicyDto(
    Guid Id, Guid WorkflowId, int StepOrder, int SlaMinutes, Guid? EscalateToRoleId,
    Guid? EscalateToUserId, bool NotifyOnEscalation, bool IsActive);

public record CreateApprovalEscalationRequest(
    Guid WorkflowId, int StepOrder, int SlaMinutes, Guid? EscalateToRoleId, Guid? EscalateToUserId, bool NotifyOnEscalation = true);

public record UpdateApprovalEscalationRequest(
    int SlaMinutes, Guid? EscalateToRoleId, Guid? EscalateToUserId, bool NotifyOnEscalation);
