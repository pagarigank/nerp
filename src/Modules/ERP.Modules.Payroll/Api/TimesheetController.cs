// <copyright file="TimesheetController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll/timesheets")]
public class TimesheetController : ControllerBase
{
    private readonly PayrollDbContext _context;
    private readonly ProjDbContext _projContext;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ERP.Core.Common.IProjectCostValidation _projectCostValidation;
    private readonly IApprovalWorkflowService _approvalWorkflow;

    public TimesheetController(
        PayrollDbContext context,
        ProjDbContext projContext,
        IDomainEventDispatcher eventDispatcher,
        ERP.Core.Common.IProjectCostValidation projectCostValidation,
        IApprovalWorkflowService approvalWorkflow)
    {
        _context = context;
        _projContext = projContext;
        _eventDispatcher = eventDispatcher;
        _projectCostValidation = projectCostValidation;
        _approvalWorkflow = approvalWorkflow;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateTimesheetRequest request, CancellationToken cancellationToken)
    {
        var timesheet = new Timesheet(request.CompanyId, request.EmployeeId, request.WeekEnding);
        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(timesheet.Id));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddLine(
        Guid id, [FromBody] AddTimesheetLineRequest request, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets.FindAsync(new object[] { id }, cancellationToken);
        if (timesheet is null)
            return NotFound(ApiResponse.Failure(new[] { "Timesheet not found." }, 404));

        // Project/task validation (Phase 10 wiring): if the line is charged to a project,
        // the project must exist and be active, and the task (if supplied) must belong to it.
        if (request.ProjectId.HasValue)
        {
            var project = await _projContext.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId.Value, cancellationToken);
            if (project is null)
                return BadRequest(ApiResponse.Failure(new[] { "Project does not exist." }));
            if (project.Status != ERP.Modules.ProjectAccounting.Domain.Entities.ProjectStatus.Active)
                return BadRequest(ApiResponse.Failure(new[] { "Project is not active." }));
            if (request.TaskId.HasValue && !project.Tasks.Any(t => t.Id == request.TaskId.Value))
                return BadRequest(ApiResponse.Failure(new[] { "Task is not valid for this project." }));
        }

        var line = timesheet.AddLine(
            request.ProjectId,
            request.TaskId,
            request.PayCodeId,
            request.WorkDate,
            request.Hours,
            request.Rate,
            request.TradeClassification,
            request.IsBillable,
            request.IsOvertime);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse>> Submit(
        Guid id, [FromBody] SubmitTimesheetRequest request, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets.FindAsync(new object[] { id }, cancellationToken);
        if (timesheet is null)
            return NotFound(ApiResponse.Failure(new[] { "Timesheet not found." }, 404));
        try
        {
            timesheet.Submit(request.SupervisorId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }

        // Phase 11 item #1103: route timesheet approval through the Phase 1 Approval Workflow
        // engine (threshold routing) rather than a bespoke flow. If a workflow is configured
        // for Payroll/Timesheet we raise an ApprovalRequest and remember its id so Approve()
        // processes it through the engine.
        var workflow = await _approvalWorkflow.GetWorkflowAsync("Payroll", "Timesheet", timesheet.TotalHours, timesheet.CompanyId, cancellationToken);
        if (workflow is not null)
        {
            var approvalRequest = await _approvalWorkflow.SubmitForApprovalAsync(
                workflow.Id,
                "Payroll",
                "Timesheet",
                timesheet.Id,
                timesheet.Id.ToString(),
                timesheet.TotalHours,
                timesheet.EmployeeId.ToString(),
                null,
                cancellationToken);
            timesheet.SetApprovalRequestId(approvalRequest.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(
        Guid id, [FromBody] ApproveTimesheetRequest request, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (timesheet is null)
            return NotFound(ApiResponse.Failure(new[] { "Timesheet not found." }, 404));

        try
        {
            timesheet.Approve(request.ApprovedById);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }

        // Phase 11 item #1103: if the timesheet was routed through the Phase 1 Approval
        // Workflow engine on submit, process the approval action there as well so the
        // engine's audit trail and routing stay authoritative.
        if (timesheet.ApprovalRequestId.HasValue)
        {
            await _approvalWorkflow.ProcessActionAsync(
                timesheet.ApprovalRequestId.Value,
                request.ApprovedById.ToString(),
                ERP.Modules.Platform.Domain.Entities.ApprovalDecision.Approved,
                null,
                "Approved via timesheet approval.",
                cancellationToken);
        }

        // Phase 11 cross-module wiring (item #1099 / #1100): validate each project-charged
        // line against the open project/task + available budget via the shared
        // IProjectCostValidation contract owned by Project Accounting (no module cycle).
        foreach (var line in timesheet.Lines.Where(l => l.ProjectId.HasValue))
        {
            var validation = await _projectCostValidation.ValidateAsync(
                timesheet.CompanyId,
                line.ProjectId,
                line.TaskId,
                line.Amount,
                cancellationToken);
            if (!validation.IsValid)
                return BadRequest(ApiResponse.Failure(new[] { validation.Message ?? "Project cost validation failed." }));
        }

        await _context.SaveChangesAsync(cancellationToken);

        var laborLines = timesheet.Lines.Select(l => new LaborPostingLine(
            l.ProjectId, l.TaskId, l.PayCodeId, l.WorkDate, l.Hours, l.Rate, l.Amount,
            l.TradeClassification, l.IsBillable, l.IsOvertime)).ToList();

        // Architecture-named lifecycle signal (§4) for integration/audit/EDI subscribers.
        await _eventDispatcher.DispatchAsync(
            new TimesheetApprovedEvent(timesheet.Id, timesheet.CompanyId, timesheet.EmployeeId, timesheet.WeekEnding, laborLines),
            cancellationToken);

        // Detailed labor-posting payload consumed by Project Accounting job-costing + GL.
        await _eventDispatcher.DispatchAsync(
            new LaborPostedToProjectEvent(
                timesheet.Id,
                timesheet.CompanyId,
                timesheet.EmployeeId,
                timesheet.WeekEnding,
                laborLines),
            cancellationToken);

        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse>> Reject(
        Guid id, [FromBody] RejectTimesheetRequest request, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets.FindAsync(new object[] { id }, cancellationToken);
        if (timesheet is null)
            return NotFound(ApiResponse.Failure(new[] { "Timesheet not found." }, 404));
        try
        {
            timesheet.Reject(request.Reason);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TimesheetDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (timesheet is null)
            return NotFound(ApiResponse.Failure(new[] { "Timesheet not found." }, 404));

        return Ok(ApiResponse<TimesheetDto>.Success(new TimesheetDto
        {
            Id = timesheet.Id,
            CompanyId = timesheet.CompanyId,
            EmployeeId = timesheet.EmployeeId,
            WeekEnding = timesheet.WeekEnding,
            Status = timesheet.Status.ToString(),
            TotalHours = timesheet.TotalHours,
            TotalRegularHours = timesheet.TotalRegularHours,
            TotalOvertimeHours = timesheet.TotalOvertimeHours,
            RejectionReason = timesheet.RejectionReason,
            Lines = timesheet.Lines.Select(l => new TimesheetLineDto
            {
                Id = l.Id,
                ProjectId = l.ProjectId,
                TaskId = l.TaskId,
                PayCodeId = l.PayCodeId,
                WorkDate = l.WorkDate,
                Hours = l.Hours,
                Rate = l.Rate,
                Amount = l.Amount,
                TradeClassification = l.TradeClassification,
                IsBillable = l.IsBillable,
                IsOvertime = l.IsOvertime,
            }).ToList(),
        }));
    }
}

public class CreateTimesheetRequest
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime WeekEnding { get; set; }
}

public class AddTimesheetLineRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid PayCodeId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public string? TradeClassification { get; set; }
    public bool IsBillable { get; set; } = true;
    public bool IsOvertime { get; set; }
}

public class SubmitTimesheetRequest
{
    public Guid SupervisorId { get; set; }
}

public class ApproveTimesheetRequest
{
    public Guid ApprovedById { get; set; }
}

public class RejectTimesheetRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class TimesheetDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime WeekEnding { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public string? RejectionReason { get; set; }
    public List<TimesheetLineDto> Lines { get; set; } = [];
}

public class TimesheetLineDto
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid PayCodeId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string? TradeClassification { get; set; }
    public bool IsBillable { get; set; }
    public bool IsOvertime { get; set; }
}
