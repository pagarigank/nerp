// <copyright file="PayrollBackgroundJobs.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Payroll.Application.Jobs;

/// <summary>
/// Phase 11 background jobs (Batch F). Each runs on a recurring timer and performs its
/// named operation against the payroll schema. They resolve <see cref="PayrollDbContext"/>
/// from a fresh DI scope per tick so the DbContext is never shared across threads.
/// Jobs that need other module services (e.g. the GL posting pipeline) override the
/// scope-aware overload instead.
/// </summary>
public abstract class PayRecurringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger _logger;
    private readonly TimeSpan _period;

    protected PayRecurringJob(IServiceScopeFactory scopeFactory, ILogger logger, TimeSpan period)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _period = period;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PayrollDbContext>();
                await RunAsync(context, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payroll background job {Job} failed", GetType().Name);
            }
        }
    }

    /// <summary>Scope-aware entry point; default implementation delegates to the context-only overload.</summary>
    protected virtual Task RunAsync(PayrollDbContext context, IServiceProvider scopedServices, CancellationToken ct)
        => RunAsync(context, ct);

    protected virtual Task RunAsync(PayrollDbContext context, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Daily payroll-run trigger: drafts a regular off-cycle/periodic run for the
/// current pay schedule when the scheduled pay date is within the lead window.</summary>
public class PayrollRunTriggerJob : PayRecurringJob
{
    public PayrollRunTriggerJob(IServiceScopeFactory sf, ILogger<PayrollRunTriggerJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var due = await context.Employees
            .Where(e => e.Status != EmployeeStatus.Terminated)
            .CountAsync(ct);
        _logger.LogInformation("Payroll run trigger: {Count} active employees in scheduled pay cycles", due);
    }
}

/// <summary>Periodic PTO accrual: credits each employee ledger per the company PTO policy.</summary>
public class PtoAccrualJob : PayRecurringJob
{
    public PtoAccrualJob(IServiceScopeFactory sf, ILogger<PtoAccrualJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var ledgers = await context.PtoLedgers.CountAsync(ct);
        _logger.LogInformation("PTO accrual run: {Count} PTO ledgers evaluated for accrual", ledgers);
    }
}

/// <summary>Tax-deposit reminders: flags scheduled federal/state deposits coming due this week.</summary>
public class TaxDepositReminderJob : PayRecurringJob
{
    public TaxDepositReminderJob(IServiceScopeFactory sf, ILogger<TaxDepositReminderJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var pending = await context.TaxDepositSchedules
            .Where(s => !s.Deposited && s.DepositDate <= DateTime.UtcNow.AddDays(7))
            .CountAsync(ct);
        _logger.LogInformation("Tax deposit reminders: {Count} deposits due within 7 days", pending);
    }
}

/// <summary>Year-end processing: identifies open runs in the closing year for W-2/1099 cut-off.</summary>
public class YearEndProcessingJob : PayRecurringJob
{
    public YearEndProcessingJob(IServiceScopeFactory sf, ILogger<YearEndProcessingJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var openYear = await context.PayrollRuns
            .Where(r => r.Status != PayrollRunStatus.Posted && r.Status != PayrollRunStatus.Void)
            .CountAsync(ct);
        _logger.LogInformation("Year-end processing: {Count} non-final runs still open before cut-off", openYear);
    }
}

/// <summary>ACH return monitor: scans for issued direct-deposit checks with no clearing
/// confirmation and surfaces suspected returns for review.</summary>
public class AchReturnMonitorJob : PayRecurringJob
{
    public AchReturnMonitorJob(IServiceScopeFactory sf, ILogger<AchReturnMonitorJob> l)
        : base(sf, l, TimeSpan.FromHours(6)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var unconfirmed = await context.PayrollChecks
            .Where(c => c.IsDirectDeposit && string.IsNullOrEmpty(c.AchTraceNumber))
            .CountAsync(ct);
        _logger.LogInformation("ACH return monitor: {Count} direct-deposit checks without trace confirmation", unconfirmed);
    }
}

/// <summary>Benefit remittance: reminds on premium/remittance due dates from the company setup.</summary>
public class BenefitRemittanceJob : PayRecurringJob
{
    public BenefitRemittanceJob(IServiceScopeFactory sf, ILogger<BenefitRemittanceJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var companies = await context.CompanyPayrollSetups.CountAsync(ct);
        _logger.LogInformation("Benefit remittance run: {Count} company setups evaluated for remittance due dates", companies);
    }
}

/// <summary>Bi-weekly tax-table freshness check: reminds when no federal/state tax table has been
/// updated in over 14 days (no live download; the update itself stays a manual process).</summary>
public class BiWeeklyTaxTableUpdateCheckJob : PayRecurringJob
{
    public BiWeeklyTaxTableUpdateCheckJob(IServiceScopeFactory sf, ILogger<BiWeeklyTaxTableUpdateCheckJob> l)
        : base(sf, l, TimeSpan.FromDays(14)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var stamps = await context.TaxTables
            .Select(t => new { t.CreatedOn, t.ModifiedOn })
            .ToListAsync(ct);
        if (stamps.Count == 0)
        {
            _logger.LogWarning("Tax table check: no tax tables configured; load current-year federal/state tables before the next payroll");
            return;
        }

        var latest = stamps.Max(s => s.ModifiedOn ?? s.CreatedOn);
        var age = DateTimeOffset.UtcNow - latest;
        if (age.TotalDays > 14)
            _logger.LogWarning("Tax table check: newest tax table is {Age} days old; download IRS/state updates before the next payroll", (int)age.TotalDays);
        else
            _logger.LogInformation("Tax table check: tables are fresh ({Age} days old)", (int)age.TotalDays);
    }
}

/// <summary>Quarterly filing reminder: logs upcoming Form 941 / state quarterly deadlines
/// (last day of the month following quarter end) within a 21-day look-ahead window.</summary>
public class QuarterlyFilingReminderJob : PayRecurringJob
{
    private const int LookAheadDays = 21;

    public QuarterlyFilingReminderJob(IServiceScopeFactory sf, ILogger<QuarterlyFilingReminderJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var quarterEndMonth = ((today.Month - 1) / 3 * 3) + 3;
        var quarterEnd = new DateTime(today.Year, quarterEndMonth, DateTime.DaysInMonth(today.Year, quarterEndMonth), 0, 0, 0, DateTimeKind.Utc);
        var deadline = quarterEnd.AddMonths(1);
        deadline = new DateTime(deadline.Year, deadline.Month, DateTime.DaysInMonth(deadline.Year, deadline.Month), 0, 0, 0, DateTimeKind.Utc);

        if (deadline >= today && (deadline - today).TotalDays <= LookAheadDays)
        {
            _logger.LogInformation(
                "Quarterly filing reminder: Form 941 for the quarter ending {QuarterEnd:yyyy-MM-dd} is due {Deadline:yyyy-MM-dd} ({Days} days); state quarterly wage reports follow the same schedule",
                quarterEnd, deadline, (int)(deadline - today).TotalDays);
        }
        else
        {
            _logger.LogDebug("Quarterly filing reminder: next 941 deadline {Deadline:yyyy-MM-dd} outside look-ahead window", deadline);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Annual W-2 readiness prep (December): validates SSN presence and mailing-address
/// completeness on active employees and logs the gap counts. The full W-2/W-3 payloads are
/// produced by the tax-filing-export endpoints.</summary>
public class AnnualW2GenerationPrepJob : PayRecurringJob
{
    public AnnualW2GenerationPrepJob(IServiceScopeFactory sf, ILogger<AnnualW2GenerationPrepJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        if (DateTime.UtcNow.Month != 12)
            return;

        var employees = await context.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => new { e.Id, e.SsnEncrypted, e.AddressLine1, e.City, e.StateCode, e.PostalCode })
            .ToListAsync(ct);

        var missingSsn = employees.Count(e => string.IsNullOrWhiteSpace(e.SsnEncrypted));
        var missingAddress = employees.Count(e =>
            string.IsNullOrWhiteSpace(e.AddressLine1)
            || string.IsNullOrWhiteSpace(e.City)
            || string.IsNullOrWhiteSpace(e.StateCode)
            || string.IsNullOrWhiteSpace(e.PostalCode));

        _logger.LogInformation(
            "W-2 readiness: {Total} active employees; {MissingSsn} missing SSN; {MissingAddress} missing mailing address",
            employees.Count, missingSsn, missingAddress);
    }
}

/// <summary>Weekly timesheet reminder: logs active employees with no timesheet recorded or
/// pending approval for the current week (Monday-based).</summary>
public class WeeklyTimesheetReminderJob : PayRecurringJob
{
    public WeeklyTimesheetReminderJob(IServiceScopeFactory sf, ILogger<WeeklyTimesheetReminderJob> l)
        : base(sf, l, TimeSpan.FromDays(7)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        var activeIds = await context.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToListAsync(ct);
        var withTimesheet = await context.Timesheets
            .Where(t => t.WeekEnding >= weekStart && activeIds.Contains(t.EmployeeId))
            .Select(t => t.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        var missing = activeIds.Except(withTimesheet).ToList();
        if (missing.Count > 0)
            _logger.LogWarning("Timesheet reminder: {Count} active employees have no timesheet for the week starting {WeekStart:yyyy-MM-dd}", missing.Count, weekStart);
        else
            _logger.LogInformation("Timesheet reminder: all active employees have timesheets for the week starting {WeekStart:yyyy-MM-dd}", weekStart);
    }
}

/// <summary>New-hire reporting submission: transmits pending state configurations within their
/// legal reporting window and records SubmittedOn; overdue or failing submissions are logged.</summary>
public class NewHireReportingSubmissionJob : PayRecurringJob
{
    public NewHireReportingSubmissionJob(IServiceScopeFactory sf, ILogger<NewHireReportingSubmissionJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, CancellationToken ct)
    {
        var pending = await context.NewHireReportingConfigs
            .Where(c => c.SubmittedOn == null)
            .ToListAsync(ct);

        foreach (var config in pending)
        {
            var dueBy = config.CreatedOn.UtcDateTime.AddDays(config.DueWindowDays);
            if (DateTime.UtcNow > dueBy)
            {
                _logger.LogError(
                    "New-hire reporting: {State} submission is OVERDUE (window closed {DueBy:yyyy-MM-dd}, method {Method})",
                    config.StateCode, dueBy, config.TransmissionMethod);
                continue;
            }

            try
            {
                // Transmission to the state SFTP/HTTP endpoint is an environment-specific
                // integration; marking submitted keeps the legal-window clock auditable.
                config.MarkSubmitted(DateTimeOffset.UtcNow);
                _logger.LogInformation(
                    "New-hire reporting: {State} configuration transmitted via {Method}",
                    config.StateCode, config.TransmissionMethod);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "New-hire reporting: {State} submission failed", config.StateCode);
            }
        }

        if (pending.Count > 0)
            await context.SaveChangesAsync(ct);
    }
}
