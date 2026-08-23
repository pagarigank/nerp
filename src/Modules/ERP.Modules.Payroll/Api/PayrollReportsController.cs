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
/// garnishment register, wage-base report, PTO report, direct-deposit register, 940 worksheet,
/// plus Batch E: positive-pay, 1099-NEC, multi-state withholding, union, workers-comp, termination,
/// plus Batch F: tax-liability, deduction-register, certified-payroll, time-expense-by-project,
/// employee-earnings, w2-reconciliation, form-941-reconciliation, payroll-accrual, eftps-schedule,
/// ach-return-report, new-hire-report, workers-comp-premium.</summary>
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

    // --- Batch E: Positive pay file (issued checks for bank reconciliation) ---
    [HttpGet("reports/positive-pay")]
    public async Task<ActionResult<ApiResponse<List<PositivePayRowDto>>>> PositivePay(
        [FromQuery] Guid companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var runs = await _context.PayrollRuns.Where(r => r.CompanyId == companyId).Select(r => r.Id).ToListAsync(cancellationToken);
        var q = _context.PayrollChecks.Where(c => runs.Contains(c.PayrollRunId));
        if (from.HasValue) q = q.Where(c => c.CheckDate >= from.Value);
        if (to.HasValue) q = q.Where(c => c.CheckDate <= to.Value);
        var rows = await q.OrderBy(c => c.CheckNumber)
            .Select(c => new PositivePayRowDto
            {
                EmployeeId = c.EmployeeId,
                CheckNumber = c.CheckNumber,
                CheckDate = c.CheckDate,
                Amount = c.NetPay,
                IsDirectDeposit = c.IsDirectDeposit,
                AchTraceNumber = c.AchTraceNumber,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<PositivePayRowDto>>.Success(rows));
    }

    // --- Batch E: 1099-NEC report (contractor wages from 1099-flagged manual checks) ---
    [HttpGet("reports/1099-nec")]
    public async Task<ActionResult<ApiResponse<List<Form1099NecRowDto>>>> Form1099Nec(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var rows = await _context.ManualChecks
            .Where(m => m.CompanyId == companyId && m.Is1099 && m.CheckDate.Year == year)
            .GroupBy(m => m.EmployeeId)
            .Select(g => new Form1099NecRowDto
            {
                RecipientId = g.Key,
                NonemployeeCompensation = g.Sum(m => m.GrossPay),
                FederalIncomeTaxWithheld = g.Sum(m => m.GrossPay - m.NetPay),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<Form1099NecRowDto>>.Success(rows));
    }

    // --- Batch E: Multi-state withholding estimate for an employee (resident + work state) ---
    [HttpGet("reports/multi-state-withholding")]
    public async Task<ActionResult<ApiResponse<MultiStateWithholdingDto>>> MultiStateWithholding(
        [FromQuery] Guid employeeId, [FromQuery] decimal taxableWages, CancellationToken cancellationToken)
    {
        var profile = await _context.EmployeeTaxProfiles.FirstOrDefaultAsync(p => p.EmployeeId == employeeId, cancellationToken);
        var result = new MultiStateWithholdingDto { EmployeeId = employeeId, TaxableWages = taxableWages, States = new List<StateWithholdingDto>() };
        if (profile is null)
            return Ok(ApiResponse<MultiStateWithholdingDto>.Success(result));

        var states = new[] { profile.ResidentState, profile.WorkState }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        foreach (var st in states)
        {
            var table = await _context.TaxTables
                .Where(t => t.StateCode == st && t.Level == TaxJurisdictionLevel.State)
                .OrderByDescending(t => t.Year)
                .FirstOrDefaultAsync(cancellationToken);
            decimal wh;
            if (table is null)
            {
                wh = Math.Round(taxableWages * 0.05m, 2);
            }
            else
            {
                var bracket = table.Brackets
                    .FirstOrDefault(b => taxableWages >= b.LowerBound && (b.UpperBound == null || taxableWages <= b.UpperBound));
                wh = bracket is null
                    ? Math.Round(taxableWages * 0.05m, 2)
                    : Math.Round((bracket.FixedAmount ?? 0m) + (taxableWages * bracket.Rate), 2);
            }

            wh += profile.AdditionalStateWithholding;
            result.States.Add(new StateWithholdingDto
            {
                State = st!,
                StateWithholding = wh,
                Exempt = st == profile.ResidentState && profile.ExemptState,
            });
        }

        result.FederalWithholding = profile.ExemptFederal ? 0m : Math.Round(taxableWages * 0.10m, 2) + profile.AdditionalFederalWithholding;
        return Ok(ApiResponse<MultiStateWithholdingDto>.Success(result));
    }

    // --- Batch E: Union prevailing-wage report (Davis-Bacon) ---
    [HttpGet("reports/union")]
    public async Task<ActionResult<ApiResponse<List<UnionReportRowDto>>>> UnionReport(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.UnionCertifiedProfiles
            .Where(p => p.CompanyId == companyId)
            .Select(p => new UnionReportRowDto
            {
                TradeClassification = p.TradeClassification,
                Jurisdiction = p.Jurisdiction ?? "(any)",
                PrevailingWageRate = p.PrevailingWageRate,
                FringeBenefitRate = p.FringeBenefitRate,
                TotalPrevailingRate = p.TotalPrevailingRate,
                UnionLocal = p.UnionLocal,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<UnionReportRowDto>>.Success(rows));
    }

    // --- Batch E: Workers' compensation premium report (basis by class code) ---
    [HttpGet("reports/workers-comp")]
    public async Task<ActionResult<ApiResponse<List<WorkersCompReportRowDto>>>> WorkersCompReport(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.WorkersCompClassCodes
            .Where(c => c.CompanyId == companyId)
            .Select(c => new WorkersCompReportRowDto
            {
                ClassCode = c.ClassCode,
                Description = c.Description,
                State = c.State,
                RatePer100 = c.RatePer100,
                ExperienceModification = c.ExperienceModification,
                EffectiveRatePer100 = Math.Round(c.RatePer100 * c.ExperienceModification, 4),
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<WorkersCompReportRowDto>>.Success(rows));
    }

    // --- Batch F: tax liability by jurisdiction for a period (deposit-schedule driven) ---
    [HttpGet("reports/tax-liability")]
    public async Task<ActionResult<ApiResponse<TaxLiabilityDto>>> TaxLiability(
        [FromQuery] Guid companyId, [FromQuery] int year, [FromQuery] int? quarter, CancellationToken cancellationToken)
    {
        if (quarter.HasValue && (quarter.Value < 1 || quarter.Value > 4))
            return BadRequest(ApiResponse.Failure(new[] { "Quarter must be between 1 and 4." }));

        var startMonth = quarter.HasValue ? ((quarter.Value - 1) * 3) + 1 : 1;
        var endMonth = quarter.HasValue ? startMonth + 2 : 12;

        var setup = await _context.CompanyPayrollSetups
            .Where(s => s.CompanyId == companyId)
            .Select(s => new { s.DepositSchedule })
            .FirstOrDefaultAsync(cancellationToken);

        var lines = _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year
                        && l.PayrollRun.PayDate.Month >= startMonth && l.PayrollRun.PayDate.Month <= endMonth);
        var employeeTaxWithheld = await lines.SumAsync(l => l.EmployeeTax, cancellationToken);
        var employerTaxAccrued = await lines.SumAsync(l => l.EmployerTax, cancellationToken);

        var deposits = await _context.TaxDepositSchedules
            .Where(d => d.CompanyId == companyId && d.DepositDate.Year == year
                        && d.DepositDate.Month >= startMonth && d.DepositDate.Month <= endMonth)
            .OrderBy(d => d.DepositDate)
            .Select(d => new TaxLiabilityRowDto
            {
                TaxType = d.TaxType,
                Agency = d.Agency,
                FormType = d.FormType,
                DueDate = d.DepositDate,
                AmountOwed = d.EstimatedAmount,
                DepositedAmount = d.DepositedAmount,
                Deposited = d.Deposited,
            }).ToListAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        foreach (var row in deposits)
        {
            row.Jurisdiction = $"{row.TaxType} / {row.Agency}";
            if (row.Deposited) row.Status = "Deposited";
            else if (row.DueDate.Date < today) row.Status = "Missed";
            else row.Status = "Open";
        }

        var nextDue = deposits
            .Where(d => !d.Deposited && d.DueDate.Date >= today)
            .OrderBy(d => d.DueDate)
            .Select(d => d.DueDate)
            .Cast<DateTime?>()
            .FirstOrDefault();

        var dueHint = "Configure CompanyPayrollSetup to record the company depositor status.";
        var depositorStatus = setup is null ? "Unknown" : setup.DepositSchedule;
        if (depositorStatus.Equals("SemiWeekly", StringComparison.OrdinalIgnoreCase))
            dueHint = "Semi-weekly depositor: deposit within 3-5 banking days after each pay date.";
        else if (!depositorStatus.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            dueHint = "Monthly depositor: deposit by the 15th of the month following the liability.";

        return Ok(ApiResponse<TaxLiabilityDto>.Success(new TaxLiabilityDto
        {
            CompanyId = companyId,
            Year = year,
            Quarter = quarter,
            TotalEmployeeTaxWithheld = employeeTaxWithheld,
            TotalEmployerTaxAccrued = employerTaxAccrued,
            DepositorStatus = depositorStatus,
            NextDepositDue = nextDue,
            DueDateHint = dueHint,
            Note = "Run lines store withholding and employer tax as period aggregates; per-jurisdiction amounts come from scheduled EFTPS deposits, not per-tax line detail.",
            Rows = deposits,
        }));
    }

    // --- Batch F: deduction/benefit register active for a period, grouped by type then employee ---
    [HttpGet("reports/deduction-register")]
    public async Task<ActionResult<ApiResponse<DeductionRegisterDto>>> DeductionRegister(
        [FromQuery] Guid companyId, [FromQuery] DateTime? asOf, CancellationToken cancellationToken)
    {
        var on = asOf ?? DateTime.UtcNow;
        var rows = await _context.EmployeeDeductionBenefits
            .Join(
                _context.DeductionBenefits,
                e => e.DeductionBenefitId,
                b => b.Id,
                (e, b) => new { Enrollment = e, Benefit = b })
            .Where(x => x.Benefit.CompanyId == companyId && x.Benefit.IsActive && x.Enrollment.IsActive
                        && (x.Enrollment.StartDate == null || x.Enrollment.StartDate <= on)
                        && (x.Enrollment.EndDate == null || x.Enrollment.EndDate >= on))
            .OrderBy(x => x.Benefit.Type).ThenBy(x => x.Enrollment.EmployeeId)
            .Select(x => new DeductionRegisterRowDto
            {
                BenefitId = x.Benefit.Id,
                BenefitCode = x.Benefit.Code,
                Description = x.Benefit.Description,
                Type = x.Benefit.Type.ToString(),
                IsPreTax = x.Benefit.IsPreTax,
                EmployeeId = x.Enrollment.EmployeeId,
                Amount = x.Enrollment.Amount,
                Percent = x.Enrollment.Percent,
                StartDate = x.Enrollment.StartDate,
                EndDate = x.Enrollment.EndDate,
                GlAccountNumber = x.Benefit.GlAccountNumber,
            }).ToListAsync(cancellationToken);

        var typeTotals = rows
            .GroupBy(r => r.Type)
            .Select(g => new DeductionTypeTotalDto { Type = g.Key, EnrollmentCount = g.Count(), AmountTotal = g.Sum(r => r.Amount) })
            .OrderBy(t => t.Type)
            .ToList();

        return Ok(ApiResponse<DeductionRegisterDto>.Success(new DeductionRegisterDto
        {
            CompanyId = companyId,
            AsOf = on,
            TotalRemittanceDue = rows.Sum(r => r.Amount),
            TypeTotals = typeTotals,
            Note = "Remittance-due-to-vendor equals enrolled per-period amounts; employer-paid match or benefit cost is not tracked separately on enrollments.",
            Rows = rows,
        }));
    }

    // --- Batch F: WH-347-style certified payroll from posted run lines of profiled trades ---
    [HttpGet("reports/certified-payroll")]
    public async Task<ActionResult<ApiResponse<CertifiedPayrollWh347Dto>>> CertifiedPayrollReport(
        [FromQuery] Guid companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var profiles = await _context.UnionCertifiedProfiles
            .Where(p => p.CompanyId == companyId)
            .Select(p => new { p.TradeClassification, p.PrevailingWageRate })
            .ToListAsync(cancellationToken);
        var trades = profiles.Select(p => p.TradeClassification).ToList();

        var q = _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId
                        && l.TradeClassification != null && trades.Contains(l.TradeClassification));
        if (from.HasValue) q = q.Where(l => l.PayrollRun!.PayDate >= from.Value);
        if (to.HasValue) q = q.Where(l => l.PayrollRun!.PayDate <= to.Value);

        var lines = await q.Select(l => new CertifiedPayrollWh347RowDto
        {
            EmployeeId = l.EmployeeId,
            TradeClassification = l.TradeClassification!,
            RegularHours = l.RegularHours,
            OvertimeHours = l.OvertimeHours,
            BaseRate = l.PrevailingWageRate ?? l.RegularRate,
            FringeRate = l.FringeRate,
            Gross = l.GrossPay,
        }).ToListAsync(cancellationToken);

        var empIds = lines.Select(l => l.EmployeeId).Distinct().ToList();
        var employees = await _context.Employees
            .Where(e => empIds.Contains(e.Id))
            .Select(e => new { e.Id, e.EmployeeCode, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id, cancellationToken);
        var topRateByTrade = profiles
            .GroupBy(p => p.TradeClassification)
            .ToDictionary(g => g.Key, g => g.Max(p => p.PrevailingWageRate));

        foreach (var row in lines)
        {
            if (employees.TryGetValue(row.EmployeeId, out var emp))
            {
                row.EmployeeCode = emp.EmployeeCode;
                row.EmployeeName = $"{emp.FirstName} {emp.LastName}".Trim();
            }

            row.FringeCost = Math.Round((row.FringeRate ?? 0m) * (row.RegularHours + row.OvertimeHours), 2);
            row.MeetsPrevailing = !topRateByTrade.TryGetValue(row.TradeClassification, out var rate) || row.BaseRate >= rate;
        }

        return Ok(ApiResponse<CertifiedPayrollWh347Dto>.Success(new CertifiedPayrollWh347Dto
        {
            CompanyId = companyId,
            From = from,
            To = to,
            TotalGross = lines.Sum(l => l.Gross),
            TotalFringe = lines.Sum(l => l.FringeCost),
            Note = "WH-347 style: employees are identified by code/name only (SSN withheld); rows cover posted run labor whose trade has a union certified profile.",
            Rows = lines.OrderBy(l => l.EmployeeCode).ThenBy(l => l.TradeClassification).ToList(),
        }));
    }

    // --- Batch F: timesheet hours/cost plus expense amounts grouped by project ---
    [HttpGet("reports/time-expense-by-project")]
    public async Task<ActionResult<ApiResponse<TimeExpenseByProjectDto>>> TimeExpenseByProject(
        [FromQuery] Guid companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var sheetQuery = _context.Timesheets
            .Where(t => t.CompanyId == companyId && t.Status == TimesheetStatus.Approved);
        if (from.HasValue) sheetQuery = sheetQuery.Where(t => t.WeekEnding >= from.Value);
        if (to.HasValue) sheetQuery = sheetQuery.Where(t => t.WeekEnding <= to.Value);
        var sheetIds = await sheetQuery
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var tsLines = await _context.TimesheetLines
            .Where(l => sheetIds.Contains(l.TimesheetId))
            .Select(l => new { l.ProjectId, l.Hours, l.Rate, l.IsBillable })
            .ToListAsync(cancellationToken);

        var reportQuery = _context.ExpenseReports
            .Where(r => r.CompanyId == companyId
                        && (r.Status == ExpenseReportStatus.Approved || r.Status == ExpenseReportStatus.Reimbursed));
        if (from.HasValue) reportQuery = reportQuery.Where(r => r.ReportDate >= from.Value);
        if (to.HasValue) reportQuery = reportQuery.Where(r => r.ReportDate <= to.Value);
        var reportIds = await reportQuery.Select(r => r.Id).ToListAsync(cancellationToken);

        var expLines = await _context.ExpenseReportLines
            .Where(l => reportIds.Contains(l.ExpenseReportId))
            .Select(l => new { l.ProjectId, l.Amount, l.ClientBillable })
            .ToListAsync(cancellationToken);

        var rows = new List<TimeExpenseProjectRowDto>();
        foreach (var g in tsLines.GroupBy(l => l.ProjectId))
        {
            rows.Add(new TimeExpenseProjectRowDto
            {
                ProjectId = g.Key,
                Hours = g.Sum(l => l.Hours),
                LaborCost = Math.Round(g.Sum(l => l.Hours * l.Rate), 2),
                Expenses = 0m,
                BillableAmount = Math.Round(g.Where(l => l.IsBillable).Sum(l => l.Hours * l.Rate), 2),
            });
        }

        foreach (var g in expLines.GroupBy(l => l.ProjectId))
        {
            var row = rows.FirstOrDefault(r => r.ProjectId == g.Key);
            if (row is null)
            {
                row = new TimeExpenseProjectRowDto { ProjectId = g.Key };
                rows.Add(row);
            }

            row.Expenses += g.Sum(l => l.Amount);
            if (g.Any(l => l.ClientBillable)) row.BillableAmount += g.Where(l => l.ClientBillable).Sum(l => l.Amount);
        }

        return Ok(ApiResponse<TimeExpenseByProjectDto>.Success(new TimeExpenseByProjectDto
        {
            CompanyId = companyId,
            From = from,
            To = to,
            Note = "Hours/labor cost from approved timesheet lines; expenses from approved or reimbursed expense reports. Lines without a project are grouped under a null ProjectId.",
            Rows = rows.OrderBy(r => r.ProjectId == null).ThenBy(r => r.ProjectId).ToList(),
        }));
    }

    // --- Batch F: per-employee YTD earnings from posted runs ---
    [HttpGet("reports/employee-earnings")]
    public async Task<ActionResult<ApiResponse<EmployeeEarningsDto>>> EmployeeEarnings(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var rows = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year)
            .GroupBy(l => l.EmployeeId)
            .Select(g => new EmployeeEarningsRowDto
            {
                EmployeeId = g.Key,
                Gross = g.Sum(l => l.GrossPay),
                EmployeeTax = g.Sum(l => l.EmployeeTax),
                EmployerTax = g.Sum(l => l.EmployerTax),
                Deductions = g.Sum(l => l.Deductions),
                Net = g.Sum(l => l.NetPay),
                RunCount = g.Select(l => l.PayrollRunId).Distinct().Count(),
            }).ToListAsync(cancellationToken);

        var empIds = rows.Select(r => r.EmployeeId).ToList();
        var employees = await _context.Employees
            .Where(e => empIds.Contains(e.Id))
            .Select(e => new { e.Id, e.EmployeeCode, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id, cancellationToken);
        foreach (var row in rows)
        {
            if (employees.TryGetValue(row.EmployeeId, out var emp))
            {
                row.EmployeeCode = emp.EmployeeCode;
                row.EmployeeName = $"{emp.FirstName} {emp.LastName}".Trim();
            }
        }

        return Ok(ApiResponse<EmployeeEarningsDto>.Success(new EmployeeEarningsDto
        {
            CompanyId = companyId,
            Year = year,
            Note = "YTD totals are summed across posted runs in the calendar year. Run lines do not carry a pay-code dimension, so gross cannot be split by pay code here.",
            Rows = rows.OrderBy(r => r.EmployeeCode).ToList(),
        }));
    }

    // --- Batch F: W-2 wage reconciliation vs expected GL wage expense ---
    [HttpGet("reports/w2-reconciliation")]
    public async Task<ActionResult<ApiResponse<W2ReconciliationDto>>> W2Reconciliation(
        [FromQuery] Guid companyId, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var runWages = await _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId && l.PayrollRun.PayDate.Year == year)
            .SumAsync(l => l.GrossPay, cancellationToken);
        var manualWages = await _context.ManualChecks
            .Where(m => m.CompanyId == companyId && !m.Is1099 && m.CheckDate.Year == year)
            .SumAsync(m => m.GrossPay, cancellationToken);
        var w2Wages = runWages + manualWages;

        return Ok(ApiResponse<W2ReconciliationDto>.Success(new W2ReconciliationDto
        {
            CompanyId = companyId,
            Year = year,
            RunWages = runWages,
            ManualCheckWages = manualWages,
            W2Wages = w2Wages,
            ExpectedGlWageExpense = w2Wages,
            Variance = 0m,
            GlTieOutPending = true,
            Assumption = "All posted-run gross plus non-1099 manual checks are treated as W-2 wages; box-1 pre-tax exclusions are not itemized on run lines.",
            Note = "Expected GL figure assumes payroll posting booked wage expense equal to total gross. Cross-module GL comparison is not read here; variance stays a placeholder until tie-out.",
        }));
    }

    // --- Batch F: quarterly Form 941 reconciliation with aggregate-tax honesty flags ---
    [HttpGet("reports/form-941-reconciliation")]
    public async Task<ActionResult<ApiResponse<Form941ReconciliationDto>>> Form941Reconciliation(
        [FromQuery] Guid companyId, [FromQuery] int year, [FromQuery] int quarter, CancellationToken cancellationToken)
    {
        if (quarter < 1 || quarter > 4)
            return BadRequest(ApiResponse.Failure(new[] { "Quarter must be between 1 and 4." }));
        var startMonth = ((quarter - 1) * 3) + 1;
        var endMonth = startMonth + 2;

        var setup = await _context.CompanyPayrollSetups
            .Where(s => s.CompanyId == companyId)
            .Select(s => new { s.SocialSecurityRate, s.MedicareRate })
            .FirstOrDefaultAsync(cancellationToken);
        var ssRate = setup?.SocialSecurityRate ?? 0.062m;
        var medRate = setup?.MedicareRate ?? 0.0145m;

        var q = _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId
                        && l.PayrollRun.PayDate.Year == year
                        && l.PayrollRun.PayDate.Month >= startMonth && l.PayrollRun.PayDate.Month <= endMonth);
        var totalWages = await q.SumAsync(l => l.GrossPay, cancellationToken);
        var employeeTaxTotal = await q.SumAsync(l => l.EmployeeTax, cancellationToken);
        var employerTaxTotal = await q.SumAsync(l => l.EmployerTax, cancellationToken);

        var eeFicaEstimate = Math.Round(totalWages * (ssRate + medRate), 2);
        var fitEstimated = Math.Max(0m, employeeTaxTotal - eeFicaEstimate);
        var erFicaEstimate = Math.Round(totalWages * (ssRate + medRate), 2);
        var employerResidual = Math.Max(0m, employerTaxTotal - erFicaEstimate);

        return Ok(ApiResponse<Form941ReconciliationDto>.Success(new Form941ReconciliationDto
        {
            CompanyId = companyId,
            Year = year,
            Quarter = quarter,
            TotalWages = totalWages,
            FederalIncomeTaxWithheldEstimated = fitEstimated,
            EmployeeFicaEstimated = eeFicaEstimate,
            EmployerFicaEstimated = erFicaEstimate,
            EmployerTaxResidual = employerResidual,
            EmployeeTaxWithheldActual = employeeTaxTotal,
            EmployerTaxActual = employerTaxTotal,
            SocialSecurityRateUsed = ssRate,
            MedicareRateUsed = medRate,
            GlTieOutPending = true,
            Note = "Run lines capture one aggregate employee-tax and one aggregate employer-tax amount per line; FIT and FICA halves above are rate-derived estimates (SS wage-base caps not applied). GL tie-out pending.",
        }));
    }

    // --- Batch F: accrued unpaid payroll (approved timesheets past last posted period end) ---
    [HttpGet("reports/payroll-accrual")]
    public async Task<ActionResult<ApiResponse<PayrollAccrualDto>>> PayrollAccrual(
        [FromQuery] Guid companyId, [FromQuery] DateTime? asOf, CancellationToken cancellationToken)
    {
        var asOfDate = (asOf ?? DateTime.UtcNow).Date;
        var lastPeriodEnd = await _context.PayrollRuns
            .Where(r => r.CompanyId == companyId && r.Status == PayrollRunStatus.Posted)
            .MaxAsync(r => (DateTime?)r.PeriodEnd, cancellationToken);
        var cutoff = lastPeriodEnd ?? DateTime.MinValue;

        var sheets = await _context.Timesheets
            .Where(t => t.CompanyId == companyId && t.Status == TimesheetStatus.Approved
                        && t.WeekEnding > cutoff && t.WeekEnding <= asOfDate)
            .Select(t => new { t.Id, t.EmployeeId })
            .ToListAsync(cancellationToken);
        var sheetEmployeeById = sheets.ToDictionary(s => s.Id, s => s.EmployeeId);
        var sheetIds = sheets.Select(s => s.Id).ToList();

        var tsLines = await _context.TimesheetLines
            .Where(l => sheetIds.Contains(l.TimesheetId))
            .Select(l => new { l.TimesheetId, l.Hours, l.Rate })
            .ToListAsync(cancellationToken);

        var rows = tsLines
            .GroupBy(l => sheetEmployeeById[l.TimesheetId])
            .Select(g => new PayrollAccrualRowDto
            {
                EmployeeId = g.Key,
                Hours = g.Sum(l => l.Hours),
                AccruedWages = Math.Round(g.Sum(l => l.Hours * l.Rate), 2),
            })
            .OrderBy(r => r.EmployeeId)
            .ToList();

        var setup = await _context.CompanyPayrollSetups
            .Where(s => s.CompanyId == companyId)
            .Select(s => new { s.SocialSecurityRate, s.MedicareRate, s.FutaRate, s.SutaRate })
            .FirstOrDefaultAsync(cancellationToken);
        var employerRateSum = setup is null ? 0.0835m : setup.SocialSecurityRate + setup.MedicareRate + setup.FutaRate + setup.SutaRate;
        var accruedWages = rows.Sum(r => r.AccruedWages);

        return Ok(ApiResponse<PayrollAccrualDto>.Success(new PayrollAccrualDto
        {
            CompanyId = companyId,
            AsOf = asOfDate,
            LastPostedPeriodEnd = lastPeriodEnd,
            AccruedWages = accruedWages,
            EmployerTaxAccrualEstimate = Math.Round(accruedWages * employerRateSum, 2),
            EmployerTaxRateUsed = employerRateSum,
            Note = "Assumes every approved timesheet week after the latest posted run's period end is earned but unpaid; draft runs are ignored and no per-timesheet payment link exists.",
            Rows = rows,
        }));
    }

    // --- Batch F: EFTPS deposit schedule status (upcoming/missed/deposited by depositor type) ---
    [HttpGet("reports/eftps-schedule")]
    public async Task<ActionResult<ApiResponse<EftpsScheduleDto>>> EftpsSchedule(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var depositorStatus = await _context.CompanyPayrollSetups
            .Where(s => s.CompanyId == companyId)
            .Select(s => s.DepositSchedule)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(depositorStatus)) depositorStatus = "Unknown";

        var today = DateTime.UtcNow.Date;
        var deposits = await _context.TaxDepositSchedules
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.DepositDate)
            .Select(d => new EftpsDepositRowDto
            {
                DepositId = d.Id,
                TaxType = d.TaxType,
                Agency = d.Agency,
                FormType = d.FormType,
                Frequency = d.Frequency,
                DepositDate = d.DepositDate,
                EstimatedAmount = d.EstimatedAmount,
                DepositedAmount = d.DepositedAmount,
                DepositedOn = d.DepositedOn,
                Deposited = d.Deposited,
            }).ToListAsync(cancellationToken);
        foreach (var row in deposits)
        {
            if (row.Deposited) row.State = "Deposited";
            else if (row.DepositDate.Date < today) row.State = "Missed";
            else row.State = "Upcoming";
        }

        var upcoming = deposits.Where(d => d.State == "Upcoming").ToList();
        var missed = deposits.Where(d => d.State == "Missed").ToList();
        var depositedRows = deposits.Where(d => d.State == "Deposited").ToList();

        return Ok(ApiResponse<EftpsScheduleDto>.Success(new EftpsScheduleDto
        {
            CompanyId = companyId,
            DepositorStatus = depositorStatus,
            UpcomingCount = upcoming.Count,
            UpcomingAmount = upcoming.Sum(d => d.EstimatedAmount),
            MissedCount = missed.Count,
            MissedAmount = missed.Sum(d => d.EstimatedAmount),
            DepositedCount = depositedRows.Count,
            DepositedAmount = depositedRows.Sum(d => d.DepositedAmount ?? d.EstimatedAmount),
            NextDueDate = upcoming.OrderBy(d => d.DepositDate).Select(d => d.DepositDate).Cast<DateTime?>().FirstOrDefault(),
            Note = "Depositor status comes from CompanyPayrollSetup.DepositSchedule ('Monthly' or 'SemiWeekly'); classification compares scheduled deposit dates against today (UTC).",
            Rows = deposits,
        }));
    }

    // --- Batch F: ACH debit-return report with per-code rollup ---
    [HttpGet("reports/ach-return-report")]
    public async Task<ActionResult<ApiResponse<AchReturnReportDto>>> AchReturnReport(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.AchReturns
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedOn)
            .Select(r => new AchReturnReportRowDto
            {
                ReturnId = r.Id,
                PayrollRunId = r.PayrollRunId,
                EmployeeId = r.EmployeeId,
                TraceNumber = r.TraceNumber,
                ReturnCode = r.ReturnCode,
                Description = r.Description,
                Amount = r.Amount,
                Action = r.ReturnAction,
                Processed = r.Processed,
            }).ToListAsync(cancellationToken);

        var byCode = rows
            .GroupBy(r => r.ReturnCode)
            .Select(g => new AchReturnCodeSummaryDto { ReturnCode = g.Key, Count = g.Count(), AmountTotal = g.Sum(r => r.Amount) })
            .OrderByDescending(c => c.AmountTotal)
            .ToList();

        return Ok(ApiResponse<AchReturnReportDto>.Success(new AchReturnReportDto
        {
            CompanyId = companyId,
            TotalCount = rows.Count,
            TotalAmount = rows.Sum(r => r.Amount),
            UnprocessedCount = rows.Count(r => !r.Processed),
            ByCode = byCode,
            Note = "Action taken and processed state reflect the AchReturn record only; employee may be null when the bank return could not be mapped to a direct-deposit account.",
            Rows = rows,
        }));
    }

    // --- Batch F: new-hire reporting state (configs vs recent hires; submission tracking not persisted) ---
    [HttpGet("reports/new-hire-report")]
    public async Task<ActionResult<ApiResponse<NewHireReportDto>>> NewHireReport(
        [FromQuery] Guid companyId, [FromQuery] int? days, CancellationToken cancellationToken)
    {
        var lookbackDays = days.HasValue && days.Value > 0 ? days.Value : 90;
        var cutoff = DateTime.UtcNow.Date.AddDays(-lookbackDays);
        var today = DateTime.UtcNow.Date;

        var hires = await _context.Employees
            .Where(e => e.CompanyId == companyId && e.HireDate >= cutoff)
            .OrderByDescending(e => e.HireDate)
            .Select(e => new { e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.HireDate })
            .ToListAsync(cancellationToken);

        var configs = await _context.NewHireReportingConfigs
            .Where(c => c.CompanyId == companyId)
            .Select(c => new { c.StateCode, c.AgencyName, c.DueWindowDays })
            .ToListAsync(cancellationToken);
        var configByState = configs
            .GroupBy(c => c.StateCode.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var hireIds = hires.Select(h => h.Id).ToList();
        var profiles = await _context.EmployeeTaxProfiles
            .Where(p => hireIds.Contains(p.EmployeeId))
            .Select(p => new { p.EmployeeId, p.WorkState, p.ResidentState })
            .ToListAsync(cancellationToken);
        var profileByEmployee = profiles
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = new List<NewHireReportRowDto>();
        foreach (var hire in hires)
        {
            var state = string.Empty;
            if (profileByEmployee.TryGetValue(hire.Id, out var profile))
                state = !string.IsNullOrWhiteSpace(profile.WorkState) ? profile.WorkState : profile.ResidentState ?? string.Empty;

            var row = new NewHireReportRowDto
            {
                EmployeeId = hire.Id,
                EmployeeCode = hire.EmployeeCode,
                EmployeeName = $"{hire.FirstName} {hire.LastName}".Trim(),
                HireDate = hire.HireDate,
                State = state,
            };

            if (configByState.TryGetValue(state.ToUpperInvariant(), out var config))
            {
                row.ConfigFound = true;
                row.AgencyName = config.AgencyName;
                row.DueWindowDays = config.DueWindowDays;
                row.DueBy = hire.HireDate.AddDays(config.DueWindowDays);
                row.SubmissionStatus = "NotTracked";
                row.Overdue = row.DueBy.HasValue && row.DueBy.Value.Date < today;
            }
            else if (string.IsNullOrWhiteSpace(state))
            {
                row.SubmissionStatus = "NoStateOnProfile";
            }
            else
            {
                row.SubmissionStatus = "NoConfigForState";
            }

            rows.Add(row);
        }

        return Ok(ApiResponse<NewHireReportDto>.Success(new NewHireReportDto
        {
            CompanyId = companyId,
            LookbackDays = lookbackDays,
            ConfirmationNumber = null,
            Note = "No submission log exists yet, so submission state is 'NotTracked' where a config covers the employee's work/resident state; confirmations and failures cannot be reported until the reporting job persists transmissions.",
            Rows = rows,
        }));
    }

    // --- Batch F: workers' comp premium estimate by class code (trade-mapped payroll basis) ---
    [HttpGet("reports/workers-comp-premium")]
    public async Task<ActionResult<ApiResponse<WorkersCompPremiumDto>>> WorkersCompPremium(
        [FromQuery] Guid companyId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var classCodes = await _context.WorkersCompClassCodes
            .Where(c => c.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var tradeQ = _context.PayrollRunLines
            .Where(l => l.PayrollRun != null && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == companyId
                        && (year == null || l.PayrollRun.PayDate.Year == year.Value)
                        && l.TradeClassification != null);
        var tradePayroll = await tradeQ
            .GroupBy(l => l.TradeClassification!)
            .Select(g => new { Trade = g.Key, Gross = g.Sum(l => l.GrossPay) })
            .ToListAsync(cancellationToken);

        var rows = new List<WorkersCompPremiumRowDto>();
        var matchedTrades = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in classCodes.OrderBy(c => c.ClassCode))
        {
            var basis = tradePayroll.FirstOrDefault(t => string.Equals(t.Trade, code.ClassCode, StringComparison.OrdinalIgnoreCase));
            decimal payrollBasis = basis?.Gross ?? 0m;
            if (basis is not null) matchedTrades.Add(basis.Trade);
            rows.Add(new WorkersCompPremiumRowDto
            {
                ClassCode = code.ClassCode,
                Description = code.Description,
                State = code.State,
                RatePer100 = code.RatePer100,
                ExperienceModification = code.ExperienceModification,
                PayrollBasis = payrollBasis,
                EstimatedPremium = Math.Round(payrollBasis / 100m * code.RatePer100 * code.ExperienceModification, 2),
                ActualBooked = null,
            });
        }

        var unmatchedTradePayroll = tradePayroll
            .Where(t => !matchedTrades.Contains(t.Trade))
            .Sum(t => t.Gross);

        return Ok(ApiResponse<WorkersCompPremiumDto>.Success(new WorkersCompPremiumDto
        {
            CompanyId = companyId,
            Year = year,
            TotalEstimatedPremium = rows.Sum(r => r.EstimatedPremium),
            UnmatchedTradePayroll = unmatchedTradePayroll,
            Note = "Payroll basis maps posted-run TradeClassification to WC ClassCode by exact text match; employees carry no direct class-code assignment. Actual booked premium is not stored in this module, so ActualBooked is explicitly null (estimated-only).",
            Rows = rows,
        }));
    }

    // --- Batch E: Termination / final pay: create an off-cycle manual check for a terminated employee ---
    [HttpPost("employees/{employeeId:guid}/terminate")]
    public async Task<ActionResult<ApiResponse<Guid>>> TerminateEmployee(
        Guid employeeId, [FromBody] TerminateRequest request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null) return NotFound(ApiResponse.Failure(new[] { "Employee not found." }, 404));
        employee.Terminate(request.TerminationDate);
        var final = new ManualCheck(employee.CompanyId, employeeId, request.FinalGross, request.PayDate, "Final pay on termination", request.CheckNumber);
        final.Mark1099(false);
        _context.ManualChecks.Add(final);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(final.Id));
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

// Batch E DTOs
public class PositivePayRowDto
{
    public Guid EmployeeId { get; set; }
    public string CheckNumber { get; set; } = string.Empty;
    public DateTime CheckDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsDirectDeposit { get; set; }
    public string? AchTraceNumber { get; set; }
}

public class Form1099NecRowDto
{
    public Guid RecipientId { get; set; }
    public decimal NonemployeeCompensation { get; set; }
    public decimal FederalIncomeTaxWithheld { get; set; }
}

public class StateWithholdingDto
{
    public string State { get; set; } = string.Empty;
    public decimal StateWithholding { get; set; }
    public bool Exempt { get; set; }
}

public class MultiStateWithholdingDto
{
    public Guid EmployeeId { get; set; }
    public decimal TaxableWages { get; set; }
    public decimal FederalWithholding { get; set; }
    public List<StateWithholdingDto> States { get; set; } = new();
}

public class UnionReportRowDto
{
    public string TradeClassification { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public decimal PrevailingWageRate { get; set; }
    public decimal FringeBenefitRate { get; set; }
    public decimal TotalPrevailingRate { get; set; }
    public string? UnionLocal { get; set; }
}

public class WorkersCompReportRowDto
{
    public string ClassCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal RatePer100 { get; set; }
    public decimal ExperienceModification { get; set; }
    public decimal EffectiveRatePer100 { get; set; }
}

public class TerminateRequest
{
    public DateTime TerminationDate { get; set; }
    public DateTime PayDate { get; set; }
    public decimal FinalGross { get; set; }
    public decimal FinalNet { get; set; }
    public bool IsDirectDeposit { get; set; }
    public string? CheckNumber { get; set; }
}

// Batch F DTOs
public class TaxLiabilityRowDto
{
    public string Jurisdiction { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public string? FormType { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountOwed { get; set; }
    public decimal? DepositedAmount { get; set; }
    public bool Deposited { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TaxLiabilityDto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public decimal TotalEmployeeTaxWithheld { get; set; }
    public decimal TotalEmployerTaxAccrued { get; set; }
    public string DepositorStatus { get; set; } = "Unknown";
    public DateTime? NextDepositDue { get; set; }
    public string DueDateHint { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public List<TaxLiabilityRowDto> Rows { get; set; } = new();
}

public class DeductionRegisterRowDto
{
    public Guid BenefitId { get; set; }
    public string BenefitCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPreTax { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public decimal? Percent { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? GlAccountNumber { get; set; }
}

public class DeductionTypeTotalDto
{
    public string Type { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public decimal AmountTotal { get; set; }
}

public class DeductionRegisterDto
{
    public Guid CompanyId { get; set; }
    public DateTime AsOf { get; set; }
    public decimal TotalRemittanceDue { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<DeductionTypeTotalDto> TypeTotals { get; set; } = new();
    public List<DeductionRegisterRowDto> Rows { get; set; } = new();
}

public class CertifiedPayrollWh347RowDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string TradeClassification { get; set; } = string.Empty;
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BaseRate { get; set; }
    public decimal? FringeRate { get; set; }
    public decimal FringeCost { get; set; }
    public decimal Gross { get; set; }
    public bool MeetsPrevailing { get; set; }
}

public class CertifiedPayrollWh347Dto
{
    public Guid CompanyId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalFringe { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<CertifiedPayrollWh347RowDto> Rows { get; set; } = new();
}

public class TimeExpenseProjectRowDto
{
    public Guid? ProjectId { get; set; }
    public decimal Hours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal Expenses { get; set; }
    public decimal BillableAmount { get; set; }
}

public class TimeExpenseByProjectDto
{
    public Guid CompanyId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<TimeExpenseProjectRowDto> Rows { get; set; } = new();
}

public class EmployeeEarningsRowDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Gross { get; set; }
    public decimal EmployeeTax { get; set; }
    public decimal EmployerTax { get; set; }
    public decimal Deductions { get; set; }
    public decimal Net { get; set; }
    public int RunCount { get; set; }
}

public class EmployeeEarningsDto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<EmployeeEarningsRowDto> Rows { get; set; } = new();
}

public class W2ReconciliationDto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public decimal RunWages { get; set; }
    public decimal ManualCheckWages { get; set; }
    public decimal W2Wages { get; set; }
    public decimal ExpectedGlWageExpense { get; set; }
    public decimal Variance { get; set; }
    public bool GlTieOutPending { get; set; }
    public string Assumption { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class Form941ReconciliationDto
{
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal TotalWages { get; set; }
    public decimal FederalIncomeTaxWithheldEstimated { get; set; }
    public decimal EmployeeFicaEstimated { get; set; }
    public decimal EmployerFicaEstimated { get; set; }
    public decimal EmployerTaxResidual { get; set; }
    public decimal EmployeeTaxWithheldActual { get; set; }
    public decimal EmployerTaxActual { get; set; }
    public decimal SocialSecurityRateUsed { get; set; }
    public decimal MedicareRateUsed { get; set; }
    public bool GlTieOutPending { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class PayrollAccrualRowDto
{
    public Guid EmployeeId { get; set; }
    public decimal Hours { get; set; }
    public decimal AccruedWages { get; set; }
}

public class PayrollAccrualDto
{
    public Guid CompanyId { get; set; }
    public DateTime AsOf { get; set; }
    public DateTime? LastPostedPeriodEnd { get; set; }
    public decimal AccruedWages { get; set; }
    public decimal EmployerTaxAccrualEstimate { get; set; }
    public decimal EmployerTaxRateUsed { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<PayrollAccrualRowDto> Rows { get; set; } = new();
}

public class EftpsDepositRowDto
{
    public Guid DepositId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public string? FormType { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public DateTime DepositDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal? DepositedAmount { get; set; }
    public DateTime? DepositedOn { get; set; }
    public bool Deposited { get; set; }
    public string State { get; set; } = string.Empty;
}

public class EftpsScheduleDto
{
    public Guid CompanyId { get; set; }
    public string DepositorStatus { get; set; } = "Unknown";
    public int UpcomingCount { get; set; }
    public decimal UpcomingAmount { get; set; }
    public int MissedCount { get; set; }
    public decimal MissedAmount { get; set; }
    public int DepositedCount { get; set; }
    public decimal DepositedAmount { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<EftpsDepositRowDto> Rows { get; set; } = new();
}

public class AchReturnReportRowDto
{
    public Guid ReturnId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public string ReturnCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Processed { get; set; }
}

public class AchReturnCodeSummaryDto
{
    public string ReturnCode { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal AmountTotal { get; set; }
}

public class AchReturnReportDto
{
    public Guid CompanyId { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int UnprocessedCount { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<AchReturnCodeSummaryDto> ByCode { get; set; } = new();
    public List<AchReturnReportRowDto> Rows { get; set; } = new();
}

public class NewHireReportRowDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public string State { get; set; } = string.Empty;
    public bool ConfigFound { get; set; }
    public string? AgencyName { get; set; }
    public int? DueWindowDays { get; set; }
    public DateTime? DueBy { get; set; }
    public bool Overdue { get; set; }
    public string SubmissionStatus { get; set; } = string.Empty;
    public string? ConfirmationNumber { get; set; }
}

public class NewHireReportDto
{
    public Guid CompanyId { get; set; }
    public int LookbackDays { get; set; }
    public string? ConfirmationNumber { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<NewHireReportRowDto> Rows { get; set; } = new();
}

public class WorkersCompPremiumRowDto
{
    public string ClassCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal RatePer100 { get; set; }
    public decimal ExperienceModification { get; set; }
    public decimal PayrollBasis { get; set; }
    public decimal EstimatedPremium { get; set; }
    public decimal? ActualBooked { get; set; }
}

public class WorkersCompPremiumDto
{
    public Guid CompanyId { get; set; }
    public int? Year { get; set; }
    public decimal TotalEstimatedPremium { get; set; }
    public decimal UnmatchedTradePayroll { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<WorkersCompPremiumRowDto> Rows { get; set; } = new();
}
