// <copyright file="ChangeOrderController.cs" company="ERP Project">
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
[Route("api/v1/projects/{projectId:guid}/change-orders")]
public class ChangeOrderController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;

    public ChangeOrderController(ProjDbContext context, IProjUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ChangeOrderDto>>>> GetAll(
        Guid projectId,
        [FromQuery] ChangeOrderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.ChangeOrders.Where(c => c.ProjectId == projectId);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var orders = await query.OrderByDescending(c => c.CreatedOn).ToListAsync(cancellationToken);
        var dtos = orders.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ChangeOrderDto>>.Success(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        Guid projectId,
        [FromBody] CreateChangeOrderRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (!Enum.TryParse<CostCategory>(request.Category, true, out var category))
            return BadRequest(ApiResponse.Failure(new[] { "Invalid category." }));

        var co = project.AddChangeOrder(request.Description, request.Amount, category, request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(co.Id));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse>> Submit(
        Guid projectId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var co = await _context.ChangeOrders
            .FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, cancellationToken);

        if (co is null)
            return NotFound(ApiResponse.Failure(new[] { "Change order not found." }, 404));

        co.UpdateStatus(ChangeOrderStatus.Submitted);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(
        Guid projectId,
        Guid id,
        [FromBody] ApproveChangeOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var co = await _context.ChangeOrders
            .FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, cancellationToken);

        if (co is null)
            return NotFound(ApiResponse.Failure(new[] { "Change order not found." }, 404));

        if (co.Status != ChangeOrderStatus.Submitted)
            return BadRequest(ApiResponse.Failure(new[] { "Only submitted change orders can be approved." }));

        co.UpdateStatus(ChangeOrderStatus.Approved, request?.ApprovedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse>> Reject(
        Guid projectId,
        Guid id,
        [FromBody] RejectChangeOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var co = await _context.ChangeOrders
            .FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, cancellationToken);

        if (co is null)
            return NotFound(ApiResponse.Failure(new[] { "Change order not found." }, 404));

        co.UpdateStatus(ChangeOrderStatus.Rejected, rejectionReason: request?.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/execute")]
    public async Task<ActionResult<ApiResponse>> Execute(
        Guid projectId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.ChangeOrders)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var co = project.ChangeOrders.FirstOrDefault(c => c.Id == id);
        if (co is null)
            return NotFound(ApiResponse.Failure(new[] { "Change order not found." }, 404));

        if (co.Status != ChangeOrderStatus.Approved)
            return BadRequest(ApiResponse.Failure(new[] { "Only approved change orders can be executed." }));

        // Apply CO to budget
        project.AddBudgetLine(Guid.Empty, co.Category, co.Amount, null, $"CO: {co.Description}");
        project.RecalculateBudget();

        // Update contract value if applicable
        if (project.ContractValue.HasValue)
        {
            project.AdjustContractValue(co.Amount);
        }

        co.UpdateStatus(ChangeOrderStatus.Executed);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    private static ChangeOrderDto MapToDto(ChangeOrder c) => new ()
    {
        Id = c.Id,
        ProjectId = c.ProjectId,
        Description = c.Description,
        Amount = c.Amount,
        Category = c.Category.ToString(),
        Reason = c.Reason,
        Status = c.Status.ToString(),
        SubmittedDate = c.SubmittedDate,
        ApprovedDate = c.ApprovedDate,
        ApprovedBy = c.ApprovedBy,
    };
}

public class ChangeOrderDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
}

public class CreateChangeOrderRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = "Materials";
    public string? Reason { get; set; }
}

public class ApproveChangeOrderRequest
{
    public string? ApprovedBy { get; set; }
}

public class RejectChangeOrderRequest
{
    public string? Reason { get; set; }
}
