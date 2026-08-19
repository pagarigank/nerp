// <copyright file="BillingController.cs" company="ERP Project">
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
[Route("api/v1/projects/{projectId:guid}/billing")]
public class BillingController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;
    private readonly ArInvoiceCreator _arInvoiceCreator;

    public BillingController(ProjDbContext context, IProjUnitOfWork unitOfWork, ArInvoiceCreator arInvoiceCreator)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _arInvoiceCreator = arInvoiceCreator;
    }

    // --- Contract Lines ---
    [HttpGet("contracts")]
    public async Task<ActionResult<ApiResponse<List<ContractLineDto>>>> GetContractLines(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var lines = await _context.ContractLines
            .Where(c => c.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var dtos = lines.Select(c => new ContractLineDto
        {
            Id = c.Id,
            ProjectId = c.ProjectId,
            Description = c.Description,
            BillingMethod = c.BillingMethod.ToString(),
            ContractAmount = c.ContractAmount,
            UnitPrice = c.UnitPrice,
            UnitQuantity = c.UnitQuantity,
            FeePercentage = c.FeePercentage,
            NotToExceed = c.NotToExceed,
            BilledAmount = c.BilledAmount,
            Remaining = c.Remaining,
            PercentComplete = c.PercentComplete,
            IsActive = c.IsActive,
            Notes = c.Notes,
        }).ToList();

        return Ok(ApiResponse<List<ContractLineDto>>.Success(dtos));
    }

    [HttpPost("contracts")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddContractLine(
        Guid projectId,
        [FromBody] AddContractLineRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (!Enum.TryParse<BillingMethod>(request.BillingMethod, true, out var method))
            return BadRequest(ApiResponse.Failure(new[] { "Invalid billing method." }));

        var line = new ContractLine(
            projectId,
            request.Description,
            method,
            request.ContractAmount,
            request.UnitPrice,
            request.UnitQuantity,
            request.FeePercentage,
            request.NotToExceed,
            request.Notes);

        _context.ContractLines.Add(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    // --- Billing Schedule ---
    [HttpGet("schedule")]
    public async Task<ActionResult<ApiResponse<List<BillingScheduleDto>>>> GetBillingSchedule(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var schedule = await _context.BillingSchedules
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.SequenceNumber)
            .ToListAsync(cancellationToken);

        var dtos = schedule.Select(b => new BillingScheduleDto
        {
            Id = b.Id,
            ProjectId = b.ProjectId,
            Description = b.Description,
            BillingMethod = b.BillingMethod.ToString(),
            Amount = b.Amount,
            PercentCompleteTrigger = b.PercentCompleteTrigger,
            ScheduledDate = b.ScheduledDate,
            SequenceNumber = b.SequenceNumber,
            IsBilled = b.IsBilled,
            BilledDate = b.BilledDate,
        }).ToList();

        return Ok(ApiResponse<List<BillingScheduleDto>>.Success(dtos));
    }

    [HttpPost("schedule")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddBillingSchedule(
        Guid projectId,
        [FromBody] AddBillingScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FindAsync(new object[] { projectId }, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (!Enum.TryParse<BillingMethod>(request.BillingMethod, true, out var method))
            return BadRequest(ApiResponse.Failure(new[] { "Invalid billing method." }));

        var schedule = new BillingSchedule(
            projectId,
            request.Description,
            method,
            request.Amount,
            request.PercentCompleteTrigger,
            request.ScheduledDate,
            request.SequenceNumber);

        _context.BillingSchedules.Add(schedule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(schedule.Id));
    }

    // --- Generate Invoice ---
    [HttpPost("generate-invoice")]
    public async Task<ActionResult<ApiResponse<BillingResultDto>>> GenerateInvoice(
        Guid projectId,
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.ContractLines)
            .Include(p => p.BillingSchedules)
            .Include(p => p.CostTransactions)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        // Billing hold: do not generate invoices while on hold (dispute, customer request, compliance).
        if (project.BillingHold)
            return BadRequest(ApiResponse.Failure(new[] { $"Billing is on hold: {project.BillingHoldReason ?? "no reason provided"}." }));

        decimal totalInvoiceAmount = 0;
        decimal totalRetainage = 0;
        var invoiceLines = new List<InvoiceLineDto>();

        foreach (var contractLine in project.ContractLines.Where(c => c.IsActive))
        {
            decimal lineAmount = 0;

            switch (contractLine.BillingMethod)
            {
                case BillingMethod.Milestone:
                    var unbilledMilestones = project.BillingSchedules
                        .Where(b => !b.IsBilled && b.BillingMethod == BillingMethod.Milestone);
                    lineAmount = unbilledMilestones.Sum(b => b.Amount);
                    break;

                case BillingMethod.PercentComplete:
                    var earnedAmount = contractLine.ContractAmount * (project.PercentComplete / 100m);
                    lineAmount = earnedAmount - contractLine.BilledAmount;
                    break;

                case BillingMethod.UnitPrice:
                    lineAmount = ((contractLine.UnitPrice ?? 0) * (contractLine.UnitQuantity ?? 0)) - contractLine.BilledAmount;
                    break;

                case BillingMethod.TimeAndMaterials:
                    // Sum unbilled, billable cost transactions (labor + expenses) and apply markup.
                    var unbilledCosts = project.CostTransactions
                        .Where(t => t.IsBillable && !t.IsBilled && t.Status == TransactionStatus.Posted)
                        .ToList();
                    var rawCost = unbilledCosts.Sum(t => t.BillableAmount > 0 ? t.BillableAmount : t.Amount);
                    var tmMarkup = contractLine.FeePercentage ?? 0m;
                    lineAmount = rawCost * (1m + (tmMarkup / 100m));
                    foreach (var c in unbilledCosts)
                    {
                        c.MarkBilled();
                    }

                    break;

                case BillingMethod.CostPlus:
                    // Bill costs-to-date × fee percentage.
                    var costPlusBase = project.CostsToDate;
                    var cpFee = contractLine.FeePercentage ?? 0m;
                    lineAmount = (costPlusBase * (1m + (cpFee / 100m))) - contractLine.BilledAmount;
                    break;

                default:
                    lineAmount = contractLine.ContractAmount - contractLine.BilledAmount;
                    break;
            }

            if (lineAmount <= 0)
            {
                continue;
            }

            // NTE enforcement
            if (contractLine.NotToExceed.HasValue && contractLine.BilledAmount + lineAmount > contractLine.NotToExceed.Value)
            {
                lineAmount = contractLine.NotToExceed.Value - contractLine.BilledAmount;
            }

            // Apply retainage
            var retainage = lineAmount * (project.RetainagePercentage / 100m);
            var netAmount = lineAmount - retainage;

            totalInvoiceAmount += netAmount;
            totalRetainage += retainage;

            invoiceLines.Add(new InvoiceLineDto
            {
                ContractLineId = contractLine.Id,
                Description = contractLine.Description,
                GrossAmount = lineAmount,
                RetainageAmount = retainage,
                NetAmount = netAmount,
            });

            contractLine.UpdateBilling(contractLine.BilledAmount + lineAmount, project.PercentComplete);
        }

        // Mark milestones as billed
        var milestonesBilled = project.BillingSchedules
            .Where(b => !b.IsBilled && b.BillingMethod == BillingMethod.Milestone)
            .ToList();

        // Update retainage held and revenue
        project.SetRetainage(project.RetainagePercentage);
        project.AddRetainageHeld(totalRetainage);
        project.AddRevenue(totalInvoiceAmount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Billing-to-AR integration: create the AR invoice for the net billed amount (§7.2).
        Guid? arInvoiceId = null;
        if (totalInvoiceAmount > 0)
        {
            var invoiceNumber = $"PJ-{project.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture)[..8].ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmm}";
            arInvoiceId = await _arInvoiceCreator.CreateProjectInvoiceAsync(
                project, invoiceNumber, totalInvoiceAmount, $"Progress billing - {project.Name}", DateTimeOffset.UtcNow, cancellationToken);
            foreach (var m in milestonesBilled)
            {
                m.MarkBilled(arInvoiceId.Value);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new BillingResultDto
        {
            ProjectId = projectId,
            InvoiceAmount = totalInvoiceAmount,
            RetainageHeld = totalRetainage,
            ArInvoiceId = arInvoiceId,
            Lines = invoiceLines,
        };

        return Ok(ApiResponse<BillingResultDto>.Success(result));
    }
}

public class ContractLineDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string BillingMethod { get; set; } = string.Empty;
    public decimal ContractAmount { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? UnitQuantity { get; set; }
    public decimal? FeePercentage { get; set; }
    public decimal? NotToExceed { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentComplete { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class BillingScheduleDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string BillingMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? PercentCompleteTrigger { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsBilled { get; set; }
    public DateTime? BilledDate { get; set; }
}

public class BillingResultDto
{
    public Guid ProjectId { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal RetainageHeld { get; set; }
    public Guid? ArInvoiceId { get; set; }
#pragma warning disable CA1002, CA2227
    public List<InvoiceLineDto> Lines { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

public class InvoiceLineDto
{
    public Guid ContractLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal RetainageAmount { get; set; }
    public decimal NetAmount { get; set; }
}

public class AddContractLineRequest
{
    public string Description { get; set; } = string.Empty;
    public string BillingMethod { get; set; } = "FixedPrice";
    public decimal ContractAmount { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? UnitQuantity { get; set; }
    public decimal? FeePercentage { get; set; }
    public decimal? NotToExceed { get; set; }
    public string? Notes { get; set; }
}

public class AddBillingScheduleRequest
{
    public string Description { get; set; } = string.Empty;
    public string BillingMethod { get; set; } = "Milestone";
    public decimal Amount { get; set; }
    public decimal? PercentCompleteTrigger { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public int SequenceNumber { get; set; }
}

public class GenerateInvoiceRequest
{
    public string? Notes { get; set; }
}
