// <copyright file="ApprovalWorkflowDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record ApprovalWorkflowDto(
    Guid Id,
    string Module,
    string DocumentType,
    string Description,
    bool IsActive,
    decimal? ThresholdAmount,
    IReadOnlyList<ApprovalStepDto> Steps,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record ApprovalStepDto(
    Guid Id,
    Guid WorkflowId,
    int StepOrder,
    string Description,
    Guid? ApproverRoleId,
    Guid? SpecificApproverUserId,
    int RequiredApprovals,
    decimal? MinAmount,
    decimal? MaxAmount);

public record ApprovalRequestDto(
    Guid Id,
    Guid WorkflowId,
    string Module,
    string DocumentType,
    Guid DocumentId,
    string DocumentNumber,
    decimal Amount,
    string RequestedBy,
    string Status,
    int CurrentStep,
    string? Notes,
    IReadOnlyList<ApprovalActionDto> Actions,
    DateTimeOffset CreatedOn);

public record ApprovalActionDto(
    Guid Id,
    Guid RequestId,
    Guid? StepId,
    string ActionedBy,
    string Decision,
    string? Comments,
    DateTimeOffset ActionedOn);

public record CreateApprovalWorkflowRequest(
    string Module,
    string DocumentType,
    string Description,
    Guid? CompanyId,
    decimal? ThresholdAmount,
    IReadOnlyList<CreateApprovalStepRequest> Steps);

public record CreateApprovalStepRequest(
    int StepOrder,
    string Description,
    Guid? ApproverRoleId,
    Guid? SpecificApproverUserId,
    int RequiredApprovals,
    decimal? MinAmount,
    decimal? MaxAmount);

public record UpdateApprovalWorkflowRequest(
    string Description,
    decimal? ThresholdAmount);

public record SubmitApprovalRequest(
    Guid WorkflowId,
    string Module,
    string DocumentType,
    Guid DocumentId,
    string DocumentNumber,
    decimal Amount,
    string? Notes);

public record ProcessApprovalActionRequest(
    string ActionedBy,
    string Decision,
    Guid? StepId,
    string? Comments);
