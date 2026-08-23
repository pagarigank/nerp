// <copyright file="PayrollTaxFilingExportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Application;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll")]
public class PayrollTaxFilingExportController : ControllerBase
{
    private readonly PayrollDbContext _context;
    private readonly PlatformDbContext _platformContext;

    public PayrollTaxFilingExportController(PayrollDbContext context, PlatformDbContext platformContext)
    {
        _context = context;
        _platformContext = platformContext;
    }

    [HttpGet("tax-filing-export/941")]
    public async Task<ActionResult<ApiResponse<string>>> Export941(
        [FromQuery] Guid companyId, [FromQuery] int year, [FromQuery] int quarter, CancellationToken cancellationToken)
    {
        if (quarter is < 1 or > 4)
            return BadRequest(ApiResponse.Failure(new[] { "Quarter must be between 1 and 4." }));

        var (quarterStart, quarterEnd) = QuarterBounds(year, quarter);
        var runs = await PostedRunsAsync(companyId, year, cancellationToken);
        var inQuarter = runs.Where(r => r.PayDate >= quarterStart && r.PayDate <= quarterEnd).ToList();

        var wages = Sum(inQuarter, l => l.GrossPay);
        var employeeTax = Sum(inQuarter, l => l.EmployeeTax);
        var employerTax = Sum(inQuarter, l => l.EmployerTax);
        var ssWages = await CappedSocialSecurityWagesAsync(
            companyId, year, quarterStart.AddDays(-1), wages, cancellationToken);
        var ssTaxEmployee = TaxFilingExportBuilder.SplitSocialSecurityTax(employeeTax);
        var medicareTaxEmployee = employeeTax - ssTaxEmployee;
        var employerSs = TaxFilingExportBuilder.SplitSocialSecurityTax(employerTax);
        var employerMedicare = employerTax - employerSs;

        var totals = new Form941Totals(
            year,
            quarter,
            wages,
            0m,
            ssWages,
            ssTaxEmployee,
            wages,
            medicareTaxEmployee,
            employerSs,
            employerMedicare);

        return Ok(ApiResponse<string>.Success(TaxFilingExportBuilder.BuildForm941(
            totals, await EinAsync(companyId, cancellationToken), await EmployerNameAsync(companyId, cancellationToken))));
    }

    [HttpGet("tax-filing-export/940")]
    public async Task<ActionResult<ApiResponse<string>>> Export940(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var runs = await PostedRunsAsync(companyId, year, cancellationToken);
        var totalWages = Sum(runs, l => l.GrossPay);
        var employeeCount = runs.SelectMany(r => r.Lines).Select(l => l.EmployeeId).Distinct().Count();
        var futaLimit = await FutaLimitAsync(year, cancellationToken);
        var futaWages = Math.Min(totalWages, employeeCount * futaLimit);
        var rate = 0.006m;
        var tax = Math.Round(futaWages * rate, 2);

        var totals = new Form940Totals(
            year,
            totalWages,
            totalWages - futaWages,
            futaWages,
            rate,
            tax);

        return Ok(ApiResponse<string>.Success(TaxFilingExportBuilder.BuildForm940(
            totals, await EinAsync(companyId, cancellationToken), await EmployerNameAsync(companyId, cancellationToken))));
    }

    [HttpGet("tax-filing-export/w2")]
    public async Task<ActionResult<ApiResponse<string>>> ExportW2(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var rows = await BuildW2RowsAsync(companyId, year, cancellationToken);
        return Ok(ApiResponse<string>.Success(TaxFilingExportBuilder.BuildW2(
            rows, year, await EinAsync(companyId, cancellationToken), await EmployerNameAsync(companyId, cancellationToken))));
    }

    [HttpGet("tax-filing-export/w3")]
    public async Task<ActionResult<ApiResponse<string>>> ExportW3(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var rows = await BuildW2RowsAsync(companyId, year, cancellationToken);
        return Ok(ApiResponse<string>.Success(TaxFilingExportBuilder.BuildW3(
            rows, year, await EinAsync(companyId, cancellationToken), await EmployerNameAsync(companyId, cancellationToken))));
    }

    [HttpGet("tax-filing-export/state-quarterly")]
    public async Task<ActionResult<ApiResponse<string>>> ExportStateQuarterly(
        [FromQuery] Guid companyId, [FromQuery] int year, [FromQuery] int quarter, CancellationToken cancellationToken)
    {
        if (quarter is < 1 or > 4)
            return BadRequest(ApiResponse.Failure(new[] { "Quarter must be between 1 and 4." }));

        var (quarterStart, quarterEnd) = QuarterBounds(year, quarter);
        var runs = (await PostedRunsAsync(companyId, year, cancellationToken))
            .Where(r => r.PayDate >= quarterStart && r.PayDate <= quarterEnd)
            .ToList();
        var employeeIds = runs.SelectMany(r => r.Lines).Select(l => l.EmployeeId).Distinct().ToList();
        var profiles = await _context.EmployeeTaxProfiles
            .Where(p => p.CompanyId == companyId && employeeIds.Contains(p.EmployeeId))
            .ToDictionaryAsync(p => p.EmployeeId, p => p.WorkState, cancellationToken);

        var byState = new Dictionary<string, (decimal Wages, decimal Withholding)>();
        foreach (var line in runs.SelectMany(r => r.Lines))
        {
            var state = profiles.GetValueOrDefault(line.EmployeeId) ?? "(UNKNOWN)";
            if (!byState.TryGetValue(state, out var current))
                byState[state] = (line.GrossPay, line.EmployeeTax);
            else
                byState[state] = (current.Wages + line.GrossPay, current.Withholding + line.EmployeeTax);
        }

        var sutaBase = await SutaBaseAsync(cancellationToken);
        var rows = byState
            .Select(kv => new StateQuarterlyRow(
                kv.Key,
                kv.Value.Wages,
                Math.Max(0m, kv.Value.Wages - sutaBase),
                kv.Value.Withholding))
            .ToList();

        return Ok(ApiResponse<string>.Success(TaxFilingExportBuilder.BuildStateQuarterly(
            rows, year, quarter, "STATE DEPARTMENT OF REVENUE")));
    }

    private async Task<List<PayrollRun>> PostedRunsAsync(Guid companyId, int year, CancellationToken cancellationToken)
    {
        return await _context.PayrollRuns
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId && r.Status == PayrollRunStatus.Posted && r.PayDate.Year == year)
            .OrderBy(r => r.PayDate)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<W2Row>> BuildW2RowsAsync(
        Guid companyId, int year, CancellationToken cancellationToken)
    {
        var runs = await PostedRunsAsync(companyId, year, cancellationToken);
        var lines = runs.SelectMany(r => r.Lines).ToList();
        var employeeIds = lines.Select(l => l.EmployeeId).Distinct().ToList();
        var employees = await _context.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(
                e => e.Id,
                e => new { e.EmployeeCode, Name = e.FullName, e.SsnEncrypted, e.StateCode },
                cancellationToken);
        var profiles = await _context.EmployeeTaxProfiles
            .Where(p => p.CompanyId == companyId && employeeIds.Contains(p.EmployeeId))
            .ToDictionaryAsync(p => p.EmployeeId, p => p.WorkState, cancellationToken);
        var ssCap = await SocialSecurityLimitAsync(year, cancellationToken);

        var rows = new List<W2Row>();
        foreach (var group in lines.GroupBy(l => l.EmployeeId))
        {
            var employee = employees.GetValueOrDefault(group.Key);
            var gross = group.Sum(l => l.GrossPay);
            var deductions = group.Sum(l => l.Deductions);
            var employeeTax = group.Sum(l => l.EmployeeTax);
            var box1 = Math.Max(0m, gross - deductions);
            var ssWages = Math.Min(gross, ssCap);
            var ssTax = TaxFilingExportBuilder.SplitSocialSecurityTax(Math.Min(employeeTax, ssCap * TaxFilingExportBuilder.CombinedFicaRate));
            var medicareTax = employeeTax - ssTax;
            var state = profiles.GetValueOrDefault(group.Key)
                        ?? employee?.StateCode
                        ?? "(UNKNOWN)";

            rows.Add(new W2Row(
                employee?.EmployeeCode ?? group.Key.ToString("N"),
                employee?.Name ?? string.Empty,
                MaskSsn(employee?.SsnEncrypted),
                box1,
                0m,
                ssWages,
                ssTax,
                gross,
                medicareTax,
                state,
                box1));
        }

        return rows;
    }

    private async Task<decimal> CappedSocialSecurityWagesAsync(
        Guid companyId, int year, DateTime throughDateExclusive, decimal quarterWages, CancellationToken cancellationToken)
    {
        var priorWages = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null
                        && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId
                        && l.PayrollRun.PayDate.Year == year
                        && l.PayrollRun.PayDate <= throughDateExclusive)
            .SumAsync(l => (decimal?)l.GrossPay, cancellationToken) ?? 0m;
        var cap = await SocialSecurityLimitAsync(year, cancellationToken);
        var remaining = cap - priorWages;
        if (remaining <= 0m)
            return 0m;
        return Math.Min(remaining, quarterWages);
    }

    private async Task<decimal> SocialSecurityLimitAsync(int year, CancellationToken cancellationToken)
    {
        var limit = await _context.WageBaseLimits
            .Where(l => l.Type == WageBaseType.SocialSecurity && l.Year == year)
            .Select(l => (decimal?)l.LimitAmount)
            .FirstOrDefaultAsync(cancellationToken);
        return limit ?? 176100m;
    }

    private async Task<decimal> FutaLimitAsync(int year, CancellationToken cancellationToken)
    {
        var limit = await _context.WageBaseLimits
            .Where(l => l.Type == WageBaseType.Futa && l.Year == year)
            .Select(l => (decimal?)l.LimitAmount)
            .FirstOrDefaultAsync(cancellationToken);
        return limit ?? 7000m;
    }

    private async Task<decimal> SutaBaseAsync(CancellationToken cancellationToken)
    {
        var latest = await _context.WageBaseLimits
            .Where(l => l.Type == WageBaseType.Suta)
            .OrderByDescending(l => l.Year)
            .Select(l => (decimal?)l.LimitAmount)
            .FirstOrDefaultAsync(cancellationToken);
        return latest ?? 9000m;
    }

    private async Task<string> EinAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var ein = await _context.CompanyPayrollSetups
            .Where(s => s.CompanyId == companyId)
            .Select(s => s.Ein)
            .FirstOrDefaultAsync(cancellationToken);
        return ein ?? string.Empty;
    }

    private async Task<string> EmployerNameAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var name = await _platformContext.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);
        return name ?? string.Empty;
    }

    private static string MaskSsn(string? ssnEncrypted)
    {
        if (string.IsNullOrWhiteSpace(ssnEncrypted))
            return string.Empty;
        return ssnEncrypted.Length > 4 ? $"***-**-{ssnEncrypted[^4..]}" : "***-**-****";
    }

    private static decimal Sum(IEnumerable<PayrollRun> runs, Func<PayrollRunLine, decimal> selector) =>
        runs.SelectMany(r => r.Lines).Sum(selector);

    private static (DateTime Start, DateTime End) QuarterBounds(int year, int quarter)
    {
        var startMonth = (quarter * 3) - 2;
        var start = new DateTime(year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(3).AddDays(-1);
        return (start, end);
    }
}
