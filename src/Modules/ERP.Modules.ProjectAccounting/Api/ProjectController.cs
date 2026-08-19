// <copyright file="ProjectController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/projects")]
public class ProjectController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;

    public ProjectController(ProjDbContext context, IProjUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProjectDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] ProjectStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.BudgetLines)
            .AsQueryable();

        if (companyId.HasValue)
            query = query.Where(p => p.CompanyId == companyId.Value);
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var projects = await query.OrderByDescending(p => p.CreatedOn).ToListAsync(cancellationToken);
        var dtos = projects.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ProjectDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.BudgetLines)
            .Include(p => p.ChangeOrders)
            .Include(p => p.ContractLines)
            .Include(p => p.BillingSchedules)
            .Include(p => p.AllocationRules)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
            return NotFound(ApiResponse<ProjectDto>.Failure(new[] { "Project not found." }, 404));

        return Ok(ApiResponse<ProjectDto>.Success(MapToDto(project)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var projectType = Enum.TryParse<ProjectType>(request.ProjectType, true, out var pt) ? pt : ProjectType.TimeAndMaterials;

        var project = new Project(
            request.CompanyId,
            request.ProjectCode,
            request.Name,
            projectType,
            request.CustomerId,
            request.ProjectManager,
            request.Description,
            request.ContractValue,
            request.PlannedStartDate,
            request.PlannedEndDate);

        if (request.RetainagePercentage.HasValue)
            project.SetRetainage(request.RetainagePercentage.Value);

        _context.Projects.Add(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(project.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var updateType = request.ProjectType is not null && Enum.TryParse<ProjectType>(request.ProjectType, true, out var ut) ? ut : (ProjectType?)null;

        project.Update(
            request.Name,
            request.Description,
            updateType,
            request.CustomerId,
            request.ProjectManager,
            request.ContractValue,
            request.RetainagePercentage,
            request.PlannedStartDate,
            request.PlannedEndDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (!Enum.TryParse<ProjectStatus>(request.Status, true, out var status))
            return BadRequest(ApiResponse.Failure(new[] { "Invalid status." }));

        project.UpdateStatus(status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Contingency / management reserve (761) ---
    [HttpPut("{id:guid}/contingency")]
    public async Task<ActionResult<ApiResponse>> SetContingency(Guid id, [FromBody] ContingencyRequest request, CancellationToken ct)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, ct);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));
        project.SetContingency(request.ContingencyAmount);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/contingency/release")]
    public async Task<ActionResult<ApiResponse<Guid>>> ReleaseContingency(Guid id, [FromBody] ReleaseContingencyRequest request, CancellationToken ct)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, ct);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));
        var co = project.ReleaseContingency(request.Amount, request.Reason);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(co.Id));
    }

    // --- Billing hold (783) ---
    [HttpPut("{id:guid}/billing-hold")]
    public async Task<ActionResult<ApiResponse>> SetBillingHold(Guid id, [FromBody] BillingHoldRequest request, CancellationToken ct)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, ct);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));
        project.SetBillingHold(request.Hold, request.Reason);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    // --- Multi-currency (742) ---
    [HttpPut("{id:guid}/currency")]
    public async Task<ActionResult<ApiResponse>> SetCurrency(Guid id, [FromBody] CurrencyRequest request, CancellationToken ct)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, ct);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));
        project.SetCurrency(request.CurrencyCode, request.ExchangeRate);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    // --- Tasks ---
    [HttpGet("{id:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetTasks(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tasks = await _context.ProjectTasks
            .Where(t => t.ProjectId == id)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        var dtos = tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            ProjectId = t.ProjectId,
            TaskCode = t.TaskCode,
            Description = t.Description,
            ParentTaskId = t.ParentTaskId,
            BudgetedHours = t.BudgetedHours,
            BudgetedCost = t.BudgetedCost,
            ActualHours = t.ActualHours,
            ActualCost = t.ActualCost,
            PercentComplete = t.PercentComplete,
            SortOrder = t.SortOrder,
        }).ToList();

        return Ok(ApiResponse<List<TaskDto>>.Success(dtos));
    }

    [HttpPost("{id:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddTask(
        Guid id,
        [FromBody] AddTaskRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var task = project.AddTask(
            request.TaskCode,
            request.Description,
            request.ParentTaskId,
            request.BudgetedHours,
            request.BudgetedCost);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(task.Id));
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateTask(
        Guid id,
        Guid taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == id, cancellationToken);

        if (task is null)
            return NotFound(ApiResponse.Failure(new[] { "Task not found." }, 404));

        task.Update(request.Description, request.ParentTaskId, request.BudgetedHours, request.BudgetedCost, request.SortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{id:guid}/tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(
        Guid id,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        project.RemoveTask(taskId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Mapping ---
    private static ProjectDto MapToDto(Project p) => new ()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        ProjectCode = p.ProjectCode,
        Name = p.Name,
        Description = p.Description,
        ProjectType = p.ProjectType.ToString(),
        Status = p.Status.ToString(),
        CustomerId = p.CustomerId,
        ProjectManager = p.ProjectManager,
        ContractValue = p.ContractValue,
        OriginalBudget = p.OriginalBudget,
        RevisedBudget = p.RevisedBudget,
        CostsToDate = p.CostsToDate,
        RevenueToDate = p.RevenueToDate,
        PercentComplete = p.PercentComplete,
        RetainagePercentage = p.RetainagePercentage,
        RetainageHeld = p.RetainageHeld,
        ProfitMargin = p.ProfitMargin,
        PlannedStartDate = p.PlannedStartDate,
        PlannedEndDate = p.PlannedEndDate,
        ActualStartDate = p.ActualStartDate,
        ActualEndDate = p.ActualEndDate,
        ContingencyAmount = p.ContingencyAmount,
        ReleasedContingency = p.ReleasedContingency,
        BillingHold = p.BillingHold,
        CurrencyCode = p.CurrencyCode,
        ExchangeRate = p.ExchangeRate,
        TaskCount = p.Tasks.Count,
        BudgetLineCount = p.BudgetLines.Count,
    };
}

// --- DTOs ---
#pragma warning disable S6960

public class ProjectDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProjectType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? ProjectManager { get; set; }
    public decimal? ContractValue { get; set; }
    public decimal OriginalBudget { get; set; }
    public decimal RevisedBudget { get; set; }
    public decimal CostsToDate { get; set; }
    public decimal RevenueToDate { get; set; }
    public decimal PercentComplete { get; set; }
    public decimal RetainagePercentage { get; set; }
    public decimal RetainageHeld { get; set; }
    public decimal? ProfitMargin { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal ContingencyAmount { get; set; }
    public decimal ReleasedContingency { get; set; }
    public bool BillingHold { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public int TaskCount { get; set; }
    public int BudgetLineCount { get; set; }
}

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentTaskId { get; set; }
    public decimal BudgetedHours { get; set; }
    public decimal BudgetedCost { get; set; }
    public decimal ActualHours { get; set; }
    public decimal ActualCost { get; set; }
    public decimal PercentComplete { get; set; }
    public int SortOrder { get; set; }
}

public class CreateProjectRequest
{
    public Guid CompanyId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProjectType { get; set; } = "TimeAndMaterials";
    public Guid? CustomerId { get; set; }
    public string? ProjectManager { get; set; }
    public decimal? ContractValue { get; set; }
    public decimal? RetainagePercentage { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ProjectType { get; set; }
    public Guid? CustomerId { get; set; }
    public string? ProjectManager { get; set; }
    public decimal? ContractValue { get; set; }
    public decimal? RetainagePercentage { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AddTaskRequest
{
    public string TaskCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentTaskId { get; set; }
    public decimal? BudgetedHours { get; set; }
    public decimal? BudgetedCost { get; set; }
}

public class UpdateTaskRequest
{
    public string? Description { get; set; }
    public Guid? ParentTaskId { get; set; }
    public decimal? BudgetedHours { get; set; }
    public decimal? BudgetedCost { get; set; }
    public int? SortOrder { get; set; }
}

public class ContingencyRequest
{
    public decimal ContingencyAmount { get; set; }
}

public class ReleaseContingencyRequest
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class BillingHoldRequest
{
    public bool Hold { get; set; }
    public string? Reason { get; set; }
}

public class CurrencyRequest
{
    public string? CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
}
