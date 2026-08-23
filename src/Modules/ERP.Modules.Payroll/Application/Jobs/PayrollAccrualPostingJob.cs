// <copyright file="PayrollAccrualPostingJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Payroll.Application.Jobs;

/// <summary>
/// Period-end payroll accrual: computes approved-but-unpaid timesheet gross plus the
/// employer-tax burden per company and posts it through the SAME canonical posting
/// pipeline payroll run finals use, tagged for reversal next period. The open accrual
/// is watermarked on CompanyPayrollSetup (no new table); the next tick posts the exact
/// reversing entry first, then accrues afresh.
/// </summary>
public class PayrollAccrualPostingJob : PayRecurringJob
{
    private const string SourceModule = "PAY";
    private const string ReversalTag = "accrualReversalNextPeriod";

    public PayrollAccrualPostingJob(IServiceScopeFactory sf, ILogger<PayrollAccrualPostingJob> l)
        : base(sf, l, TimeSpan.FromDays(1)) { }

    protected override async Task RunAsync(PayrollDbContext context, IServiceProvider scopedServices, CancellationToken ct)
    {
        var platform = scopedServices.GetRequiredService<PlatformDbContext>();
        var publisher = scopedServices.GetRequiredService<IPostingEventPublisher>();

        var setups = await context.CompanyPayrollSetups.ToListAsync(ct);
        foreach (var setup in setups)
        {
            if (setup.OpenAccrualAmount.HasValue && setup.OpenAccrualAmount.Value != 0m)
            {
                await ReverseOpenAccrualAsync(publisher, platform, setup, ct);
                await context.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Payroll accrual reversal posted for company {CompanyId} ({BatchRef})",
                    setup.CompanyId, setup.OpenAccrualBatchRef ?? "(cleared)");
            }

            var accrual = await ComputeAccrualAsync(context, setup, ct);
            if (accrual.Total > 0m)
            {
                await PostAccrualAsync(publisher, platform, setup, accrual.Gross, accrual.EmployerTax, ct);
                await context.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Payroll accrual posted for company {CompanyId}: gross {Gross}, employer tax {EmployerTax}",
                    setup.CompanyId, accrual.Gross, accrual.EmployerTax);
            }
        }
    }

    /// <summary>Posts the period-end accrual entry through the canonical GL posting contract.</summary>
    public static async Task PostAccrualAsync(
        IPostingEventPublisher publisher,
        PlatformDbContext platform,
        CompanyPayrollSetup setup,
        decimal gross,
        decimal employerTax,
        CancellationToken ct)
    {
        var batchNumber = $"PAYROLL-ACCRUAL-AUTO-{DateTime.UtcNow:yyyyMMdd}-{setup.CompanyId.ToString("N")[..8]}";
        var postingDate = DateTimeOffset.UtcNow;
        var fiscalPeriodId = await ResolveFiscalPeriodIdAsync(platform, setup.CompanyId, postingDate.UtcDateTime, ct) ?? setup.Id;

        var segments = AccountKey.Create();
        var lines = new List<PostingLine>();
        AddLine(lines, setup.WageExpenseAccountId, gross, 0m, segments);
        AddLine(lines, setup.PayrollTaxExpenseAccountId, employerTax, 0m, segments);
        AddLine(lines, setup.PayrollLiabilityAccountId, 0m, gross + employerTax, segments);

        var postingEvent = CanonicalPostingEvent.Create(
            SourceModule,
            batchNumber,
            setup.CompanyId,
            fiscalPeriodId,
            setup.CompanyId.ToString("N"),
            fiscalPeriodId.ToString("N"),
            postingDate,
            lines,
            BuildMetadata(
                setup.CompanyId,
                (ReversalTag, "true"),
                ("gross", gross.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                ("employerTax", employerTax.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))));

        await publisher.PublishAsync(postingEvent, ct);
        setup.SetOpenAccrual(gross + employerTax, employerTax, postingDate, batchNumber);
    }

    private static async Task ReverseOpenAccrualAsync(
        IPostingEventPublisher publisher,
        PlatformDbContext platform,
        CompanyPayrollSetup setup,
        CancellationToken ct)
    {
        var total = setup.OpenAccrualAmount!.Value;
        var employerTax = setup.OpenAccrualEmployerTax ?? 0m;
        var gross = Math.Max(0m, total - employerTax);
        var batchNumber = $"PAYROLL-ACCRUAL-REV-{DateTime.UtcNow:yyyyMMdd}-{setup.CompanyId.ToString("N")[..8]}";
        var postingDate = DateTimeOffset.UtcNow;
        var priorBatchRef = setup.OpenAccrualBatchRef ?? string.Empty;
        var fiscalPeriodId = await ResolveFiscalPeriodIdAsync(platform, setup.CompanyId, postingDate.UtcDateTime, ct) ?? setup.Id;

        var segments = AccountKey.Create();
        var lines = new List<PostingLine>();
        AddLine(lines, setup.PayrollLiabilityAccountId, total, 0m, segments);
        AddLine(lines, setup.WageExpenseAccountId, 0m, gross, segments);
        if (employerTax > 0m)
            AddLine(lines, setup.PayrollTaxExpenseAccountId, 0m, employerTax, segments);

        var postingEvent = CanonicalPostingEvent.Create(
            SourceModule,
            batchNumber,
            setup.CompanyId,
            fiscalPeriodId,
            setup.CompanyId.ToString("N"),
            fiscalPeriodId.ToString("N"),
            postingDate,
            lines,
            BuildMetadata(setup.CompanyId, ("reversesBatch", priorBatchRef)));

        await publisher.PublishAsync(postingEvent, ct);
        setup.ClearOpenAccrual();
    }

    private static async Task<(decimal Gross, decimal EmployerTax, decimal Total)> ComputeAccrualAsync(
        PayrollDbContext context, CompanyPayrollSetup setup, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        var approvedGross = await context.TimesheetLines
            .Join(context.Timesheets.Where(t => t.CompanyId == setup.CompanyId
                                                && t.Status == TimesheetStatus.Approved
                                                && t.WeekEnding < weekStart),
                l => l.TimesheetId,
                t => t.Id,
                (l, _) => l.Hours * l.Rate)
            .SumAsync(v => (decimal?)v, ct) ?? 0m;

        var postedGrossThroughCutoff = await context.PayrollRunLines
            .Where(l => l.PayrollRun != null
                        && l.PayrollRun.Status == PayrollRunStatus.Posted
                        && l.PayrollRun.CompanyId == setup.CompanyId
                        && l.PayrollRun.PeriodEnd < weekStart)
            .SumAsync(l => (decimal?)l.GrossPay, ct) ?? 0m;

        var unpaidGross = Math.Max(0m, Math.Round(approvedGross - postedGrossThroughCutoff, 2));
        var employerRate = setup.SocialSecurityRate + setup.MedicareRate + setup.FutaRate + setup.SutaRate;
        var employerTax = Math.Round(unpaidGross * employerRate, 2);
        return (unpaidGross, employerTax, unpaidGross + employerTax);
    }

    private static void AddLine(List<PostingLine> lines, Guid accountId, decimal debit, decimal credit, AccountKey segments)
    {
        if (debit == 0m && credit == 0m)
            return;
        lines.Add(new PostingLine { AccountId = accountId, Segments = segments, Debit = debit, Credit = credit, Currency = "USD" });
    }

    private static PostingMetadata BuildMetadata(Guid companyId, params (string Key, string Value)[] tags)
    {
        var custom = new Dictionary<string, string> { ["companyId"] = companyId.ToString() };
        foreach (var (key, value) in tags)
            custom[key] = value;
        return PostingMetadata.Create("system", Guid.NewGuid()) with { CustomProperties = custom };
    }

    private static async Task<Guid?> ResolveFiscalPeriodIdAsync(
        PlatformDbContext platform, Guid companyId, DateTime date, CancellationToken ct)
    {
        var d = new DateTimeOffset(date);
        return await platform.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.StartDate <= d && p.EndDate >= d)
            .OrderBy(p => p.StartDate)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
    }
}
