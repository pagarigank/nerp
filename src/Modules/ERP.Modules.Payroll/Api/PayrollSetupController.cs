// <copyright file="PayrollSetupController.cs" company="ERP Project">
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
public class PayrollSetupController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollSetupController(PayrollDbContext context)
    {
        _context = context;
    }

    // --- Company payroll setup (single per company) ---
    [HttpPost("company-setup")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCompanySetup(
        [FromBody] CreateCompanySetupRequest request, CancellationToken cancellationToken)
    {
        var existing = await _context.CompanyPayrollSetups.FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, cancellationToken);
        if (existing is not null)
            return Conflict(ApiResponse.Failure(new[] { "Company payroll setup already exists." }, 409));

        var setup = new CompanyPayrollSetup(request.CompanyId, request.Ein, request.FederalTaxId, request.StateTaxId,
            request.SutaState, request.EftpsPin, request.DepositSchedule, request.SocialSecurityRate, request.MedicareRate,
            request.FutaRate, request.SutaRate, request.WageExpenseAccountId, request.PayrollTaxExpenseAccountId,
            request.PayrollLiabilityAccountId, request.ClearingAccountId);
        _context.CompanyPayrollSetups.Add(setup);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(setup.Id));
    }

    [HttpGet("company-setup")]
    public async Task<ActionResult<ApiResponse<CompanyPayrollSetupDto?>>> GetCompanySetup(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var setup = await _context.CompanyPayrollSetups.FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken);
        if (setup is null)
            return Ok(ApiResponse<CompanyPayrollSetupDto?>.Success(null));
        return Ok(ApiResponse<CompanyPayrollSetupDto?>.Success(new CompanyPayrollSetupDto
        {
            Id = setup.Id,
            Ein = setup.Ein,
            FederalTaxId = setup.FederalTaxId,
            StateTaxId = setup.StateTaxId,
            SutaState = setup.SutaState,
            EftpsPin = setup.EftpsPin,
            DepositSchedule = setup.DepositSchedule,
            SocialSecurityRate = setup.SocialSecurityRate,
            MedicareRate = setup.MedicareRate,
            FutaRate = setup.FutaRate,
            SutaRate = setup.SutaRate,
        }));
    }

    // --- PTO policies ---
    [HttpPost("pto-policies")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePtoPolicy(
        [FromBody] CreatePtoPolicyRequest request, CancellationToken cancellationToken)
    {
        var policy = new PtoPolicy(request.CompanyId, request.Name, request.AccrualRate, request.AccrualBasis,
            request.MaxAccrual, request.CarryoverLimit, request.CashOutRate, request.CashOutAllowed);
        _context.PtoPolicies.Add(policy);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(policy.Id));
    }

    [HttpGet("pto-policies")]
    public async Task<ActionResult<ApiResponse<List<PtoPolicyDto>>>> GetPtoPolicies(CancellationToken cancellationToken)
    {
        var list = await _context.PtoPolicies
            .Select(p => new PtoPolicyDto
            {
                Id = p.Id, Name = p.Name, AccrualRate = p.AccrualRate, AccrualBasis = p.AccrualBasis,
                MaxAccrual = p.MaxAccrual, CarryoverLimit = p.CarryoverLimit, CashOutAllowed = p.CashOutAllowed,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PtoPolicyDto>>.Success(list));
    }

    // --- New-hire reporting config (per state) ---
    [HttpPost("new-hire-configs")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateNewHireConfig(
        [FromBody] CreateNewHireConfigRequest request, CancellationToken cancellationToken)
    {
        var cfg = new NewHireReportingConfig(request.CompanyId, request.StateCode, request.AgencyName,
            request.DueWindowDays, request.TransmissionMethod, request.SftpEndpoint, request.AgencyId);
        _context.NewHireReportingConfigs.Add(cfg);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(cfg.Id));
    }

    [HttpGet("new-hire-configs")]
    public async Task<ActionResult<ApiResponse<List<NewHireConfigDto>>>> GetNewHireConfigs(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var list = await _context.NewHireReportingConfigs
            .Where(c => c.CompanyId == companyId)
            .Select(c => new NewHireConfigDto
            {
                Id = c.Id, StateCode = c.StateCode, AgencyName = c.AgencyName, DueWindowDays = c.DueWindowDays,
                TransmissionMethod = c.TransmissionMethod,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<NewHireConfigDto>>.Success(list));
    }

    // --- ACH returns ---
    [HttpPost("ach-returns")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAchReturn(
        [FromBody] CreateAchReturnRequest request, CancellationToken cancellationToken)
    {
        var ret = new AchReturn(request.CompanyId, request.PayrollRunId, request.EmployeeId, request.TraceNumber,
            request.ReturnCode, request.Description, request.Amount, request.ReturnAction);
        _context.AchReturns.Add(ret);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(ret.Id));
    }

    [HttpGet("ach-returns")]
    public async Task<ActionResult<ApiResponse<List<AchReturnDto>>>> GetAchReturns(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var list = await _context.AchReturns
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedOn)
            .Select(r => new AchReturnDto
            {
                Id = r.Id, ReturnCode = r.ReturnCode, Description = r.Description, Amount = r.Amount,
                ReturnAction = r.ReturnAction, Processed = r.Processed, TraceNumber = r.TraceNumber,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<AchReturnDto>>.Success(list));
    }

    [HttpPost("ach-returns/{id:guid}/process")]
    public async Task<ActionResult<ApiResponse>> ProcessAchReturn(Guid id, CancellationToken cancellationToken)
    {
        var ret = await _context.AchReturns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (ret is null) return NotFound(ApiResponse.Failure(new[] { "ACH return not found." }, 404));
        ret.MarkProcessed();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }
}

// Request / DTO types
public class CreateCompanySetupRequest
{
    public Guid CompanyId { get; set; }
    public string Ein { get; set; } = string.Empty;
    public string FederalTaxId { get; set; } = string.Empty;
    public string? StateTaxId { get; set; }
    public string? SutaState { get; set; }
    public string EftpsPin { get; set; } = string.Empty;
    public string DepositSchedule { get; set; } = string.Empty;
    public decimal SocialSecurityRate { get; set; }
    public decimal MedicareRate { get; set; }
    public decimal FutaRate { get; set; }
    public decimal SutaRate { get; set; }
    public Guid WageExpenseAccountId { get; set; }
    public Guid PayrollTaxExpenseAccountId { get; set; }
    public Guid PayrollLiabilityAccountId { get; set; }
    public Guid ClearingAccountId { get; set; }
}

public class CompanyPayrollSetupDto
{
    public Guid Id { get; set; }
    public string Ein { get; set; } = string.Empty;
    public string FederalTaxId { get; set; } = string.Empty;
    public string? StateTaxId { get; set; }
    public string? SutaState { get; set; }
    public string EftpsPin { get; set; } = string.Empty;
    public string DepositSchedule { get; set; } = string.Empty;
    public decimal SocialSecurityRate { get; set; }
    public decimal MedicareRate { get; set; }
    public decimal FutaRate { get; set; }
    public decimal SutaRate { get; set; }
}

public class CreatePtoPolicyRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AccrualRate { get; set; }
    public string AccrualBasis { get; set; } = string.Empty;
    public decimal MaxAccrual { get; set; }
    public decimal CarryoverLimit { get; set; }
    public decimal? CashOutRate { get; set; }
    public bool CashOutAllowed { get; set; }
}

public class PtoPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AccrualRate { get; set; }
    public string AccrualBasis { get; set; } = string.Empty;
    public decimal MaxAccrual { get; set; }
    public decimal CarryoverLimit { get; set; }
    public bool CashOutAllowed { get; set; }
}

public class CreateNewHireConfigRequest
{
    public Guid CompanyId { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;
    public int DueWindowDays { get; set; }
    public string TransmissionMethod { get; set; } = string.Empty;
    public string? SftpEndpoint { get; set; }
    public string? AgencyId { get; set; }
}

public class NewHireConfigDto
{
    public Guid Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;
    public int DueWindowDays { get; set; }
    public string TransmissionMethod { get; set; } = string.Empty;
}

public class CreateAchReturnRequest
{
    public Guid CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public string ReturnCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReturnAction { get; set; } = string.Empty;
}

public class AchReturnDto
{
    public Guid Id { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public string ReturnCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReturnAction { get; set; } = string.Empty;
    public bool Processed { get; set; }
}
