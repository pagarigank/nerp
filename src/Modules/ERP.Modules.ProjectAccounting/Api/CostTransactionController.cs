// <copyright file="CostTransactionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Events;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/projects/{projectId:guid}/costs")]
public class CostTransactionController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CostTransactionController(
        ProjDbContext context,
        IProjUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CostTransactionDto>>>> GetAll(
        Guid projectId,
        [FromQuery] CostCategory? category,
        [FromQuery] Guid? taskId,
        CancellationToken cancellationToken)
    {
        var query = _context.CostTransactions
            .Where(t => t.ProjectId == projectId)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);
        if (taskId.HasValue)
            query = query.Where(t => t.TaskId == taskId.Value);

        var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync(cancellationToken);
        var dtos = transactions.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<CostTransactionDto>>.Success(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> PostCost(
        Guid projectId,
        [FromBody] PostCostRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (!Enum.TryParse<CostCategory>(request.Category, true, out var category))
            return BadRequest(ApiResponse.Failure(new[] { "Invalid cost category." }));

        if (!Enum.TryParse<CostTransactionType>(request.TransactionType, true, out var txnType))
            return BadRequest(ApiResponse.Failure(new[] { "Invalid transaction type." }));

        var txn = new CostTransaction(
            project.CompanyId,
            projectId,
            request.TaskId,
            category,
            txnType,
            request.Amount,
            request.Hours,
            request.Description,
            request.SourceId,
            request.SourceReference,
            request.IsBillable,
            request.VendorId,
            request.EmployeeId);

        _context.CostTransactions.Add(txn);

        // Update project costs
        project.RecalculateCosts();
        project.UpdatePercentComplete(costToCostPercent: project.RevisedBudget > 0
            ? project.CostsToDate / project.RevisedBudget * 100
            : null);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Emit event for GL posting (dual-posting)
        await _eventDispatcher.DispatchAsync(
            new ProjectCostPostedEvent(
                txn.Id,
                projectId,
                request.TaskId,
                category.ToString(),
                request.Amount,
                txn.CompanyId),
            cancellationToken);

        return Ok(ApiResponse<Guid>.Success(txn.Id));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<CostSummaryDto>>> GetCostSummary(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.BudgetLines)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var costsByCategory = await _context.CostTransactions
            .Where(t => t.ProjectId == projectId && t.Status == TransactionStatus.Posted)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount), Hours = g.Sum(t => t.Hours) })
            .ToListAsync(cancellationToken);

        var budgetByCategory = project.BudgetLines
            .GroupBy(b => b.Category)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.BudgetAmount));

        var dto = new CostSummaryDto
        {
            ProjectId = projectId,
            TotalCosts = project.CostsToDate,
            TotalBudget = project.RevisedBudget,
            Remaining = project.RevisedBudget - project.CostsToDate,
            PercentComplete = project.PercentComplete,
            ByCategory = costsByCategory.ToDictionary(
                c => c.Category.ToString(),
                c => new CostCategorySummary
                {
                    Actual = c.Total,
                    Budget = budgetByCategory.GetValueOrDefault(c.Category),
                    Hours = c.Hours,
                    Variance = budgetByCategory.GetValueOrDefault(c.Category) - c.Total,
                }),
        };

        return Ok(ApiResponse<CostSummaryDto>.Success(dto));
    }

    private static CostTransactionDto MapToDto(CostTransaction t) => new ()
    {
        Id = t.Id,
        ProjectId = t.ProjectId,
        TaskId = t.TaskId,
        Category = t.Category.ToString(),
        TransactionType = t.TransactionType.ToString(),
        Amount = t.Amount,
        Hours = t.Hours,
        BurdenAmount = t.BurdenAmount,
        BillableAmount = t.BillableAmount,
        Description = t.Description,
        SourceReference = t.SourceReference,
        IsBillable = t.IsBillable,
        Status = t.Status.ToString(),
        TransactionDate = t.TransactionDate,
    };
}

public class CostTransactionDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid TaskId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Hours { get; set; }
    public decimal BurdenAmount { get; set; }
    public decimal BillableAmount { get; set; }
    public string? Description { get; set; }
    public string? SourceReference { get; set; }
    public bool IsBillable { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
}

#pragma warning disable CA1002, CA2227
public class CostSummaryDto
{
    public Guid ProjectId { get; set; }
    public decimal TotalCosts { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentComplete { get; set; }
    public Dictionary<string, CostCategorySummary> ByCategory { get; set; } = [];
}
#pragma warning restore CA2227

#pragma warning disable CA1002
public class CostCategorySummary
{
    public decimal Actual { get; set; }
    public decimal Budget { get; set; }
    public decimal Hours { get; set; }
    public decimal Variance { get; set; }
}
#pragma warning restore CA1002

public class PostCostRequest
{
    public Guid TaskId { get; set; }
    public string Category { get; set; } = "Labor";
    public string TransactionType { get; set; } = "ManualAdjustment";
    public decimal Amount { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceReference { get; set; }
    public bool IsBillable { get; set; } = true;
    public Guid? VendorId { get; set; }
    public Guid? EmployeeId { get; set; }
}
