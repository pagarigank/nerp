// <copyright file="ApprovalWorkflowService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Infrastructure;

public interface IApprovalWorkflowService
{
    Task<ApprovalWorkflow?> GetWorkflowAsync(string module, string documentType, decimal amount, Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<ApprovalRequest> SubmitForApprovalAsync(Guid workflowId, string module, string documentType, Guid documentId, string documentNumber, decimal amount, string requestedBy, string? notes = null, CancellationToken cancellationToken = default);
    Task<ApprovalAction> ProcessActionAsync(Guid requestId, string actionedBy, ApprovalDecision decision, Guid? stepId = null, string? comments = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalRequest>> GetPendingRequestsAsync(string? module = null, string? actionedBy = null, CancellationToken cancellationToken = default);
    Task<ApprovalRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> CanUserApproveAsync(Guid requestId, string userId, Guid? stepId = null, CancellationToken cancellationToken = default);
}

public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly PlatformDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ApprovalWorkflowService(PlatformDbContext context, IAuditLogService auditLogService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<ApprovalWorkflow?> GetWorkflowAsync(
        string module,
        string documentType,
        decimal amount,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalWorkflows
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .Where(w => w.Module == module && w.DocumentType == documentType && w.IsActive);

        if (companyId.HasValue)
        {
            query = query.Where(w => w.CompanyId == null || w.CompanyId == companyId.Value);
        }
        else
        {
            query = query.Where(w => w.CompanyId == null);
        }

        var workflows = await query.ToListAsync(cancellationToken);

        return workflows
            .Where(w => !w.ThresholdAmount.HasValue || amount >= w.ThresholdAmount.Value)
            .OrderByDescending(w => w.ThresholdAmount.HasValue)
            .FirstOrDefault();
    }

    public async Task<ApprovalRequest> SubmitForApprovalAsync(
        Guid workflowId,
        string module,
        string documentType,
        Guid documentId,
        string documentNumber,
        decimal amount,
        string requestedBy,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ApprovalRequest(
            workflowId,
            module,
            documentType,
            documentId,
            documentNumber,
            amount,
            requestedBy,
            currentStep: 1,
            notes);

        _context.ApprovalRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ApprovalRequestSubmitted",
            nameof(ApprovalRequest),
            request.Id,
            requestedBy,
            newValues: new { module, documentType, documentNumber, amount },
            cancellationToken: cancellationToken);

        return request;
    }

    public async Task<ApprovalAction> ProcessActionAsync(
        Guid requestId,
        string actionedBy,
        ApprovalDecision decision,
        Guid? stepId = null,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.ApprovalRequests
            .Include(r => r.Actions)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Approval request {requestId} not found.");

        if (request.Status != ApprovalStatus.Pending && request.Status != ApprovalStatus.PartiallyApproved)
        {
            throw new InvalidOperationException($"Approval request {requestId} is already {request.Status}.");
        }

        request.AddAction(actionedBy, decision, stepId, comments);

        if (decision == ApprovalDecision.Approved)
        {
            var workflow = await _context.ApprovalWorkflows
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, cancellationToken);

            if (workflow != null)
            {
                var currentStepActions = request.Actions
                    .Where(a => a.StepId == stepId && a.Decision == ApprovalDecision.Approved)
                    .ToList();

                var step = workflow.Steps.FirstOrDefault(s => s.Id == stepId);
                var requiredApprovals = step?.RequiredApprovals ?? 1;

                if (currentStepActions.Count >= requiredApprovals)
                {
                    var nextStep = workflow.Steps
                        .Where(s => s.StepOrder > request.CurrentStep)
                        .OrderBy(s => s.StepOrder)
                        .FirstOrDefault();

                    if (nextStep != null)
                    {
                        request.AdvanceStep();
                        request.MarkPartiallyApproved();
                    }
                    else
                    {
                        request.Approve();
                    }
                }
            }
            else
            {
                request.Approve();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            $"ApprovalRequest{(decision == ApprovalDecision.Approved ? "Approved" : "Rejected")}",
            nameof(ApprovalRequest),
            request.Id,
            actionedBy,
            newValues: new { decision, comments },
            cancellationToken: cancellationToken);

        return request.Actions[^1];
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingRequestsAsync(
        string? module = null,
        string? actionedBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalRequests
            .Include(r => r.Actions)
            .Where(r => r.Status == ApprovalStatus.Pending || r.Status == ApprovalStatus.PartiallyApproved);

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(r => r.Module == module);
        }

        if (!string.IsNullOrEmpty(actionedBy))
        {
            query = query.Where(r => !r.Actions.Any(a => a.ActionedBy == actionedBy));
        }

        return await query.OrderByDescending(r => r.CreatedOn).ToListAsync(cancellationToken);
    }

    public async Task<ApprovalRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalRequests
            .Include(r => r.Actions)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<bool> CanUserApproveAsync(Guid requestId, string userId, Guid? stepId = null, CancellationToken cancellationToken = default)
    {
        var request = await _context.ApprovalRequests
            .Include(r => r.Actions)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request == null || request.RequestedBy == userId)
            return false;

        if (request.Actions.Any(a => a.ActionedBy == userId))
            return false;

        var effectiveStepId = stepId
            ?? ResolveCurrentStepId(request);

        return await IsAuthorizedApproverAsync(request, userId, effectiveStepId, cancellationToken);
    }

    private Guid? ResolveCurrentStepId(ApprovalRequest request)
    {
        // Use the workflow's current step (request.CurrentStep) to find its step id.
        var workflow = _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .FirstOrDefault(w => w.Id == request.WorkflowId);
        var step = workflow?.Steps.FirstOrDefault(s => s.StepOrder == request.CurrentStep);
        return step?.Id;
    }

    /// <summary>
    /// True when <paramref name="userId"/> may act on <paramref name="stepId"/> for this
    /// request, either as the step's configured approver (role member or specific user),
    /// via an active delegation from that approver, or as the step's escalation target.
    /// </summary>
    private async Task<bool> IsAuthorizedApproverAsync(ApprovalRequest request, string userId, Guid? stepId, CancellationToken cancellationToken = default)
    {
        if (stepId is null)
            return false;

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, cancellationToken);

        var step = workflow?.Steps.FirstOrDefault(s => s.Id == stepId.Value);
        if (step is null)
            return false;

        var userIsStepApprover = await UserMatchesApproverAsync(userId, step, cancellationToken);
        if (userIsStepApprover)
            return true;

        // Delegation: a delegate standing in for the step approver.
        if (await DelegationCoversAsync(userId, step, request.Module, request.DocumentType, workflow!.Id, cancellationToken))
            return true;

        // Escalation: the escalation policy target for this step.
        var policy = await _context.ApprovalEscalationPolicies
            .FirstOrDefaultAsync(p => p.WorkflowId == workflow!.Id && p.StepOrder == step.StepOrder && p.IsActive, cancellationToken);
        if (policy is not null)
        {
            if (policy.EscalateToUserId.HasValue && policy.EscalateToUserId.Value.ToString() == userId)
                return true;
            if (policy.EscalateToRoleId.HasValue && await UserHasRoleAsync(userId, policy.EscalateToRoleId.Value, cancellationToken))
                return true;
        }

        return false;
    }

    private async Task<bool> UserMatchesApproverAsync(string userId, ApprovalStep step, CancellationToken cancellationToken = default)
    {
        if (step.SpecificApproverUserId.HasValue && step.SpecificApproverUserId.Value.ToString() == userId)
            return true;
        if (step.ApproverRoleId.HasValue)
            return await UserHasRoleAsync(userId, step.ApproverRoleId.Value, cancellationToken);
        return false;
    }

    private async Task<bool> UserHasRoleAsync(string userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles.AnyAsync(ur => ur.UserId.ToString() == userId && ur.RoleId == roleId, cancellationToken);
    }

    private async Task<bool> DelegationCoversAsync(
        string delegateUserId, ApprovalStep step, string? module, string? documentType, Guid workflowId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var delegations = await _context.ApprovalDelegations
            .Where(d => d.DelegateUserId.ToString() == delegateUserId && d.IsActive && d.StartsOn <= now && d.EndsOn >= now)
            .ToListAsync(cancellationToken);

        foreach (var del in delegations)
        {
            if (!del.Covers(module, documentType, workflowId, now))
                continue;

            // The delegate may act if the delegator would have been an authorized approver.
            if (await UserMatchesApproverAsync(del.DelegatorUserId.ToString(), step, cancellationToken))
                return true;
        }

        return false;
    }
}
