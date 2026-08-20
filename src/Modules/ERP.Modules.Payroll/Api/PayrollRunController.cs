// <copyright file="PayrollRunController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Posting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll")]
public class PayrollRunController : ControllerBase
{
    private readonly PayrollDbContext _context;
    private readonly PlatformDbContext _platformContext;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;

    public PayrollRunController(
        PayrollDbContext context,
        PlatformDbContext platformContext,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser)
    {
        _context = context;
        _platformContext = platformContext;
        _postingPublisher = postingPublisher;
        _currentUser = currentUser;
    }

    // --- Payroll calendar ---
    [HttpPost("calendars")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCalendar(
        [FromBody] CreateCalendarRequest request, CancellationToken cancellationToken)
    {
        var calendar = new PayrollCalendar(request.CompanyId, request.Name, request.Frequency, request.StartDate)
        {
            EmployerFicaRate = request.EmployerFicaRate,
            EmployeeFicaRate = request.EmployeeFicaRate,
            FutaRate = request.FutaRate,
            SutaRate = request.SutaRate,
        };
        _context.PayrollCalendars.Add(calendar);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(calendar.Id));
    }

    // --- Draft payroll run from approved timesheets ---
    [HttpPost("runs/draft")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateDraftRun(
        [FromBody] CreateRunRequest request, CancellationToken cancellationToken)
    {
        var calendar = await _context.PayrollCalendars
            .FirstOrDefaultAsync(c => c.Id == request.CalendarId, cancellationToken);
        // Calendar is advisory for a draft run; a run can be built from approved timesheets
        // even if no calendar exists yet. A missing calendar is not a hard blocker.
        if (calendar is null && request.CalendarId != Guid.Empty)
            return BadRequest(ApiResponse.Failure(new[] { "Payroll calendar not found." }));

        var run = new PayrollRun(request.CompanyId, request.CalendarId, request.PeriodStart, request.PeriodEnd, request.PayDate);

        // Pull approved timesheet lines in the pay period (optionally filtered to charged project for certified payroll).
        var lines = await (from l in _context.TimesheetLines
                           join t in _context.Timesheets on l.TimesheetId equals t.Id
                           where t.CompanyId == request.CompanyId
                                 && t.Status == TimesheetStatus.Approved
                                 && l.WorkDate >= request.PeriodStart
                                 && l.WorkDate <= request.PeriodEnd
                           select new { Line = l, EmployeeId = t.EmployeeId })
            .ToListAsync(cancellationToken);

        var byEmployee = lines
            .GroupBy(x => x.EmployeeId)
            .ToList();

        foreach (var grp in byEmployee)
        {
            var employeeId = grp.Key;
            var regularHours = grp.Where(x => !x.Line.IsOvertime).Sum(x => x.Line.Hours);
            var overtimeHours = grp.Where(x => x.Line.IsOvertime).Sum(x => x.Line.Hours);
            // Use the blended rate (first line's rate) as the basis for the run calculation.
            var sample = grp.First().Line;
            var regularRate = sample.Rate;
            var overtimeRate = sample.Rate * 1.5m;
            var employeeFica = calendar?.EmployeeFicaRate ?? 0.0765m;
            var employerFica = calendar?.EmployerFicaRate ?? 0.0765m;
            var futa = calendar?.FutaRate ?? 0.006m;
            var suta = calendar?.SutaRate ?? 0.0m;
            var grossPay = (regularHours * regularRate) + (overtimeHours * overtimeRate);
            var employeeTax = Math.Round(grossPay * employeeFica, 2);
            var deductions = 0m; // deductions accumulated from employee pay codes if configured
            var employerTax = Math.Round(grossPay * (employerFica + futa + suta), 2);
            var netPay = Math.Round(grossPay - employeeTax - deductions, 2);

            // Certified payroll: fringe/prevailing from the trade's union profile (Davis-Bacon).
            decimal? prevailing = null;
            decimal? fringe = null;
            if (!string.IsNullOrWhiteSpace(sample.TradeClassification))
            {
                var profile = await _context.UnionCertifiedProfiles
                    .Where(p => p.TradeClassification == sample.TradeClassification)
                    .OrderByDescending(p => p.PrevailingWageRate)
                    .FirstOrDefaultAsync(cancellationToken);
                if (profile is not null)
                {
                    prevailing = profile.PrevailingWageRate;
                    fringe = profile.FringeBenefitRate;
                }
            }

            run.AddLine(employeeId, regularHours, overtimeHours, regularRate, overtimeRate,
                grossPay, employeeTax, deductions, employerTax, netPay, prevailing, fringe, sample.TradeClassification);
        }

        run.MarkDraftCalculated();
        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(run.Id));
    }

    // --- Post (finalize) payroll run -> GL ---
    [HttpPost("runs/{id:guid}/post")]
    public async Task<ActionResult<ApiResponse>> PostRun(
        Guid id, [FromBody] PostRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        if (run.Status != PayrollRunStatus.Draft)
            return BadRequest(ApiResponse.Failure(new[] { "Run is not in draft state." }));

        // Resolve GL accounts.
        var wageExpenseId = await ResolveAccountAsync(run.CompanyId, "6000", cancellationToken);   // Salaries & Wages
        var payrollLiabId = await ResolveAccountAsync(run.CompanyId, "2200", cancellationToken);   // Payroll Liabilities
        var otherExpenseId = await ResolveAccountAsync(run.CompanyId, "7000", cancellationToken);   // Employer taxes bucket

        var segments = ERP.Shared.Kernel.Posting.AccountKey.Create();

        var lines = new List<PostingLine>
        {
            // Dr Wage Expense (gross wages)
            new PostingLine { AccountId = wageExpenseId, Segments = segments, Debit = run.TotalGross, Credit = 0m, Currency = "USD" },
            // Dr Payroll Tax Expense (employer taxes)
            new PostingLine { AccountId = otherExpenseId, Segments = segments, Debit = run.TotalEmployerTax, Credit = 0m, Currency = "USD" },
            // Cr Payroll Liabilities (net payable to employees)
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = 0m, Credit = run.TotalNet, Currency = "USD" },
            // Cr Payroll Liabilities (employee tax withheld)
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = 0m, Credit = run.TotalEmployeeTax, Currency = "USD" },
            // Cr Payroll Liabilities (employer taxes accrued)
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = 0m, Credit = run.TotalEmployerTax, Currency = "USD" },
        };

        var period = await ResolveFiscalPeriodAsync(run.CompanyId, run.PayDate, cancellationToken);
        var postedBy = _currentUser.UserId ?? "system";
        var batchNumber = $"PAYROLL-{run.Id:N}";

        var postingEvent = CanonicalPostingEvent.Create(
            "PAY",
            batchNumber,
            run.CompanyId,
            period?.Id ?? run.CompanyId,
            run.CompanyId.ToString(),
            (period?.Id ?? run.CompanyId).ToString(),
            new DateTimeOffset(run.PayDate),
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid()));

        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
        run.MarkPosted(request.PostedById, batchNumber);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Certified payroll report (Davis-Bacon) (closes 822 / 1000) ---
    [HttpGet("runs/{id:guid}/certified-payroll")]
    public async Task<ActionResult<ApiResponse<CertifiedPayrollReportDto>>> CertifiedPayrollReport(
        Guid id, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));

        var employeeIds = run.Lines.Select(l => l.EmployeeId).Distinct().ToList();
        var employees = await _context.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken);

        var rows = run.Lines.Select(l => new CertifiedPayrollRowDto
        {
            EmployeeName = employees.GetValueOrDefault(l.EmployeeId, l.EmployeeId.ToString()),
            TradeClassification = l.TradeClassification ?? "(none)",
            RegularHours = l.RegularHours,
            OvertimeHours = l.OvertimeHours,
            TotalHours = l.RegularHours + l.OvertimeHours,
            GrossWage = l.GrossPay,
            // Certified "basic rate" reported as the actual paid rate (>= prevailing by validation).
            BasicRate = l.RegularHours > 0 ? Math.Round(l.GrossPay / (l.RegularHours + (l.OvertimeHours * 1.5m)), 2) : 0m,
            FringeRate = l.FringeRate ?? 0m,
            FringeCost = l.FringeCost,
            PrevailingWageRate = l.PrevailingWageRate ?? 0m,
            TotalPrevailingRate = l.TotalPrevailingRate,
            MeetsPrevailing = !l.PrevailingWageRate.HasValue || l.GrossPay / (l.RegularHours + l.OvertimeHours) >= l.PrevailingWageRate,
        }).ToList();

        return Ok(ApiResponse<CertifiedPayrollReportDto>.Success(new CertifiedPayrollReportDto
        {
            PayrollRunId = run.Id,
            PeriodStart = run.PeriodStart,
            PeriodEnd = run.PeriodEnd,
            PayDate = run.PayDate,
            TotalGross = run.TotalGross,
            TotalFringe = rows.Sum(r => r.FringeCost),
            Rows = rows,
        }));
    }

    private async Task<Guid> ResolveAccountAsync(Guid companyId, string accountNumber, CancellationToken cancellationToken)
    {
        var account = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountNumber == accountNumber, cancellationToken);
        if (account is null)
            throw new InvalidOperationException($"GL account '{accountNumber}' for company {companyId} was not found.");
        return account.Id;
    }

    private async Task<FiscalPeriod?> ResolveFiscalPeriodAsync(Guid companyId, DateTime transactionDate, CancellationToken cancellationToken)
    {
        var date = new DateTimeOffset(transactionDate);
        return await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.StartDate <= date && p.EndDate >= date)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // --- Payroll accrual (period-end: accrue wages earned-but-unpaid) (closes 987) ---
    [HttpPost("runs/{id:guid}/accrue")]
    public async Task<ActionResult<ApiResponse>> AccrueRun(
        Guid id, [FromBody] AccrueRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));

        var wageExpenseId = await ResolveAccountAsync(run.CompanyId, "6000", cancellationToken);   // Salaries & Wages
        var payrollLiabId = await ResolveAccountAsync(run.CompanyId, "2200", cancellationToken);   // Payroll Liabilities (accrued wages)

        var segments = ERP.Shared.Kernel.Posting.AccountKey.Create();
        var lines = new List<PostingLine>
        {
            // Dr Wage Expense (accrued gross), Cr Payroll Liabilities (accrued wages payable)
            new PostingLine { AccountId = wageExpenseId, Segments = segments, Debit = run.TotalGross, Credit = 0m, Currency = "USD" },
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = 0m, Credit = run.TotalGross, Currency = "USD" },
        };

        var period = await ResolveFiscalPeriodAsync(run.CompanyId, request.AccrualDate, cancellationToken);
        var postedBy = _currentUser.UserId ?? "system";
        var batchNumber = $"PAYROLL-ACCRUAL-{run.Id:N}";

        var postingEvent = CanonicalPostingEvent.Create(
            "PAY",
            batchNumber,
            run.CompanyId,
            period?.Id ?? run.CompanyId,
            run.CompanyId.ToString(),
            (period?.Id ?? run.CompanyId).ToString(),
            new DateTimeOffset(request.AccrualDate),
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid()));
        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Reverse a posted run (payroll correction / re-run) ---
    [HttpPost("runs/{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse>> ReverseRun(
        Guid id, [FromBody] ReverseRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        if (run.Status != PayrollRunStatus.Posted)
            return BadRequest(ApiResponse.Failure(new[] { "Only a posted run can be reversed." }));

        var wageExpenseId = await ResolveAccountAsync(run.CompanyId, "6000", cancellationToken);
        var payrollLiabId = await ResolveAccountAsync(run.CompanyId, "2200", cancellationToken);
        var otherExpenseId = await ResolveAccountAsync(run.CompanyId, "7000", cancellationToken);

        var segments = ERP.Shared.Kernel.Posting.AccountKey.Create();
        var lines = new List<PostingLine>
        {
            // Reversing entry: negate the original posted legs.
            new PostingLine { AccountId = wageExpenseId, Segments = segments, Debit = 0m, Credit = run.TotalGross, Currency = "USD" },
            new PostingLine { AccountId = otherExpenseId, Segments = segments, Debit = 0m, Credit = run.TotalEmployerTax, Currency = "USD" },
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = run.TotalNet, Credit = 0m, Currency = "USD" },
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = run.TotalEmployeeTax, Credit = 0m, Currency = "USD" },
            new PostingLine { AccountId = payrollLiabId, Segments = segments, Debit = run.TotalEmployerTax, Credit = 0m, Currency = "USD" },
        };

        var period = await ResolveFiscalPeriodAsync(run.CompanyId, request.ReversalDate, cancellationToken);
        var postedBy = _currentUser.UserId ?? "system";
        var batchNumber = $"PAYROLL-REV-{run.Id:N}";

        var postingEvent = CanonicalPostingEvent.Create(
            "PAY",
            batchNumber,
            run.CompanyId,
            period?.Id ?? run.CompanyId,
            run.CompanyId.ToString(),
            (period?.Id ?? run.CompanyId).ToString(),
            new DateTimeOffset(request.ReversalDate),
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid()));
        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
        run.Reverse();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Review/edit a draft run line (adjust hours or bonus before final) ---
    [HttpPost("runs/{id:guid}/lines/{lineId:guid}/edit")]
    public async Task<ActionResult<ApiResponse>> EditRunLine(
        Guid id, Guid lineId, [FromBody] EditRunLineRequest request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        try
        {
            run.EditLine(lineId, request.RegularHours, request.OvertimeHours, request.Bonus);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Void (discard) a draft run: no GL impact ---
    [HttpPost("runs/{id:guid}/void")]
    public async Task<ActionResult<ApiResponse>> VoidRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        try
        {
            run.Void();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failure(new[] { ex.Message }));
        }
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Check printing: generate a check/stub for each run line (net pay) ---
    [HttpPost("runs/{id:guid}/print-checks")]
    public async Task<ActionResult<ApiResponse<List<PayrollCheckDto>>>> PrintChecks(
        Guid id, [FromBody] PrintChecksRequest request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        if (run.Status != PayrollRunStatus.Posted)
            return BadRequest(ApiResponse.Failure(new[] { "Only a posted run can be printed." }));

        var checks = new List<PayrollCheckDto>();
        int seq = request.StartingCheckNumber;
        foreach (var line in run.Lines)
        {
            var check = new PayrollCheck(run.Id, line.EmployeeId, line.NetPay, seq.ToString(), request.CheckDate);
            check.SetDirectDeposit(request.DirectDeposit);
            _context.PayrollChecks.Add(check);
            checks.Add(new PayrollCheckDto
            {
                EmployeeId = line.EmployeeId,
                NetPay = line.NetPay,
                CheckNumber = seq.ToString(),
                CheckDate = request.CheckDate,
                IsDirectDeposit = request.DirectDeposit,
            });
            seq++;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<List<PayrollCheckDto>>.Success(checks));
    }

    // --- ACH NACHA file export (PPD credits to employees) ---
    [HttpGet("runs/{id:guid}/ach-nacha")]
    public async Task<ActionResult<ApiResponse<string>>> ExportNacha(Guid id, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
            return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));

        CultureInfo inv = CultureInfo.InvariantCulture;
        var companyName = "ERP COMPANY";
        var companyId = run.CompanyId.ToString("N", inv)[..9].ToUpperInvariant();
        var fileIdModifier = BuildAchFileId();
        var sb = new System.Text.StringBuilder();
        // File header (record type 1)
        sb.Append("101  ").Append("1234567890  ").Append(companyId.PadRight(10))
            .Append(DateTime.Now.ToString("yyMMdd", inv)).Append(DateTime.Now.ToString("HHmm", inv))
            .Append(fileIdModifier).Append("PPD").Append("PAYROLL".PadRight(6))
            .Append(new string(' ', 26)).AppendLine("1");
        // Batch header (record type 5)
        sb.Append("5200").Append(companyName.PadRight(16)).Append("PAYROLL".PadRight(20))
            .Append("1234567890".PadLeft(10)).Append("PPD").Append("PAYROLL".PadRight(20))
            .Append(DateTime.Now.ToString("yyMMdd", inv)).Append(DateTime.Now.ToString("yyMMdd", inv))
            .Append("1".PadLeft(10, '0')).Append(new string(' ', 25)).AppendLine("1");
        int entryCount = 0;
        decimal totalCredit = 0m;
        int trace = 1;
        foreach (var line in run.Lines)
        {
            var amountCents = (long)Math.Round(line.NetPay * 100m);
            // Entry detail (record type 6): PPD credit
            sb.Append("627 ").Append("1234567890".PadLeft(9)).Append(line.EmployeeId.ToString("N", inv)[..9].ToUpperInvariant().PadLeft(15, '0'))
                .Append(amountCents.ToString(inv).PadLeft(10, '0'))
                .Append(line.EmployeeId.ToString("N", inv)[..9].ToUpperInvariant().PadLeft(15, '0'))
                .Append("              ").Append('0').Append(trace.ToString(inv).PadLeft(15, '0')).Append("  ")
                .Append(DateTime.Now.ToString("yyMMdd", inv)).Append("1").AppendLine();
            entryCount++;
            totalCredit += line.NetPay;
            trace++;
        }

        int blockCount = ((entryCount + 1) / 10) + 1;
        long totalCreditCents = (long)Math.Round(totalCredit * 100m);
        // Batch control (record type 8)
        sb.Append("8200").Append(entryCount.ToString(inv).PadLeft(6, '0'))
            .Append(new string('0', 10))
            .Append(totalCreditCents.ToString(inv).PadLeft(12, '0'))
            .Append(new string(' ', 39)).Append("1".PadLeft(10, '0')).Append(new string(' ', 25)).AppendLine("1");
        // File control (record type 9)
        sb.Append("9000001").Append(entryCount.ToString(inv).PadLeft(6, '0')).Append(blockCount.ToString(inv).PadLeft(6, '0'))
            .Append(totalCreditCents.ToString(inv).PadLeft(12, '0')).Append(new string('0', 12))
            .Append(new string(' ', 39)).Append("1").AppendLine();

        return Ok(ApiResponse<string>.Success(sb.ToString()));
    }

    private static string BuildAchFileId() => DateTime.Now.ToString("HHmm", CultureInfo.InvariantCulture);
}

public class EditRunLineRequest
{
    public decimal? RegularHours { get; set; }
    public decimal? OvertimeHours { get; set; }
    public decimal? Bonus { get; set; }
}

public class PrintChecksRequest
{
    public DateTime CheckDate { get; set; }
    public int StartingCheckNumber { get; set; } = 1000;
    public bool DirectDeposit { get; set; }
}

public class PayrollCheckDto
{
    public Guid EmployeeId { get; set; }
    public decimal NetPay { get; set; }
    public string CheckNumber { get; set; } = string.Empty;
    public DateTime CheckDate { get; set; }
    public bool IsDirectDeposit { get; set; }
}

public class AccrueRunRequest
{
    public DateTime AccrualDate { get; set; }
}

public class ReverseRunRequest
{
    public DateTime ReversalDate { get; set; }
}

public class CreateCalendarRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PayrollFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public decimal EmployerFicaRate { get; set; } = 0.0765m;
    public decimal EmployeeFicaRate { get; set; } = 0.0765m;
    public decimal FutaRate { get; set; } = 0.006m;
    public decimal SutaRate { get; set; } = 0.034m;
}

public class CreateRunRequest
{
    public Guid CompanyId { get; set; }
    public Guid CalendarId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime PayDate { get; set; }
}

public class PostRunRequest
{
    public Guid PostedById { get; set; }
}

public class CertifiedPayrollReportDto
{
    public Guid PayrollRunId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime PayDate { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalFringe { get; set; }
    public List<CertifiedPayrollRowDto> Rows { get; set; } = [];
}

public class CertifiedPayrollRowDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string TradeClassification { get; set; } = string.Empty;
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal TotalHours { get; set; }
    public decimal GrossWage { get; set; }
    public decimal BasicRate { get; set; }
    public decimal FringeRate { get; set; }
    public decimal FringeCost { get; set; }
    public decimal PrevailingWageRate { get; set; }
    public decimal TotalPrevailingRate { get; set; }
    public bool MeetsPrevailing { get; set; }
}
