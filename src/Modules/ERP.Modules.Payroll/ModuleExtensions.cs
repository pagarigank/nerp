// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Payroll;

public static class ModuleExtensions
{
    public static IServiceCollection AddPayrollModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PayrollDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "pay"));
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        services.AddScoped<IPayrollUnitOfWork, PayrollUnitOfWork>();

        // Phase 11 cross-module wiring (#1101): creates an AP voucher (employee as payee)
        // when an expense report is reimbursed. Depends on AP's IVoucherService (Payroll
        // holds a one-way reference to AP; no module cycle).
        services.AddScoped<ApVoucherCreator>();

        // Phase 11 background jobs (Batch F): scheduled payroll operations.
        services.AddHostedService<Application.Jobs.PayrollRunTriggerJob>();
        services.AddHostedService<Application.Jobs.PtoAccrualJob>();
        services.AddHostedService<Application.Jobs.TaxDepositReminderJob>();
        services.AddHostedService<Application.Jobs.YearEndProcessingJob>();
        services.AddHostedService<Application.Jobs.AchReturnMonitorJob>();
        services.AddHostedService<Application.Jobs.BenefitRemittanceJob>();

        // Phase 11 background jobs (Batch G): remaining payroll operations.
        services.AddHostedService<Application.Jobs.BiWeeklyTaxTableUpdateCheckJob>();
        services.AddHostedService<Application.Jobs.QuarterlyFilingReminderJob>();
        services.AddHostedService<Application.Jobs.AnnualW2GenerationPrepJob>();
        services.AddHostedService<Application.Jobs.WeeklyTimesheetReminderJob>();
        services.AddHostedService<Application.Jobs.NewHireReportingSubmissionJob>();
        services.AddHostedService<Application.Jobs.PayrollAccrualPostingJob>();

        return services;
    }
}

public interface IPayrollUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PayrollUnitOfWork : IPayrollUnitOfWork
{
    private readonly PayrollDbContext _context;
    public PayrollUnitOfWork(PayrollDbContext context) => _context = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
