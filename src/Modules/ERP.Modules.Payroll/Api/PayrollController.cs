// <copyright file="PayrollController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll")]
public class PayrollController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollController(PayrollDbContext context)
    {
        _context = context;
    }

    // --- Employee master ---
    [HttpPost("employees")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = new Employee(
            request.CompanyId, request.EmployeeCode, request.FirstName, request.LastName,
            request.EmploymentType, request.HireDate);
        employee.Update(request.FirstName, request.LastName, request.Email, request.DefaultProjectId, request.DefaultRole, request.AllocationPercentage, request.IsBillable);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(employee.Id));
    }

    [HttpGet("employees")]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetEmployees(CancellationToken cancellationToken)
    {
        var list = await _context.Employees
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                EmploymentType = e.EmploymentType.ToString(),
                Status = e.Status.ToString(),
                Email = e.Email,
                DefaultProjectId = e.DefaultProjectId,
                IsBillable = e.IsBillable,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<EmployeeDto>>.Success(list));
    }

    // --- Pay code master ---
    [HttpPost("pay-codes")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePayCode(
        [FromBody] CreatePayCodeRequest request, CancellationToken cancellationToken)
    {
        var payCode = new PayCode(request.CompanyId, request.Code, request.Description, request.Type, request.GlAccountNumber);
        _context.PayCodes.Add(payCode);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(payCode.Id));
    }

    [HttpGet("pay-codes")]
    public async Task<ActionResult<ApiResponse<List<PayCodeDto>>>> GetPayCodes(CancellationToken cancellationToken)
    {
        var list = await _context.PayCodes
            .Select(p => new PayCodeDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                Type = p.Type.ToString(),
                GlAccountNumber = p.GlAccountNumber,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PayCodeDto>>.Success(list));
    }

    // --- Union / certified-payroll profile (prevailing wage + fringe) ---
    [HttpPost("union-profiles")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateUnionProfile(
        [FromBody] CreateUnionProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = new UnionCertifiedProfile(
            request.CompanyId, request.TradeClassification, request.Jurisdiction,
            request.PrevailingWageRate, request.FringeBenefitRate, request.UnionLocal);
        _context.UnionCertifiedProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(profile.Id));
    }

    [HttpGet("union-profiles")]
    public async Task<ActionResult<ApiResponse<List<UnionProfileDto>>>> GetUnionProfiles(CancellationToken cancellationToken)
    {
        var list = await _context.UnionCertifiedProfiles
            .Select(p => new UnionProfileDto
            {
                Id = p.Id,
                TradeClassification = p.TradeClassification,
                Jurisdiction = p.Jurisdiction,
                PrevailingWageRate = p.PrevailingWageRate,
                FringeBenefitRate = p.FringeBenefitRate,
                TotalPrevailingRate = p.TotalPrevailingRate,
                UnionLocal = p.UnionLocal,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<UnionProfileDto>>.Success(list));
    }

    /// <summary>Prevailing-wage validation (closes Phase 10 gaps 823/1001): checks an actual wage
    /// against the prevailing rate for the trade/jurisdiction; returns whether it meets the requirement.</summary>
    [HttpGet("union-profiles/validate")]
    public async Task<ActionResult<ApiResponse<PrevailingWageValidationDto>>> ValidatePrevailingWage(
        [FromQuery] string tradeClassification,
        [FromQuery] string? jurisdiction,
        [FromQuery] decimal actualWage,
        CancellationToken cancellationToken)
    {
        var profile = await _context.UnionCertifiedProfiles
            .Where(p => p.TradeClassification == tradeClassification &&
                        (jurisdiction == null || p.Jurisdiction == jurisdiction))
            .OrderByDescending(p => p.PrevailingWageRate)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return Ok(ApiResponse<PrevailingWageValidationDto>.Success(new PrevailingWageValidationDto
            {
                TradeClassification = tradeClassification,
                Jurisdiction = jurisdiction,
                Found = false,
                MeetsRate = true,
            }));

        return Ok(ApiResponse<PrevailingWageValidationDto>.Success(new PrevailingWageValidationDto
        {
            TradeClassification = tradeClassification,
            Jurisdiction = jurisdiction,
            Found = true,
            PrevailingWageRate = profile.PrevailingWageRate,
            FringeBenefitRate = profile.FringeBenefitRate,
            TotalPrevailingRate = profile.TotalPrevailingRate,
            ActualWage = actualWage,
            MeetsRate = profile.MeetsPrevailingWage(actualWage),
        }));
    }
}

public class CreateEmployeeRequest
{
    public Guid CompanyId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public DateTime HireDate { get; set; }
    public string? Email { get; set; }
    public Guid? DefaultProjectId { get; set; }
    public string? DefaultRole { get; set; }
    public decimal AllocationPercentage { get; set; } = 100m;
    public bool IsBillable { get; set; } = true;
}

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid? DefaultProjectId { get; set; }
    public bool IsBillable { get; set; }
}

public class CreatePayCodeRequest
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PayCodeType Type { get; set; }
    public string? GlAccountNumber { get; set; }
}

public class PayCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? GlAccountNumber { get; set; }
}

public class CreateUnionProfileRequest
{
    public Guid CompanyId { get; set; }
    public string TradeClassification { get; set; } = string.Empty;
    public string? Jurisdiction { get; set; }
    public decimal PrevailingWageRate { get; set; }
    public decimal FringeBenefitRate { get; set; }
    public string? UnionLocal { get; set; }
}

public class UnionProfileDto
{
    public Guid Id { get; set; }
    public string TradeClassification { get; set; } = string.Empty;
    public string? Jurisdiction { get; set; }
    public decimal PrevailingWageRate { get; set; }
    public decimal FringeBenefitRate { get; set; }
    public decimal TotalPrevailingRate { get; set; }
    public string? UnionLocal { get; set; }
}

public class PrevailingWageValidationDto
{
    public string? TradeClassification { get; set; }
    public string? Jurisdiction { get; set; }
    public bool Found { get; set; }
    public decimal PrevailingWageRate { get; set; }
    public decimal FringeBenefitRate { get; set; }
    public decimal TotalPrevailingRate { get; set; }
    public decimal ActualWage { get; set; }
    public bool MeetsRate { get; set; }
}
