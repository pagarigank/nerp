// <copyright file="Asc606Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/projects/{projectId:guid}/asc606")]
public class Asc606Controller : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public Asc606Controller(ProjDbContext context, IProjUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    private static decimal ComputePercentSatisfied(ContractPerformanceObligation o)
    {
        if (o.TransactionPriceAllocated > 0)
            return Math.Min(100m, o.RecognizedRevenueToDate / o.TransactionPriceAllocated * 100);
        return o.Status == ObligationStatus.Satisfied ? 100 : 0;
    }

    private static Asc606ObligationDto MapToDto(ContractPerformanceObligation o) => new ()
    {
        Id = o.Id,
        CompanyId = o.CompanyId,
        ProjectId = o.ProjectId,
        Description = o.Description,
        TransactionPriceAllocated = o.TransactionPriceAllocated,
        StandaloneSellingPriceBasis = o.StandaloneSellingPriceBasis,
        RecognizedRevenueToDate = o.RecognizedRevenueToDate,
        PercentSatisfied = ComputePercentSatisfied(o),
        Status = o.Status.ToString(),
        SatisfiedOn = o.SatisfiedOn,
        CanEditOrDelete = o.IsUnrecognized,
    };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Asc606ObligationDto>>>> GetObligations(
        Guid projectId, CancellationToken cancellationToken)
    {
        var obligations = await _context.ContractPerformanceObligations
            .ApplyCompanyScope(HttpContext, o => o.CompanyId)
            .Where(o => o.ProjectId == projectId && o.DeletedOn == null)
            .OrderBy(o => o.CreatedOn)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<Asc606ObligationDto>>.Success(obligations.Select(MapToDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateObligation(
        Guid projectId,
        [FromBody] CreateAsc606ObligationRequest request,
        CancellationToken cancellationToken)
    {
        var project = await LoadProjectScoped(projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(ApiResponse.Failure(new[] { "Description is required." }));
        if (request.TransactionPriceAllocated < 0)
            return BadRequest(ApiResponse.Failure(new[] { "Allocated transaction price cannot be negative." }));

        var obligation = new ContractPerformanceObligation(
            project.CompanyId,
            project.Id,
            request.Description,
            request.TransactionPriceAllocated,
            request.StandaloneSellingPriceBasis);

        _context.ContractPerformanceObligations.Add(obligation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(obligation.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateObligation(
        Guid projectId,
        Guid id,
        [FromBody] UpdateAsc606ObligationRequest request,
        CancellationToken cancellationToken)
    {
        var obligation = await LoadObligationScoped(projectId, id, cancellationToken);
        if (obligation is null)
            return NotFound(ApiResponse.Failure(new[] { "Performance obligation not found." }, 404));

        try
        {
            obligation.Update(request.Description, request.TransactionPriceAllocated, request.StandaloneSellingPriceBasis);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Failure(new[] { ex.Message }, 409));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteObligation(
        Guid projectId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var obligation = await LoadObligationScoped(projectId, id, cancellationToken);
        if (obligation is null)
            return NotFound(ApiResponse.Failure(new[] { "Performance obligation not found." }, 404));

        if (!obligation.IsUnrecognized)
            return Conflict(ApiResponse.Failure(new[] { "Only obligations without recorded recognition can be deleted." }, 409));

        obligation.MarkDeleted(_currentUser.UserId ?? "system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("allocate")]
    public async Task<ActionResult<ApiResponse<AllocateResultDto>>> Allocate(
        Guid projectId,
        [FromBody] AllocateRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsScoped(projectId, cancellationToken))
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (request.TotalContractPrice < 0)
            return BadRequest(ApiResponse.Failure(new[] { "Total contract price cannot be negative." }));

        var obligations = await _context.ContractPerformanceObligations
            .Where(o => o.ProjectId == projectId && o.DeletedOn == null)
            .OrderBy(o => o.CreatedOn)
            .ToListAsync(cancellationToken);

        if (obligations.Count == 0)
            return BadRequest(ApiResponse.Failure(new[] { "Define at least one performance obligation before allocating." }));

        if (obligations.Any(o => o.RecognizedRevenueToDate > 0))
            return Conflict(ApiResponse.Failure(new[] { "Allocation cannot change after revenue has been recognized on any obligation." }, 409));

        var weightTotal = obligations.Sum(o => o.TransactionPriceAllocated);
        var rows = new List<AllocationRowDto>();
        var allocatedSoFar = 0m;

        for (var i = 0; i < obligations.Count; i++)
        {
            decimal allocation;
            if (weightTotal <= 0)
            {
                allocation = i == obligations.Count - 1
                    ? request.TotalContractPrice - allocatedSoFar
                    : decimal.Round(request.TotalContractPrice / obligations.Count, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                allocation = i == obligations.Count - 1
                    ? request.TotalContractPrice - allocatedSoFar
                    : decimal.Round(request.TotalContractPrice * obligations[i].TransactionPriceAllocated / weightTotal, 2, MidpointRounding.AwayFromZero);
            }

            var priorAllocation = obligations[i].TransactionPriceAllocated;
            obligations[i].SetAllocation(allocation);
            allocatedSoFar += allocation;

            rows.Add(new AllocationRowDto
            {
                ObligationId = obligations[i].Id,
                Description = obligations[i].Description,
                StandaloneSellingPriceBasis = obligations[i].StandaloneSellingPriceBasis,
                PriorAllocation = priorAllocation,
                NewAllocation = allocation,
                SharePercent = request.TotalContractPrice > 0 ? allocation / request.TotalContractPrice * 100 : 0,
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AllocateResultDto>.Success(new AllocateResultDto
        {
            ProjectId = projectId,
            TotalContractPrice = request.TotalContractPrice,
            Rows = rows,
        }));
    }

    private async Task<bool> ProjectExistsScoped(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId)
            .AnyAsync(p => p.Id == projectId, cancellationToken);
    }

    private async Task<Project?> LoadProjectScoped(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }

    private async Task<ContractPerformanceObligation?> LoadObligationScoped(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        return await _context.ContractPerformanceObligations
            .ApplyCompanyScope(HttpContext, o => o.CompanyId)
            .FirstOrDefaultAsync(o => o.Id == id && o.ProjectId == projectId && o.DeletedOn == null, cancellationToken);
    }

    [HttpGet("recognition-status")]
    public async Task<ActionResult<ApiResponse<RecognitionStatusDto>>> GetRecognitionStatus(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId)
            .Include(p => p.BudgetLines)
            .Include(p => p.CostTransactions)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var obligations = await _context.ContractPerformanceObligations
            .Where(o => o.ProjectId == projectId && o.DeletedOn == null)
            .OrderBy(o => o.CreatedOn)
            .ToListAsync(cancellationToken);

        var budgetAtCompletion = project.BudgetLines.Sum(b => b.BudgetAmount);
        if (budgetAtCompletion <= 0)
        {
            budgetAtCompletion = project.RevisedBudget;
        }

        if (budgetAtCompletion <= 0)
        {
            budgetAtCompletion = project.ContractValue ?? 0;
        }

        var estimateAtCompletion = project.EstimateAtCompletion > 0 ? project.EstimateAtCompletion : budgetAtCompletion;
        var costsPosted = project.CostTransactions
            .Where(t => t.Status == TransactionStatus.Posted)
            .Sum(t => t.Amount);
        var costToCostPercent = estimateAtCompletion > 0 ? costsPosted / estimateAtCompletion * 100 : 0;

        var dto = new RecognitionStatusDto
        {
            ProjectId = projectId,
            BudgetAtCompletion = budgetAtCompletion,
            EstimateAtCompletion = estimateAtCompletion,
            CostsPostedToDate = costsPosted,
            CostToCostPercent = costToCostPercent,
            Obligations = obligations.Select(MapToDto).ToList(),
        };

        return Ok(ApiResponse<RecognitionStatusDto>.Success(dto));
    }

    [HttpPost("{id:guid}/recognize")]
    public async Task<ActionResult<ApiResponse<RecognizeResultDto>>> Recognize(
        Guid projectId,
        Guid id,
        [FromBody] RecognizeRequest request,
        CancellationToken cancellationToken)
    {
        var obligation = await LoadObligationScoped(projectId, id, cancellationToken);
        if (obligation is null)
            return NotFound(ApiResponse.Failure(new[] { "Performance obligation not found." }, 404));

        try
        {
            obligation.RecordRecognition(request.Amount, request.AsOf ?? DateTimeOffset.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Failure(new[] { ex.Message }, 409));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<RecognizeResultDto>.Success(new RecognizeResultDto
        {
            ObligationId = obligation.Id,
            ProjectId = projectId,
            AmountRecognized = request.Amount,
            RecognizedRevenueToDate = obligation.RecognizedRevenueToDate,
            TransactionPriceAllocated = obligation.TransactionPriceAllocated,
            Status = obligation.Status.ToString(),
            SatisfiedOn = obligation.SatisfiedOn,
            GlPostingPending = true,
            Note = "Recognition persisted on the ASC 606 obligation ledger; GL revenue posting pending.",
        }));
    }

    [HttpGet("five-step-summary")]
    public async Task<ActionResult<ApiResponse<FiveStepSummaryDto>>> GetFiveStepSummary(
        Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId)
            .Include(p => p.ChangeOrders)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        var obligations = await _context.ContractPerformanceObligations
            .Where(o => o.ProjectId == projectId && o.DeletedOn == null)
            .OrderBy(o => o.CreatedOn)
            .ToListAsync(cancellationToken);

        var totalAllocated = obligations.Sum(o => o.TransactionPriceAllocated);
        var totalRecognized = obligations.Sum(o => o.RecognizedRevenueToDate);
        var pendingChangeOrders = project.ChangeOrders
            .Where(c => c.Status == ChangeOrderStatus.Draft || c.Status == ChangeOrderStatus.Submitted)
            .Sum(c => c.Amount);

        var constraintNote = pendingChangeOrders != 0
            ? $"Pending change orders of {pendingChangeOrders:F2} are unapproved variable consideration; include them in the transaction price only when a significant reversal is not probable (ASC 606-10-55-17/18)."
            : "No pending change orders; no additional variable consideration is constrained.";

        var dto = new FiveStepSummaryDto
        {
            Contract = new FiveStepContractDto
            {
                ProjectId = project.Id,
                ProjectCode = project.ProjectCode,
                Name = project.Name,
                CustomerId = project.CustomerId,
                ProjectType = project.ProjectType.ToString(),
                Status = project.Status.ToString(),
                ContractValue = project.ContractValue,
                PlannedStartDate = project.PlannedStartDate,
                PlannedEndDate = project.PlannedEndDate,
            },
            PendingChangeOrderAmount = pendingChangeOrders,
            VariableConsiderationConstraintNote = constraintNote,
            TotalContractPriceAllocated = totalAllocated,
            TotalRecognizedRevenue = totalRecognized,
            Obligations = obligations.Select(o =>
            {
                var dto1 = MapToDto(o);
                return new FiveStepObligationRowDto
                {
                    Id = dto1.Id,
                    Description = dto1.Description,
                    StandaloneSellingPriceBasis = dto1.StandaloneSellingPriceBasis,
                    TransactionPriceAllocated = dto1.TransactionPriceAllocated,
                    AllocationSharePercent = totalAllocated > 0 ? o.TransactionPriceAllocated / totalAllocated * 100 : 0,
                    RecognizedRevenueToDate = dto1.RecognizedRevenueToDate,
                    PercentSatisfied = dto1.PercentSatisfied,
                    Status = dto1.Status,
                    SatisfiedOn = dto1.SatisfiedOn,
                };
            }).ToList(),
        };

        return Ok(ApiResponse<FiveStepSummaryDto>.Success(dto));
    }
}

#pragma warning disable CA1002, CA2227
public class RecognitionStatusDto
{
    public Guid ProjectId { get; set; }
    public decimal BudgetAtCompletion { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal CostsPostedToDate { get; set; }
    public decimal CostToCostPercent { get; set; }
    public List<Asc606ObligationDto> Obligations { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class RecognizeRequest
{
    public decimal Amount { get; set; }
    public DateTimeOffset? AsOf { get; set; }
}

public class RecognizeResultDto
{
    public Guid ObligationId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal AmountRecognized { get; set; }
    public decimal RecognizedRevenueToDate { get; set; }
    public decimal TransactionPriceAllocated { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SatisfiedOn { get; set; }
    public bool GlPostingPending { get; set; }
    public string Note { get; set; } = string.Empty;
}

#pragma warning disable CA1002, CA2227
public class FiveStepSummaryDto
{
    public FiveStepContractDto Contract { get; set; } = new ();
    public decimal PendingChangeOrderAmount { get; set; }
    public string VariableConsiderationConstraintNote { get; set; } = string.Empty;
    public decimal TotalContractPriceAllocated { get; set; }
    public decimal TotalRecognizedRevenue { get; set; }
    public List<FiveStepObligationRowDto> Obligations { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class FiveStepContractDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string ProjectType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? ContractValue { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
}

#pragma warning disable CA1002, CA2227
public class FiveStepObligationRowDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? StandaloneSellingPriceBasis { get; set; }
    public decimal TransactionPriceAllocated { get; set; }
    public decimal AllocationSharePercent { get; set; }
    public decimal RecognizedRevenueToDate { get; set; }
    public decimal PercentSatisfied { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SatisfiedOn { get; set; }
}
#pragma warning restore CA1002, CA2227

#pragma warning disable CA1002, CA2227
public class Asc606ObligationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TransactionPriceAllocated { get; set; }
    public string? StandaloneSellingPriceBasis { get; set; }
    public decimal RecognizedRevenueToDate { get; set; }
    public decimal PercentSatisfied { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SatisfiedOn { get; set; }
    public bool CanEditOrDelete { get; set; }
}
#pragma warning restore CA1002, CA2227

public class CreateAsc606ObligationRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal TransactionPriceAllocated { get; set; }
    public string? StandaloneSellingPriceBasis { get; set; }
}

public class UpdateAsc606ObligationRequest
{
    public string? Description { get; set; }
    public decimal? TransactionPriceAllocated { get; set; }
    public string? StandaloneSellingPriceBasis { get; set; }
}

public class AllocateRequest
{
    public decimal TotalContractPrice { get; set; }
}

#pragma warning disable CA1002, CA2227
public class AllocationRowDto
{
    public Guid ObligationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? StandaloneSellingPriceBasis { get; set; }
    public decimal PriorAllocation { get; set; }
    public decimal NewAllocation { get; set; }
    public decimal SharePercent { get; set; }
}

public class AllocateResultDto
{
    public Guid ProjectId { get; set; }
    public decimal TotalContractPrice { get; set; }
    public List<AllocationRowDto> Rows { get; set; } = [];
}
#pragma warning restore CA1002, CA2227
