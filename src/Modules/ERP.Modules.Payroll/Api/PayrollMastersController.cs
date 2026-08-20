// <copyright file="PayrollMastersController.cs" company="ERP Project">
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
public class PayrollMastersController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollMastersController(PayrollDbContext context)
    {
        _context = context;
    }

    // --- Deduction / benefit master + employee enrollment ---
    [HttpPost("deduction-benefits")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateDeductionBenefit(
        [FromBody] CreateDeductionBenefitRequest request, CancellationToken cancellationToken)
    {
        var db = new DeductionBenefit(request.CompanyId, request.Code, request.Description, request.Type, request.IsPreTax, request.DefaultRate, request.GlAccountNumber);
        _context.DeductionBenefits.Add(db);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(db.Id));
    }

    [HttpGet("deduction-benefits")]
    public async Task<ActionResult<ApiResponse<List<DeductionBenefitDto>>>> GetDeductionBenefits(CancellationToken cancellationToken)
    {
        var list = await _context.DeductionBenefits
            .Select(d => new DeductionBenefitDto
            {
                Id = d.Id, Code = d.Code, Description = d.Description, Type = d.Type.ToString(),
                IsPreTax = d.IsPreTax, DefaultRate = d.DefaultRate, GlAccountNumber = d.GlAccountNumber, IsActive = d.IsActive,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<DeductionBenefitDto>>.Success(list));
    }

    [HttpPost("employees/{employeeId:guid}/deduction-benefits")]
    public async Task<ActionResult<ApiResponse<Guid>>> EnrollDeductionBenefit(
        Guid employeeId, [FromBody] EnrollDeductionBenefitRequest request, CancellationToken cancellationToken)
    {
        var enr = new EmployeeDeductionBenefit(employeeId, request.DeductionBenefitId, request.Amount, request.Percent, request.StartDate, request.EndDate);
        _context.EmployeeDeductionBenefits.Add(enr);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(enr.Id));
    }

    // --- W-4 / withholding record ---
    [HttpPost("employees/{employeeId:guid}/w4")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateW4(
        Guid employeeId, [FromBody] CreateW4Request request, CancellationToken cancellationToken)
    {
        // Supersede any active W-4 for this employee.
        var active = await _context.W4Records
            .Where(w => w.EmployeeId == employeeId && w.EndDate == null)
            .ToListAsync(cancellationToken);
        foreach (var w in active)
            w.Supersede(DateTime.UtcNow);

        var rec = new W4Record(employeeId, request.FilingStatus, request.Allowances, request.IsLegacyPre2020,
            request.AdditionalWithholding, request.MultipleJobs, request.DependentsCredit, request.OtherIncome, request.Deductions);
        _context.W4Records.Add(rec);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(rec.Id));
    }

    [HttpGet("employees/{employeeId:guid}/w4")]
    public async Task<ActionResult<ApiResponse<List<W4RecordDto>>>> GetW4(Guid employeeId, CancellationToken cancellationToken)
    {
        var list = await _context.W4Records
            .Where(w => w.EmployeeId == employeeId)
            .OrderByDescending(w => w.EffectiveDate)
            .Select(w => new W4RecordDto
            {
                Id = w.Id, FilingStatus = w.FilingStatus.ToString(), Allowances = w.Allowances,
                IsLegacyPre2020 = w.IsLegacyPre2020, AdditionalWithholding = w.AdditionalWithholding,
                MultipleJobs = w.MultipleJobs, DependentsCredit = w.DependentsCredit,
                OtherIncome = w.OtherIncome, Deductions = w.Deductions, EffectiveDate = w.EffectiveDate,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<W4RecordDto>>.Success(list));
    }

    // --- Wage base / limit table ---
    [HttpPost("wage-base-limits")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateWageBaseLimit(
        [FromBody] CreateWageBaseLimitRequest request, CancellationToken cancellationToken)
    {
        var lim = new WageBaseLimit(request.CompanyId, request.Name, request.Type, request.Year, request.LimitAmount, request.SurtaxThreshold);
        _context.WageBaseLimits.Add(lim);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(lim.Id));
    }

    [HttpGet("wage-base-limits")]
    public async Task<ActionResult<ApiResponse<List<WageBaseLimitDto>>>> GetWageBaseLimits([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var q = _context.WageBaseLimits.AsQueryable();
        if (year.HasValue) q = q.Where(l => l.Year == year.Value);
        var list = await q
            .Select(l => new WageBaseLimitDto
            {
                Id = l.Id, Name = l.Name, Type = l.Type.ToString(), Year = l.Year,
                LimitAmount = l.LimitAmount, SurtaxThreshold = l.SurtaxThreshold,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WageBaseLimitDto>>.Success(list));
    }

    // --- Workers' comp class code ---
    [HttpPost("workers-comp-class-codes")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateWorkersCompClassCode(
        [FromBody] CreateWorkersCompClassCodeRequest request, CancellationToken cancellationToken)
    {
        var cc = new WorkersCompClassCode(request.CompanyId, request.ClassCode, request.Description, request.State, request.RatePer100, request.ExperienceModification);
        _context.WorkersCompClassCodes.Add(cc);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(cc.Id));
    }

    // --- PTO ledger (accrual) ---
    [HttpPost("employees/{employeeId:guid}/pto-ledger")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePtoLedger(
        Guid employeeId, [FromBody] CreatePtoLedgerRequest request, CancellationToken cancellationToken)
    {
        var ledger = new PtoLedger(employeeId, request.PolicyName, request.AccrualRate, request.MaxAccrual, request.CarryoverLimit);
        _context.PtoLedgers.Add(ledger);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(ledger.Id));
    }

    [HttpPost("pto-ledgers/{id:guid}/accrue")]
    public async Task<ActionResult<ApiResponse>> AccruePto(Guid id, [FromBody] PtoAccrualRequest request, CancellationToken cancellationToken)
    {
        var ledger = await _context.PtoLedgers.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (ledger is null)
            return NotFound(ApiResponse.Failure(new[] { "PTO ledger not found." }, 404));
        ledger.Accrue(request.Hours, request.AsOf);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("pto-ledgers/{id:guid}/use")]
    public async Task<ActionResult<ApiResponse>> UsePto(Guid id, [FromBody] PtoUsageRequest request, CancellationToken cancellationToken)
    {
        var ledger = await _context.PtoLedgers.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (ledger is null)
            return NotFound(ApiResponse.Failure(new[] { "PTO ledger not found." }, 404));
        ledger.Use(request.Hours, request.AsOf);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpGet("pto-ledgers")]
    public async Task<ActionResult<ApiResponse<List<PtoLedgerDto>>>> GetPtoLedgers([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var q = _context.PtoLedgers.AsQueryable();
        if (employeeId.HasValue) q = q.Where(l => l.EmployeeId == employeeId.Value);
        var list = await q
            .Select(l => new PtoLedgerDto
            {
                Id = l.Id, EmployeeId = l.EmployeeId, PolicyName = l.PolicyName,
                Accrued = l.Accrued, Used = l.Used, Available = l.Available, Carryover = l.Carryover,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PtoLedgerDto>>.Success(list));
    }

    // --- Manual (off-cycle) checks ---
    [HttpPost("manual-checks")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateManualCheck(
        [FromBody] CreateManualCheckRequest request, CancellationToken cancellationToken)
    {
        var mc = new ManualCheck(request.CompanyId, request.EmployeeId, request.Amount, request.CheckDate, request.Reason, request.CheckNumber);
        _context.ManualChecks.Add(mc);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(mc.Id));
    }
}

// Request / DTO types
public class CreateDeductionBenefitRequest
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeductionBenefitType Type { get; set; }
    public bool IsPreTax { get; set; }
    public decimal? DefaultRate { get; set; }
    public string? GlAccountNumber { get; set; }
}

public class DeductionBenefitDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPreTax { get; set; }
    public decimal? DefaultRate { get; set; }
    public string? GlAccountNumber { get; set; }
    public bool IsActive { get; set; }
}

public class EnrollDeductionBenefitRequest
{
    public Guid DeductionBenefitId { get; set; }
    public decimal Amount { get; set; }
    public decimal? Percent { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CreateW4Request
{
    public FilingStatus FilingStatus { get; set; }
    public int Allowances { get; set; }
    public bool IsLegacyPre2020 { get; set; }
    public decimal? AdditionalWithholding { get; set; }
    public bool MultipleJobs { get; set; }
    public int DependentsCredit { get; set; }
    public decimal? OtherIncome { get; set; }
    public decimal? Deductions { get; set; }
}

public class W4RecordDto
{
    public Guid Id { get; set; }
    public string FilingStatus { get; set; } = string.Empty;
    public int Allowances { get; set; }
    public bool IsLegacyPre2020 { get; set; }
    public decimal? AdditionalWithholding { get; set; }
    public bool MultipleJobs { get; set; }
    public int DependentsCredit { get; set; }
    public decimal? OtherIncome { get; set; }
    public decimal? Deductions { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class CreateWageBaseLimitRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public WageBaseType Type { get; set; }
    public int Year { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal? SurtaxThreshold { get; set; }
}

public class WageBaseLimitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal? SurtaxThreshold { get; set; }
}

public class CreateWorkersCompClassCodeRequest
{
    public Guid CompanyId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal RatePer100 { get; set; }
    public decimal? ExperienceModification { get; set; }
}

public class CreatePtoLedgerRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public decimal AccrualRate { get; set; }
    public decimal MaxAccrual { get; set; }
    public decimal CarryoverLimit { get; set; }
}

public class PtoAccrualRequest
{
    public decimal Hours { get; set; }
    public DateTime AsOf { get; set; }
}

public class PtoUsageRequest
{
    public decimal Hours { get; set; }
    public DateTime AsOf { get; set; }
}

public class PtoLedgerDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal Accrued { get; set; }
    public decimal Used { get; set; }
    public decimal Available { get; set; }
    public decimal Carryover { get; set; }
}

public class CreateManualCheckRequest
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CheckDate { get; set; }
    public string? Reason { get; set; }
    public string? CheckNumber { get; set; }
}
