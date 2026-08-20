// <copyright file="PayrollReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

/// <summary>Read-only payroll reporting endpoints: register, summary, labor distribution,
/// garnishment register, wage-base report, PTO report, direct-deposit register, 940 worksheet.</summary>
[ApiController]
[Route("api/v1/payroll")]
public class PayrollReportsController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollReportsController(PayrollDbContext context)
    {
        _context = context;
    }

    // --- Payroll register: every posted run with its line totals ---
    [HttpGet("reports/payroll-register")]
    public async Task<ActionResult<ApiResponse<List<PayrollRegisterRowDto>>>> PayrollRegister(
        [FromQuery] Guid companyId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var q = _context.PayrollRuns.Where(r => r.CompanyId == companyId);
        if (year.HasValue) q = q.Where(r => r.PayDate.Year == year.Value);
        var rows = await q
            .OrderByDescending(r => r.PayDate)
            .Select(r => new PayrollRegisterRowDto
            {
                RunId = r.Id,
                PayDate = r.PayDate,
                PeriodStart = r.PeriodStart,
                PeriodEnd = r.PeriodEnd,
                Status = r.Status.ToString(),
                TotalGross = r.TotalGross,
                TotalEmployeeTax = r.TotalEmployeeTax,
                TotalEmployerTax = r.TotalEmployerTax,
                TotalDeductions = r.TotalDeductions,
                TotalNet = r.TotalNet,
                LineCount = r.Lines.Count,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PayrollRegisterRowDto>>.Success(rows));
    }

    // --- Payroll summary: company-wide YTD totals ---
    [HttpGet("reports/payroll-summary")]
    public async Task<ActionResult<ApiResponse<PayrollSummaryDto>>> PayrollSummary(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var lines = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year)
            .ToListAsync(cancellationToken);

        var summary = new PayrollSummaryDto
        {
            CompanyId = companyId,
            Year = year,
            TotalGross = lines.Sum(l => l.GrossPay),
            TotalEmployeeTax = lines.Sum(l => l.EmployeeTax),
            TotalEmployerTax = lines.Sum(l => l.EmployerTax),
            TotalDeductions = lines.Sum(l => l.Deductions),
            TotalNet = lines.Sum(l => l.NetPay),
            EmployeeCount = lines.Select(l => l.EmployeeId).Distinct().Count(),
        };
        return Ok(ApiResponse<PayrollSummaryDto>.Success(summary));
    }

    // --- Labor distribution: wages by project (from the GL/project cost linkage via run lines) ---
    [HttpGet("reports/labor-distribution")]
    public async Task<ActionResult<ApiResponse<List<LaborDistributionRowDto>>>> LaborDistribution(
        [FromQuery] Guid companyId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        // Labor distribution by employee (dept proxy) — project-level detail would come from
        // Project Accounting cost transactions; here we summarize run gross by employee.
        var lines = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId
                        && (year == null || l.PayrollRun.PayDate.Year == year.Value))
            .GroupBy(l => l.EmployeeId)
            .Select(g => new LaborDistributionRowDto
            {
                EmployeeId = g.Key,
                RegularHours = g.Sum(l => l.RegularHours),
                OvertimeHours = g.Sum(l => l.OvertimeHours),
                GrossWages = g.Sum(l => l.GrossPay),
                EmployerTax = g.Sum(l => l.EmployerTax),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<LaborDistributionRowDto>>.Success(lines));
    }

    // --- Garnishment register: active orders with computed amounts ---
    [HttpGet("reports/garnishment-register")]
    public async Task<ActionResult<ApiResponse<List<GarnishmentRegisterRowDto>>>> GarnishmentRegister(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.Garnishments
            .Where(g => g.CompanyId == companyId && g.IsActive && g.TerminatedOn == null)
            .OrderBy(g => g.EmployeeId)
            .Select(g => new GarnishmentRegisterRowDto
            {
                GarnishmentId = g.Id,
                EmployeeId = g.EmployeeId,
                Type = g.Type.ToString(),
                Priority = g.Priority,
                DisposableIncomePercent = g.DisposableIncomePercent,
                FixedAmount = g.FixedAmount,
                ArrearsWeeks = g.ArrearsWeeks,
                CaseNumber = g.CaseNumber,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<GarnishmentRegisterRowDto>>.Success(rows));
    }

    // --- Wage base report: employees approaching/hitting SS/FUTA caps ---
    [HttpGet("reports/wage-base")]
    public async Task<ActionResult<ApiResponse<List<WageBaseRowDto>>>> WageBaseReport(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var ssLimit = await _context.WageBaseLimits
            .Where(l => l.Type == WageBaseType.SocialSecurity && l.Year == year)
            .Select(l => l.LimitAmount).FirstOrDefaultAsync(cancellationToken);
        var futaLimit = await _context.WageBaseLimits
            .Where(l => l.Type == WageBaseType.Futa && l.Year == year)
            .Select(l => l.LimitAmount).FirstOrDefaultAsync(cancellationToken);
        ssLimit = ssLimit == 0 ? 176100m : ssLimit;
        futaLimit = futaLimit == 0 ? 7000m : futaLimit;

        var ytd = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year)
            .GroupBy(l => l.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Ytd = g.Sum(l => l.GrossPay) })
            .ToListAsync(cancellationToken);

        var rows = ytd.Select(x => new WageBaseRowDto
        {
            EmployeeId = x.EmployeeId,
            YtdWages = x.Ytd,
            SsRemaining = Math.Max(0m, ssLimit - x.Ytd),
            SsPct = Math.Round(x.Ytd / ssLimit * 100m, 1),
            FutaRemaining = Math.Max(0m, futaLimit - x.Ytd),
            FutaMet = x.Ytd >= futaLimit,
        }).ToList();
        return Ok(ApiResponse<List<WageBaseRowDto>>.Success(rows));
    }

    // --- PTO report ---
    [HttpGet("reports/pto")]
    public async Task<ActionResult<ApiResponse<List<PtoReportRowDto>>>> PtoReport(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.PtoLedgers
            .Select(l => new PtoReportRowDto
            {
                LedgerId = l.Id,
                EmployeeId = l.EmployeeId,
                PolicyName = l.PolicyName,
                Accrued = l.Accrued,
                Used = l.Used,
                Available = l.Available,
                Carryover = l.Carryover,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PtoReportRowDto>>.Success(rows));
    }

    // --- Direct deposit register: PayrollCheck rows flagged direct deposit (per run) ---
    [HttpGet("reports/direct-deposit")]
    public async Task<ActionResult<ApiResponse<List<DirectDepositRowDto>>>> DirectDepositRegister(
        [FromQuery] Guid companyId, [FromQuery] Guid? runId, CancellationToken cancellationToken)
    {
        var q = _context.PayrollChecks.Where(c => c.IsDirectDeposit);
        if (runId.HasValue) q = q.Where(c => c.PayrollRunId == runId.Value);
        var rows = await q
            .Select(c => new DirectDepositRowDto
            {
                CheckId = c.Id,
                PayrollRunId = c.PayrollRunId,
                EmployeeId = c.EmployeeId,
                NetPay = c.NetPay,
                CheckNumber = c.CheckNumber,
                CheckDate = c.CheckDate,
                AchTraceNumber = c.AchTraceNumber,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<DirectDepositRowDto>>.Success(rows));
    }

    // --- Form 940 annual worksheet (FUTA) ---
    [HttpGet("reports/form-940")]
    public async Task<ActionResult<ApiResponse<Form940Dto>>> Form940(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var futaLimit = await _context.WageBaseLimits
            .Where(l => l.Type == WageBaseType.Futa && l.Year == year)
            .Select(l => l.LimitAmount).FirstOrDefaultAsync(cancellationToken);
        futaLimit = futaLimit == 0 ? 7000m : futaLimit;

        var ytd = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year)
            .SumAsync(l => l.GrossPay, cancellationToken);

        var futaWages = Math.Min(ytd, futaLimit);
        var futaTax = Math.Round(futaWages * 0.006m, 2); // 0.6% net FUTA after 5.4% credit
        return Ok(ApiResponse<Form940Dto>.Success(new Form940Dto
        {
            CompanyId = companyId,
            Year = year,
            TotalWages = ytd,
            FutaWages = futaWages,
            FutaTax = futaTax,
        }));
    }
}

// DTOs
public class PayrollRegisterRowDto
{
    public Guid RunId { get; set; }
    public DateTime PayDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalGross { get; set; }
    public decimal TotalEmployeeTax { get; set; }
    public decimal TotalEmployerTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
    public int LineCount { get; set; }
}

public class PayrollSummaryDto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalEmployeeTax { get; set; }
    public decimal TotalEmployerTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
    public int EmployeeCount { get; set; }
}

public class LaborDistributionRowDto
{
    public Guid EmployeeId { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal GrossWages { get; set; }
    public decimal EmployerTax { get; set; }
}

public class GarnishmentRegisterRowDto
{
    public Guid GarnishmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Priority { get; set; }
    public decimal DisposableIncomePercent { get; set; }
    public decimal? FixedAmount { get; set; }
    public int? ArrearsWeeks { get; set; }
    public string? CaseNumber { get; set; }
}

public class WageBaseRowDto
{
    public Guid EmployeeId { get; set; }
    public decimal YtdWages { get; set; }
    public decimal SsRemaining { get; set; }
    public decimal SsPct { get; set; }
    public decimal FutaRemaining { get; set; }
    public bool FutaMet { get; set; }
}

public class PtoReportRowDto
{
    public Guid LedgerId { get; set; }
    public Guid EmployeeId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal Accrued { get; set; }
    public decimal Used { get; set; }
    public decimal Available { get; set; }
    public decimal Carryover { get; set; }
}

public class DirectDepositRowDto
{
    public Guid CheckId { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal NetPay { get; set; }
    public string CheckNumber { get; set; } = string.Empty;
    public DateTime CheckDate { get; set; }
    public string? AchTraceNumber { get; set; }
}

public class Form940Dto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public decimal TotalWages { get; set; }
    public decimal FutaWages { get; set; }
    public decimal FutaTax { get; set; }
}
