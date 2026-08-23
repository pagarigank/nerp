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
            .ApplyCompanyScope(HttpContext, e => e.CompanyId)
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

    [HttpPut("employees/{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateEmployee(
        Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (emp is null)
            return NotFound(ApiResponse<string>.Failure(["Employee not found."]));

        emp.Update(
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            defaultProjectId: null,
            defaultRole: null,
            allocationPercentage: null,
            isBillable: null);

        if (request.Status.HasValue)
        {
            var status = (EmployeeStatus)request.Status.Value;
            if (status == EmployeeStatus.Active) emp.Reactivate();
            else if (status == EmployeeStatus.Terminated) emp.Terminate(DateTime.UtcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Employee updated."));
    }

    // --- Employee direct-deposit accounts ---
    [HttpPost("employees/{employeeId:guid}/direct-deposits")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateDirectDeposit(
        Guid employeeId, [FromBody] CreateDirectDepositRequest request, CancellationToken cancellationToken)
    {
        var dd = new DirectDeposit(request.CompanyId, employeeId, request.BankName, request.RoutingNumber,
            request.AccountNumberEncrypted, request.AccountType, request.AllocationPercentage, request.FixedAmount, request.IsRemainder);
        _context.DirectDeposits.Add(dd);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(dd.Id));
    }

    [HttpPut("direct-deposits/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateDirectDeposit(
        Guid id, [FromBody] CreateDirectDepositRequest request, CancellationToken cancellationToken)
    {
        var dd = await _context.DirectDeposits.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dd is null)
            return NotFound(ApiResponse.Failure(new[] { "Direct deposit not found." }, 404));
        dd.Update(request.BankName, request.RoutingNumber, request.AccountNumberEncrypted, request.AccountType,
            request.AllocationPercentage, request.FixedAmount, request.IsRemainder);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpGet("employees/{employeeId:guid}/direct-deposits")]
    public async Task<ActionResult<ApiResponse<List<DirectDepositDto>>>> GetDirectDeposits(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var rows = await _context.DirectDeposits
            .Where(d => d.EmployeeId == employeeId)
            .Select(d => new { d.Id, d.BankName, d.RoutingNumber, d.AccountType, d.AllocationPercentage, d.FixedAmount, d.IsRemainder, d.AccountNumberEncrypted, d.PrenoteSentOn, d.VerifiedOn })
            .ToListAsync(cancellationToken);

        var dtos = rows.Select(d => new DirectDepositDto
        {
            Id = d.Id,
            BankName = d.BankName,
            RoutingNumber = d.RoutingNumber,
            AccountType = d.AccountType,
            AllocationPercentage = d.AllocationPercentage,
            FixedAmount = d.FixedAmount,
            IsRemainder = d.IsRemainder,
            PrenoteSentOn = d.PrenoteSentOn,
            VerifiedOn = d.VerifiedOn,
            // Account number masked; full value is PII stored encrypted.
            MaskedAccount = "****" + (d.AccountNumberEncrypted.Length > 4 ? d.AccountNumberEncrypted[^4..] : d.AccountNumberEncrypted),
        }).ToList();
        return Ok(ApiResponse<List<DirectDepositDto>>.Success(dtos));
    }

    [HttpDelete("direct-deposits/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteDirectDeposit(Guid id, CancellationToken cancellationToken)
    {
        var dd = await _context.DirectDeposits.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dd is null)
            return NotFound(ApiResponse.Failure(new[] { "Direct deposit not found." }, 404));
        _context.DirectDeposits.Remove(dd);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("employees/{employeeId:guid}/direct-deposits/{id:guid}/send-prenote")]
    public async Task<ActionResult<ApiResponse>> SendPrenote(
        Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var dd = await _context.DirectDeposits.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dd is null || dd.EmployeeId != employeeId)
            return NotFound(ApiResponse.Failure(new[] { "Direct deposit not found." }, 404));
        try
        {
            dd.SendPrenote();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("employees/{employeeId:guid}/direct-deposits/{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse>> VerifyDirectDeposit(
        Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var dd = await _context.DirectDeposits.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dd is null || dd.EmployeeId != employeeId)
            return NotFound(ApiResponse.Failure(new[] { "Direct deposit not found." }, 404));
        try
        {
            dd.Verify();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
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

    [HttpPut("pay-codes/{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdatePayCode(
        Guid id, [FromBody] UpdatePayCodeRequest request, CancellationToken cancellationToken)
    {
        var payCode = await _context.PayCodes.FindAsync(new object[] { id }, cancellationToken);
        if (payCode is null)
            return NotFound(ApiResponse<string>.Failure(["Pay code not found."]));

        payCode.Update(
            description: request.Description,
            glAccountNumber: request.GlAccountNumber,
            isOvertime: null,
            countsAsHoursWorked: null);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Pay code updated."));
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

public class CreateDirectDepositRequest
{
    public Guid CompanyId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    /// <summary>Account number; callers should send the encrypted/masked value (PII).</summary>
    public string AccountNumberEncrypted { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal? AllocationPercentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public bool IsRemainder { get; set; }
}

public class DirectDepositDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal? AllocationPercentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public bool IsRemainder { get; set; }
    public string MaskedAccount { get; set; } = string.Empty;
    public DateTimeOffset? PrenoteSentOn { get; set; }
    public DateTimeOffset? VerifiedOn { get; set; }
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

public class UpdateEmployeeRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public int? Status { get; set; }
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

public class UpdatePayCodeRequest
{
    public string? Description { get; set; }
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
