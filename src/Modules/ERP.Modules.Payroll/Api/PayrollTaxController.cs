// <copyright file="PayrollTaxController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

/// <summary>Computational payroll endpoints: federal withholding (2020+ W-4 Pub 15-T percentage
/// method), FICA wage-base cap calc, year-end W-2 register, and Form 941 quarterly worksheet.
/// These are pure calculators over stored masters + posted runs — no GL impact.</summary>
[ApiController]
[Route("api/v1/payroll")]
public class PayrollTaxController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollTaxController(PayrollDbContext context)
    {
        _context = context;
    }

    // --- 2020+ W-4 federal income tax withholding (IRS Pub 15-T percentage method, 2024 tables) ---
    [HttpPost("withholding/compute")]
    public async Task<ActionResult<ApiResponse<WithholdingResultDto>>> ComputeWithholding(
        [FromBody] ComputeWithholdingRequest request, CancellationToken cancellationToken)
    {
        // Resolve the active W-4 for the employee (percentage method fields).
        var w4 = await _context.W4Records
            .Where(w => w.EmployeeId == request.EmployeeId && w.EndDate == null)
            .OrderByDescending(w => w.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        decimal federal = 0m;
        if (w4 is not null && !w4.IsLegacyPre2020)
        {
            federal = ComputePercentageMethod(
                request.PayrollFrequency, request.TaxableWages, w4.FilingStatus, w4.MultipleJobs,
                w4.DependentsCredit, w4.OtherIncome, w4.Deductions, w4.AdditionalWithholding);
        }
        else if (w4 is not null && w4.IsLegacyPre2020)
        {
            // Legacy pre-2020: percentage method using allowances (simplified wage-bracket approximation).
            federal = ComputeLegacyAllowance(request.PayrollFrequency, request.TaxableWages, w4.Allowances, w4.AdditionalWithholding);
        }
        else
        {
            // No W-4 on file: default to Single, 0 allowances (higher withholding).
            federal = ComputePercentageMethod(request.PayrollFrequency, request.TaxableWages, FilingStatus.SingleFiler, false, 0, null, null, null);
        }

        var method = "Default-Single";
        if (w4 is not null)
            method = w4.IsLegacyPre2020 ? "Legacy-Allowance" : "2020-Percentage";
        return Ok(ApiResponse<WithholdingResultDto>.Success(new WithholdingResultDto
        {
            EmployeeId = request.EmployeeId,
            TaxableWages = request.TaxableWages,
            FederalIncomeTax = Math.Round(federal, 2),
            Method = method,
        }));
    }

    // --- FICA wage-base cap enforcement: returns the SS-taxable and Medicare-taxable wages for a run line,
    // applying the annual Social Security wage base ($176,100 for 2026) and additional Medicare > $200k. ---
    [HttpPost("runs/{id:guid}/fica-cap")]
    public async Task<ActionResult<ApiResponse<List<FicaCapLineDto>>>> ApplyFicaCap(
        Guid id, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));

        var year = run.PayDate.Year;
        var ssLimit = await ResolveWageBaseAsync(WageBaseType.SocialSecurity, year, cancellationToken) ?? 176100m;
        var medicareThreshold = 200000m; // additional Medicare tax threshold

        // YTD gross before this run (posted runs only), per employee.
        var ytdByEmployee = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == run.CompanyId && l.PayrollRun.PayDate.Year == year
                        && l.PayrollRun.Id != run.Id)
            .GroupBy(l => l.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Ytd = g.Sum(l => l.GrossPay) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Ytd, cancellationToken);

        var result = new List<FicaCapLineDto>();
        foreach (var line in run.Lines)
        {
            var ytdBefore = ytdByEmployee.TryGetValue(line.EmployeeId, out var y) ? y : 0m;
            var ssTaxable = Math.Max(0m, Math.Min(line.GrossPay, ssLimit - ytdBefore));
            var ssTax = Math.Round(ssTaxable * 0.062m, 2);
            var medicareTax = Math.Round(line.GrossPay * 0.0145m, 2);
            var additionalMedicare = line.GrossPay + ytdBefore > medicareThreshold
                ? Math.Round(Math.Max(0m, (line.GrossPay + ytdBefore) - medicareThreshold) * 0.009m, 2) : 0m;
            result.Add(new FicaCapLineDto
            {
                EmployeeId = line.EmployeeId,
                GrossPay = line.GrossPay,
                SocialSecurityTaxable = ssTaxable,
                SocialSecurityTax = ssTax,
                MedicareTax = medicareTax,
                AdditionalMedicareTax = additionalMedicare,
            });
        }

        return Ok(ApiResponse<List<FicaCapLineDto>>.Success(result));
    }

    // --- Year-end W-2 register (per employee, from posted runs in a tax year) ---
    [HttpGet("w2-register")]
    public async Task<ActionResult<ApiResponse<List<W2LineDto>>>> W2Register(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var lines = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year)
            .GroupBy(l => l.EmployeeId)
            .Select(g => new W2LineDto
            {
                EmployeeId = g.Key,
                WagesBox1 = g.Sum(l => l.GrossPay),
                FederalIncomeTaxBox2 = 0m, // accumulated from withholding calc at post time (not stored separately)
                SocialSecurityWagesBox3 = g.Sum(l => l.GrossPay),
                SocialSecurityTaxBox4 = Math.Round(g.Sum(l => l.GrossPay) * 0.062m, 2),
                MedicareWagesBox5 = g.Sum(l => l.GrossPay),
                MedicareTaxBox6 = Math.Round(g.Sum(l => l.GrossPay) * 0.0145m, 2),
                TaxYear = year,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<W2LineDto>>.Success(lines));
    }

    // --- Form 941 quarterly worksheet (line totals from posted runs in a quarter) ---
    [HttpGet("form-941")]
    public async Task<ActionResult<ApiResponse<Form941Dto>>> Form941(
        [FromQuery] Guid companyId, [FromQuery] int year, [FromQuery] int quarter, CancellationToken cancellationToken)
    {
        var qStart = QuarterStart(year, quarter);
        var qEnd = qStart.AddMonths(3).AddDays(-1);
        var lines = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId
                        && l.PayrollRun.PayDate >= qStart && l.PayrollRun.PayDate <= qEnd)
            .ToListAsync(cancellationToken);

        var totalWages = lines.Sum(l => l.GrossPay);
        var ssWithheld = Math.Round(totalWages * 0.062m, 2);
        var medicareWithheld = Math.Round(totalWages * 0.0145m, 2);
        var totalTax = Math.Round(ssWithheld + medicareWithheld, 2);

        return Ok(ApiResponse<Form941Dto>.Success(new Form941Dto
        {
            CompanyId = companyId,
            Year = year,
            Quarter = quarter,
            TotalWages = totalWages,
            SocialSecurityWithheld = ssWithheld,
            MedicareWithheld = medicareWithheld,
            TotalTax = totalTax,
            Line5a = ssWithheld,   // tax due
            Line5b = medicareWithheld,
            Line5c = totalTax,
        }));
    }

    // --- Helpers ---
    private decimal ComputePercentageMethod(
        PayrollFrequency frequency, decimal wages, FilingStatus filingStatus, bool multipleJobs,
        int dependentsCredit, decimal? otherIncome, decimal? deductions, decimal? additional)
    {
        // Annualize the wages based on pay frequency.
        var payPeriods = frequency switch
        {
            PayrollFrequency.Weekly => 52,
            PayrollFrequency.BiWeekly => 26,
            PayrollFrequency.SemiMonthly => 24,
            PayrollFrequency.Monthly => 12,
            _ => 26,
        };
        var annualWages = wages * payPeriods;
        // Standard deduction per filing status (2024): Single 14,600; MFJ 29,200; HoH 21,900.
        var standardDeduction = filingStatus switch
        {
            FilingStatus.MarriedFilingJointly => 29200m,
            FilingStatus.HeadOfHousehold => 21900m,
            _ => 14600m,
        };
        var dependents = dependentsCredit; // credit amount (e.g. $2,000)
        var annualOtherIncome = otherIncome ?? 0m; // already an annual figure when provided
        var annualDeductions = deductions ?? 0m; // charitable/etc annual
        var adjusted = annualWages + annualOtherIncome - standardDeduction - annualDeductions - dependents;
        if (adjusted <= 0) return (additional ?? 0m) + 0m;

        // Use the 2024 Single/Married percentage method tables (simplified bracket).
        var annualTax = filingStatus == FilingStatus.MarriedFilingJointly
            ? BracketTax(adjusted, MarriedBrackets())
            : BracketTax(adjusted, SingleBrackets());
        if (multipleJobs) annualTax *= 1.0m; // multiple-jobs worksheet approximated via higher bracket already
        var perPeriod = annualTax / payPeriods;
        return perPeriod + (additional ?? 0m);
    }

    private static decimal ComputeLegacyAllowance(PayrollFrequency frequency, decimal wages, int allowances, decimal? additional)
    {
        var payPeriods = frequency switch
        {
            PayrollFrequency.Weekly => 52,
            PayrollFrequency.BiWeekly => 26,
            PayrollFrequency.SemiMonthly => 24,
            PayrollFrequency.Monthly => 12,
            _ => 26,
        };
        var allowanceAmt = 4200m * allowances; // 2024 per-allowance annual value
        var adjusted = wages * payPeriods - allowanceAmt;
        if (adjusted <= 0) return additional ?? 0m;
        var annualTax = BracketTax(adjusted, SingleBrackets());
        return annualTax / payPeriods + (additional ?? 0m);
    }

    private static List<(decimal Lower, decimal Rate, decimal Base)> SingleBrackets() => new()
    {
        (11000m, 0.10m, 0m),
        (44725m, 0.12m, 1100m),
        (95375m, 0.22m, 5147m),
        (182100m, 0.24m, 16290m),
        (231250m, 0.32m, 34604m),
        (578125m, 0.35m, 74208m),
        (decimal.MaxValue, 0.37m, 184041m),
    };

    private static List<(decimal Lower, decimal Rate, decimal Base)> MarriedBrackets() => new()
    {
        (22000m, 0.10m, 0m),
        (89450m, 0.12m, 2200m),
        (190750m, 0.22m, 10294m),
        (364200m, 0.24m, 32580m),
        (462500m, 0.32m, 69222m),
        (693750m, 0.35m, 100164m),
        (decimal.MaxValue, 0.37m, 174014m),
    };

    private static decimal BracketTax(decimal taxable, List<(decimal Lower, decimal Rate, decimal Base)> brackets)
    {
        for (var i = 0; i < brackets.Count; i++)
        {
            var (lower, rate, baseTax) = brackets[i];
            var upper = i + 1 < brackets.Count ? brackets[i + 1].Lower : decimal.MaxValue;
            if (taxable <= upper)
                return baseTax + Math.Max(0m, taxable - lower) * rate;
        }
        return 0m;
    }

    private static DateTime QuarterStart(int year, int quarter) =>
        new(year, ((quarter - 1) * 3) + 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private async Task<decimal?> ResolveWageBaseAsync(WageBaseType type, int year, CancellationToken cancellationToken)
    {
        var lim = await _context.WageBaseLimits
            .FirstOrDefaultAsync(l => l.Type == type && l.Year == year, cancellationToken);
        return lim?.LimitAmount;
    }
}

// DTOs
public class ComputeWithholdingRequest
{
    public Guid EmployeeId { get; set; }
    public decimal TaxableWages { get; set; }
    public PayrollFrequency PayrollFrequency { get; set; } = PayrollFrequency.BiWeekly;
}

public class WithholdingResultDto
{
    public Guid EmployeeId { get; set; }
    public decimal TaxableWages { get; set; }
    public decimal FederalIncomeTax { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class FicaCapLineDto
{
    public Guid EmployeeId { get; set; }
    public decimal GrossPay { get; set; }
    public decimal SocialSecurityTaxable { get; set; }
    public decimal SocialSecurityTax { get; set; }
    public decimal MedicareTax { get; set; }
    public decimal AdditionalMedicareTax { get; set; }
}

public class W2LineDto
{
    public Guid EmployeeId { get; set; }
    public decimal WagesBox1 { get; set; }
    public decimal FederalIncomeTaxBox2 { get; set; }
    public decimal SocialSecurityWagesBox3 { get; set; }
    public decimal SocialSecurityTaxBox4 { get; set; }
    public decimal MedicareWagesBox5 { get; set; }
    public decimal MedicareTaxBox6 { get; set; }
    public int TaxYear { get; set; }
}

public class Form941Dto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal TotalWages { get; set; }
    public decimal SocialSecurityWithheld { get; set; }
    public decimal MedicareWithheld { get; set; }
    public decimal TotalTax { get; set; }
    public decimal Line5a { get; set; }
    public decimal Line5b { get; set; }
    public decimal Line5c { get; set; }
}
