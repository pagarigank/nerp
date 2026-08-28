// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Infrastructure;
using ERP.Modules.Reporting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Reporting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReportingDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ReportingDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "rpt");
                });
        });

        // Reporting services
        services.AddScoped<IRowLevelSecurityService, RowLevelSecurityService>();
        services.AddScoped<IConsolidatedStatementService, ConsolidatedStatementService>();
        services.AddSingleton<IReportOutputCacheService, ReportOutputCacheService>();
        services.AddScoped<IScheduledDeliveryService, ScheduledDeliveryService>();
        services.AddScoped<ReportDeliveryJob>();
        services.AddScoped<ICdcEtlSyncService, CdcEtlSyncService>();
        services.AddScoped<ISearchIndexSyncService, SearchIndexSyncService>();
        services.AddScoped<IDataMartIntegrityService, DataMartIntegrityService>();
        services.AddScoped<IDeliveryRetryService, DeliveryRetryService>();
        services.AddScoped<DeliveryRetryJob>();

        return services;
    }
}
