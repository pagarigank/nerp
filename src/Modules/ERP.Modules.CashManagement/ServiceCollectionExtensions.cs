// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.CashManagement.Infrastructure;
using ERP.Modules.CashManagement.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.CashManagement;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCashManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CashDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(CashDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "cash");
                });
        });

        services.AddScoped<IBankStatementParserService, BankStatementParserService>();
        services.AddScoped<IAutoMatchService, AutoMatchService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IPositivePayService, PositivePayService>();
        services.AddScoped<INsfService, NsfService>();
        services.AddScoped<IBankFeeService, BankFeeService>();
        services.AddScoped<ICashPositionJob, CashPositionJob>();
        services.AddScoped<IOutstandingCheckAgingJob, OutstandingCheckAgingJob>();
        services.AddScoped<IBankStatementDownloadJob, BankStatementDownloadJob>();
        services.AddHttpClient("bank-feeds");

        // Phase 11 item #1102: payroll -> Cash Management reconciliation. Consumes the
        // PayrollPostedEvent (shared ERP.Core contract) to record issued pay instruments.
        services.AddScoped<IDomainEventHandler<PayrollPostedEvent>, PayrollPostedToCashHandler>();

        return services;
    }
}
