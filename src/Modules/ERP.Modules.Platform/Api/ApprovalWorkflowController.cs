// <copyright file="ApprovalWorkflowController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable S6960

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/approval-workflows")]
public class ApprovalWorkflowController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly PlatformDbContext _context;

    public ApprovalWorkflowController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, IApprovalWorkflowService approvalWorkflowService, PlatformDbContext context)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _approvalWorkflowService = approvalWorkflowService ?? throw new ArgumentNullException(nameof(approvalWorkflowService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalWorkflowDto>>> GetAll([FromQuery] string? module, CancellationToken cancellationToken)
    {
        var query = _context.ApprovalWorkflows
            .AsNoTracking()
            .ApplyCompanyScope(HttpContext, w => w.CompanyId ?? Guid.Empty);

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(w => w.Module == module);
        }

        var workflows = await query.ToListAsync(cancellationToken);

        var dtos = new List<ApprovalWorkflowDto>();
        foreach (var workflow in workflows)
        {
            var steps = await _unitOfWork.ApprovalSteps.FindAsync(s => s.WorkflowId == workflow.Id, cancellationToken);
            var dto = MapToDto(workflow) with
            {
                Steps = steps.OrderBy(s => s.StepOrder).Select(MapStepToDto).ToList()
            };
            dtos.Add(dto);
        }

        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
            return NotFound();

        var steps = await _unitOfWork.ApprovalSteps.FindAsync(s => s.WorkflowId == id, cancellationToken);

        var dto = MapToDto(workflow) with
        {
            Steps = steps.OrderBy(s => s.StepOrder).Select(MapStepToDto).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApprovalWorkflowDto>> Create([FromBody] CreateApprovalWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = new ApprovalWorkflow(
            request.Module,
            request.DocumentType,
            request.Description,
            request.CompanyId,
            request.ThresholdAmount);

        foreach (var step in request.Steps ?? [])
        {
            workflow.AddStep(
                step.StepOrder,
                step.Description,
                step.ApproverRoleId,
                step.SpecificApproverUserId,
                step.RequiredApprovals,
                step.MinAmount,
                step.MaxAmount);
        }

        await _unitOfWork.ApprovalWorkflows.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(ApprovalWorkflow),
            workflow.Id,
            "system",
            newValues: new { request.Module, request.DocumentType, request.Description },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, MapToDto(workflow));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> Update(Guid id, [FromBody] UpdateApprovalWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
            return NotFound();

        var oldValues = new { workflow.Description, workflow.ThresholdAmount };

        workflow.Update(request.Description, request.ThresholdAmount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Updated",
            nameof(ApprovalWorkflow),
            workflow.Id,
            "system",
            oldValues: oldValues,
            newValues: new { request.Description, request.ThresholdAmount },
            cancellationToken: cancellationToken);

        return Ok(MapToDto(workflow));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
            return NotFound();

        workflow.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Deleted",
            nameof(ApprovalWorkflow),
            workflow.Id,
            "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
            return NotFound();

        workflow.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
            return NotFound();

        workflow.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("{workflowId:guid}/steps")]
    public async Task<ActionResult<ApprovalStepDto>> AddStep(Guid workflowId, [FromBody] CreateApprovalStepRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(workflowId, cancellationToken);
        if (workflow == null)
            return NotFound();

        workflow.AddStep(
            request.StepOrder,
            request.Description,
            request.ApproverRoleId,
            request.SpecificApproverUserId,
            request.RequiredApprovals,
            request.MinAmount,
            request.MaxAmount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var steps = await _unitOfWork.ApprovalSteps.FindAsync(s => s.WorkflowId == workflowId, cancellationToken);
        var createdStep = steps.OrderByDescending(s => s.StepOrder).First();

        return Ok(MapStepToDto(createdStep));
    }

    [HttpDelete("{workflowId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> RemoveStep(Guid workflowId, Guid stepId, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.ApprovalWorkflows.GetByIdAsync(workflowId, cancellationToken);
        if (workflow == null)
            return NotFound();

        workflow.RemoveStep(stepId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<ApprovalRequestDto>>> GetPendingRequests(
        [FromQuery] string? module,
        CancellationToken cancellationToken)
    {
        var requests = await _approvalWorkflowService.GetPendingRequestsAsync(module, cancellationToken: cancellationToken);
        return Ok(requests.Select(MapRequestToDto).ToList());
    }

    [HttpGet("requests/{requestId:guid}")]
    public async Task<ActionResult<ApprovalRequestDto>> GetRequestById(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _approvalWorkflowService.GetRequestByIdAsync(requestId, cancellationToken);
        if (request == null)
            return NotFound();

        return Ok(MapRequestToDto(request));
    }

    [HttpPost("requests")]
    public async Task<ActionResult<ApprovalRequestDto>> SubmitForApproval([FromBody] SubmitApprovalRequest request, CancellationToken cancellationToken)
    {
        var approvalRequest = await _approvalWorkflowService.SubmitForApprovalAsync(
            request.WorkflowId,
            request.Module,
            request.DocumentType,
            request.DocumentId,
            request.DocumentNumber,
            request.Amount,
            "system",
            request.Notes,
            cancellationToken);

        return CreatedAtAction(nameof(GetRequestById), new { requestId = approvalRequest.Id }, MapRequestToDto(approvalRequest));
    }

    [HttpPost("requests/{requestId:guid}/actions")]
    public async Task<ActionResult<ApprovalActionDto>> ProcessAction(Guid requestId, [FromBody] ProcessApprovalActionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApprovalDecision>(request.Decision, true, out var decision))
        {
            return BadRequest($"Invalid decision value. Must be 'Approved' or 'Rejected'.");
        }

        var action = await _approvalWorkflowService.ProcessActionAsync(
            requestId,
            request.ActionedBy,
            decision,
            request.StepId,
            request.Comments,
            cancellationToken);

        return Ok(MapActionToDto(action));
    }

    private static ApprovalWorkflowDto MapToDto(ApprovalWorkflow workflow)
    {
        return new ApprovalWorkflowDto(
            workflow.Id,
            workflow.Module,
            workflow.DocumentType,
            workflow.Description,
            workflow.IsActive,
            workflow.ThresholdAmount,
            [],
            workflow.CreatedOn,
            workflow.ModifiedOn);
    }

    private static ApprovalStepDto MapStepToDto(ApprovalStep step)
    {
        return new ApprovalStepDto(
            step.Id,
            step.WorkflowId,
            step.StepOrder,
            step.Description,
            step.ApproverRoleId,
            step.SpecificApproverUserId,
            step.RequiredApprovals,
            step.MinAmount,
            step.MaxAmount);
    }

    private static ApprovalRequestDto MapRequestToDto(ApprovalRequest request)
    {
        return new ApprovalRequestDto(
            request.Id,
            request.WorkflowId,
            request.Module,
            request.DocumentType,
            request.DocumentId,
            request.DocumentNumber,
            request.Amount,
            request.RequestedBy,
            request.Status.ToString(),
            request.CurrentStep,
            request.Notes,
            request.Actions.Select(MapActionToDto).ToList(),
            request.CreatedOn);
    }

    private static ApprovalActionDto MapActionToDto(ApprovalAction action)
    {
        return new ApprovalActionDto(
            action.Id,
            action.RequestId,
            action.StepId,
            action.ActionedBy,
            action.Decision.ToString(),
            action.Comments,
            action.ActionedOn);
    }
}
