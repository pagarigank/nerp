// <copyright file="PayrollTaxController.cs" company="ERP Project">
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
public class PayrollTaxController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollTaxController(PayrollDbContext context)
    {
        _context = context;
    }

    // --- Tax tables (with brackets) ---
    [HttpPost("tax-tables")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTaxTable(
        [FromBody] CreateTaxTableRequest request, CancellationToken cancellationToken)
    {
        var table = new TaxTable(request.CompanyId, request.Name, request.Level, request.StateCode,
            request.Year, request.FilingStatus, request.StandardDeduction);
        foreach (var b in request.Brackets)
            table.AddBracket(b.Rate, b.LowerBound, b.UpperBound, b.FixedAmount);
        _context.TaxTables.Add(table);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(table.Id));
    }

    [HttpGet("tax-tables")]
    public async Task<ActionResult<ApiResponse<List<TaxTableDto>>>> GetTaxTables(
        [FromQuery] int? year, [FromQuery] string? stateCode, CancellationToken cancellationToken)
    {
        var q = _context.TaxTables.AsQueryable();
        if (year.HasValue) q = q.Where(t => t.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(stateCode)) q = q.Where(t => t.StateCode == stateCode);
        var list = await q
            .OrderBy(t => t.Level).ThenBy(t => t.Year)
            .Select(t => new TaxTableDto
            {
                Id = t.Id,
                Name = t.Name,
                Level = t.Level.ToString(),
                StateCode = t.StateCode,
                Year = t.Year,
                FilingStatus = t.FilingStatus.ToString(),
                StandardDeduction = t.StandardDeduction,
                BracketCount = t.Brackets.Count,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TaxTableDto>>.Success(list));
    }

    // --- Tax jurisdictions ---
    [HttpPost("tax-jurisdictions")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTaxJurisdiction(
        [FromBody] CreateTaxJurisdictionRequest request, CancellationToken cancellationToken)
    {
        var jur = new TaxJurisdiction(request.CompanyId, request.Code, request.Name, request.Level,
            request.StateCode, request.HasReciprocalAgreement, request.ReciprocalWithState,
            request.LocalRate, request.FilingFrequency);
        _context.TaxJurisdictions.Add(jur);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(jur.Id));
    }

    [HttpGet("tax-jurisdictions")]
    public async Task<ActionResult<ApiResponse<List<TaxJurisdictionDto>>>> GetTaxJurisdictions(
        [FromQuery] string? stateCode, CancellationToken cancellationToken)
    {
        var q = _context.TaxJurisdictions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(stateCode)) q = q.Where(j => j.StateCode == stateCode);
        var list = await q
            .OrderBy(j => j.Level).ThenBy(j => j.Code)
            .Select(j => new TaxJurisdictionDto
            {
                Id = j.Id,
                Code = j.Code,
                Name = j.Name,
                Level = j.Level.ToString(),
                StateCode = j.StateCode,
                HasReciprocalAgreement = j.HasReciprocalAgreement,
                ReciprocalWithState = j.ReciprocalWithState,
                LocalRate = j.LocalRate,
                FilingFrequency = j.FilingFrequency,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TaxJurisdictionDto>>.Success(list));
    }

    // --- Employee tax profile ---
    [HttpPost("employees/{employeeId:guid}/tax-profile")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEmployeeTaxProfile(
        Guid employeeId, [FromBody] CreateEmployeeTaxProfileRequest request, CancellationToken cancellationToken)
    {
        var existing = await _context.EmployeeTaxProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId && p.EmployeeId == employeeId, cancellationToken);
        if (existing is not null)
            return Conflict(ApiResponse.Failure(new[] { "Tax profile already exists for this employee." }, 409));

        var profile = new EmployeeTaxProfile(request.CompanyId, employeeId, request.ResidentState,
            request.WorkState, request.AdditionalFederalWithholding, request.AdditionalStateWithholding,
            request.ExemptFederal, request.ExemptState);
        _context.EmployeeTaxProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(profile.Id));
    }

    [HttpPut("employees/{employeeId:guid}/tax-profile")]
    public async Task<ActionResult<ApiResponse>> UpdateEmployeeTaxProfile(
        Guid employeeId, [FromBody] CreateEmployeeTaxProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _context.EmployeeTaxProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId && p.EmployeeId == employeeId, cancellationToken);
        if (profile is null)
            return NotFound(ApiResponse.Failure(new[] { "Tax profile not found for this employee." }, 404));

        profile.Update(request.ResidentState, request.WorkState, request.AdditionalFederalWithholding,
            request.AdditionalStateWithholding, request.ExemptFederal, request.ExemptState);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpGet("employees/{employeeId:guid}/tax-profile")]
    public async Task<ActionResult<ApiResponse<EmployeeTaxProfileDto>>> GetEmployeeTaxProfile(
        Guid employeeId, [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var profile = await _context.EmployeeTaxProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.EmployeeId == employeeId, cancellationToken);
        if (profile is null)
            return NotFound(ApiResponse.Failure(new[] { "Tax profile not found for this employee." }, 404));

        var dto = new EmployeeTaxProfileDto
        {
            Id = profile.Id,
            EmployeeId = profile.EmployeeId,
            ResidentState = profile.ResidentState,
            WorkState = profile.WorkState,
            AdditionalFederalWithholding = profile.AdditionalFederalWithholding,
            AdditionalStateWithholding = profile.AdditionalStateWithholding,
            ExemptFederal = profile.ExemptFederal,
            ExemptState = profile.ExemptState,
        };
        return Ok(ApiResponse<EmployeeTaxProfileDto>.Success(dto));
    }
}

// Request / DTO types
public class CreateTaxTableRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TaxJurisdictionLevel Level { get; set; }
    public string? StateCode { get; set; }
    public int Year { get; set; }
    public FilingStatus FilingStatus { get; set; }
    public decimal? StandardDeduction { get; set; }
    public List<CreateTaxBracketRequest> Brackets { get; set; } = [];
}

public class CreateTaxBracketRequest
{
    public decimal Rate { get; set; }
    public decimal LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal? FixedAmount { get; set; }
}

public class TaxTableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? StateCode { get; set; }
    public int Year { get; set; }
    public string FilingStatus { get; set; } = string.Empty;
    public decimal? StandardDeduction { get; set; }
    public int BracketCount { get; set; }
}

public class CreateTaxJurisdictionRequest
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TaxJurisdictionLevel Level { get; set; }
    public string? StateCode { get; set; }
    public bool HasReciprocalAgreement { get; set; }
    public string? ReciprocalWithState { get; set; }
    public decimal? LocalRate { get; set; }
    public int? FilingFrequency { get; set; }
}

public class TaxJurisdictionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? StateCode { get; set; }
    public bool HasReciprocalAgreement { get; set; }
    public string? ReciprocalWithState { get; set; }
    public decimal? LocalRate { get; set; }
    public int? FilingFrequency { get; set; }
}

public class CreateEmployeeTaxProfileRequest
{
    public Guid CompanyId { get; set; }
    public string? ResidentState { get; set; }
    public string? WorkState { get; set; }
    public decimal AdditionalFederalWithholding { get; set; }
    public decimal AdditionalStateWithholding { get; set; }
    public bool ExemptFederal { get; set; }
    public bool ExemptState { get; set; }
}

public class EmployeeTaxProfileDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? ResidentState { get; set; }
    public string? WorkState { get; set; }
    public decimal AdditionalFederalWithholding { get; set; }
    public decimal AdditionalStateWithholding { get; set; }
    public bool ExemptFederal { get; set; }
    public bool ExemptState { get; set; }
}
