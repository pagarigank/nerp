// <copyright file="CostAllocationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Domain.Services;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/project-accounting/cost-allocations")]
public class CostAllocationController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CostAllocationController(ProjDbContext context, IProjUnitOfWork unitOfWork, IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CostAllocationBatch>>>> GetAll([FromQuery] Guid companyId, CancellationToken ct)
        => Ok(ApiResponse<List<CostAllocationBatch>>.Success(await _context.CostAllocationBatches
            .ApplyCompanyScope(HttpContext, b => b.CompanyId, companyId)
            .Include(b => b.Lines)
            .ToListAsync(ct)));

    /// <summary>Create and post a shared-cost allocation batch. Each line posts a cost transaction to its project.</summary>
    /// <param name="r">The allocation batch request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created batch id.</returns>
    [HttpPost("post")]
    public async Task<ActionResult<ApiResponse<Guid>>> PostBatch([FromBody] PostAllocationRequest r, CancellationToken ct)
    {
        var batch = new CostAllocationBatch(r.CompanyId, r.Description, r.AllocationBase, r.PeriodStart, r.PeriodEnd);
        foreach (var l in r.Lines)
            batch.AddLine(l.ProjectId, l.Amount, Enum.Parse<CostCategory>(l.Category, true), l.Note);

        _context.CostAllocationBatches.Add(batch);
        await _unitOfWork.SaveChangesAsync(ct);

        var total = batch.Lines.Sum(l => l.Amount);
        batch.Post(total);

        // Post a cost transaction (and dual-post to GL) for every line.
        foreach (var line in batch.Lines)
        {
            var project = await _context.Projects.FindAsync(new object[] { line.ProjectId }, ct);
            if (project is null)
                continue;
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectId == line.ProjectId, ct);
            var txn = new CostTransaction(
                project.CompanyId,
                line.ProjectId,
                task?.Id ?? Guid.Empty,
                line.Category,
                CostTransactionType.ManualAdjustment,
                line.Amount,
                0,
                $"Allocation: {r.Description}",
                null,
                $"ALLOC-{batch.Id:N}");
            _context.CostTransactions.Add(txn);
            line.MarkPosted();
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Emit events for GL dual-posting.
        var posted = await _context.CostTransactions
            .Where(t => t.SourceReference == $"ALLOC-{batch.Id:N}").ToListAsync(ct);
        foreach (var t in posted)
        {
            await _eventDispatcher.DispatchAsync(
                new ProjectCostPostedEvent(t.Id, t.ProjectId, t.TaskId, t.Category.ToString(), t.Amount, t.CompanyId), ct);
        }

        return Ok(ApiResponse<Guid>.Success(batch.Id));
    }
}

public class PostAllocationRequest
{
    public Guid CompanyId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AllocationBase { get; set; } = "DirectLabor";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

#pragma warning disable CA1002, CA2227
    public List<AllocationLineRequest> Lines { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

public class AllocationLineRequest
{
    public Guid ProjectId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = "Overhead";
    public string? Note { get; set; }
}
