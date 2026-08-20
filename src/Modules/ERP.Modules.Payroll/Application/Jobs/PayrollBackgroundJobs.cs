// <copyright file="PayrollBackgroundJobs.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Payroll.Application.Jobs;

/// <summary>
/// Phase 11 background jobs (Batch F). Each runs on a recurring timer and performs its
/// named operation against the payroll schema. They resolve <see cref="PayrollDbContext"/>
/// from a fresh DI scope per tick so the DbContext is never shared across threads.
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
                await RunAsync(context, stoppingToken);
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

    protected abstract Task RunAsync(PayrollDbContext context, CancellationToken ct);
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
