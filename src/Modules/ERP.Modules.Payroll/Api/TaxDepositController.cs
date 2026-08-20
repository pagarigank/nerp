// <copyright file="TaxDepositController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll")]
public class TaxDepositController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public TaxDepositController(PayrollDbContext context)
    {
        _context = context;
    }

    [HttpPost("tax-deposits")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTaxDeposit(
        [FromBody] CreateTaxDepositRequest request, CancellationToken cancellationToken)
    {
        var sched = new TaxDepositSchedule(request.CompanyId, request.TaxType, request.Agency,
            request.PayrollRunId, request.DepositDate, request.EstimatedAmount, request.Frequency, request.FormType);
        _context.TaxDepositSchedules.Add(sched);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(sched.Id));
    }

    [HttpGet("tax-deposits")]
    public async Task<ActionResult<ApiResponse<List<TaxDepositDto>>>> GetTaxDeposits(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var list = await _context.TaxDepositSchedules
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.DepositDate)
            .Select(s => new TaxDepositDto
            {
                Id = s.Id,
                TaxType = s.TaxType,
                Agency = s.Agency,
                DepositDate = s.DepositDate,
                EstimatedAmount = s.EstimatedAmount,
                DepositedAmount = s.DepositedAmount,
                DepositedOn = s.DepositedOn,
                Frequency = s.Frequency,
                FormType = s.FormType,
                Deposited = s.Deposited,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TaxDepositDto>>.Success(list));
    }

    [HttpPost("tax-deposits/{id:guid}/deposit")]
    public async Task<ActionResult<ApiResponse>> MarkDeposited(
        Guid id, [FromBody] MarkDepositedRequest request, CancellationToken cancellationToken)
    {
        var sched = await _context.TaxDepositSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sched is null) return NotFound(ApiResponse.Failure(new[] { "Tax deposit schedule not found." }, 404));
        sched.MarkDeposited(request.DepositedAmount, request.DepositedOn);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    /// <summary>
    /// Auto-schedules federal (941/EFTPS) and FUTA deposits for a posted run based on
    /// the company deposit schedule. Semi-weekly deposits land the next banking day +3/5;
    /// monthly deposits land on the 15th of the following month. Returns the created schedules.
    /// </summary>
    [HttpPost("tax-deposits/generate/{runId:guid}")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GenerateForRun(
        Guid runId, [FromQuery] string frequency, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null) return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        if (run.Status != PayrollRunStatus.Posted)
            return BadRequest(ApiResponse.Failure(new[] { "Only a posted run can generate tax deposits." }));

        var created = new List<Guid>();
        var fedTax = run.TotalEmployeeTax + run.TotalEmployerTax;
        var futa = Math.Round(run.TotalGross * 0.006m, 2);

        DateTime FedDate() => frequency.Equals("SemiWeekly", StringComparison.OrdinalIgnoreCase)
            ? run.PayDate.AddDays(3)
            : run.PayDate.AddMonths(1).AddDays(15 - run.PayDate.Day);

        var fed = new TaxDepositSchedule(run.CompanyId, "Federal941", "EFTPS", runId, FedDate(), fedTax, frequency, "941");
        _context.TaxDepositSchedules.Add(fed);
        created.Add(fed.Id);

        var futaSched = new TaxDepositSchedule(run.CompanyId, "FUTA", "EFTPS", runId,
            new DateTime(run.PayDate.Year, 1, 31, 0, 0, 0, DateTimeKind.Utc).AddYears(run.PayDate.Month > 3 ? 1 : 0), futa,
            "Quarterly", "940");
        _context.TaxDepositSchedules.Add(futaSched);
        created.Add(futaSched.Id);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<List<Guid>>.Success(created));
    }
}

public class CreateTaxDepositRequest
{
    public Guid CompanyId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public Guid? PayrollRunId { get; set; }
    public DateTime DepositDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string? FormType { get; set; }
}

public class MarkDepositedRequest
{
    public decimal DepositedAmount { get; set; }
    public DateTime DepositedOn { get; set; }
}

public class TaxDepositDto
{
    public Guid Id { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public DateTime DepositDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal? DepositedAmount { get; set; }
    public DateTime? DepositedOn { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string? FormType { get; set; }
    public bool Deposited { get; set; }
}
