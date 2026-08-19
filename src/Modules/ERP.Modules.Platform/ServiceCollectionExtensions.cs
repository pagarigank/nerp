// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Platform;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "platform");
                });

            options.AddInterceptors(new AuditSaveChangesInterceptor(sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        services.AddHttpClient<IExchangeRateProvider, ExchangeRateProvider>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISegmentValidationService, SegmentValidationService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<ISodService, SodService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ERP.Modules.Platform.Infrastructure.CompanyAuthorizationFilter>();

        return services;
    }

    public static IServiceCollection AddPlatformJobs(this IServiceCollection services)
    {
        // Hangfire recurring jobs are configured in Program.cs after module registration
        return services;
    }
}
