// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Integration.Application;
using ERP.Modules.Integration.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Integration;

public static class ModuleExtensions
{
    public static IServiceCollection AddIntegrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<IntegrationDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "int"));
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        // Background jobs
        services.AddHostedService<WebhookRetryJob>();
        services.AddHostedService<CertificateExpiryAlertJob>();
        services.AddHostedService<EdiAckTimeoutMonitorJob>();
        services.AddHostedService<ImportJobMonitorJob>();

        return services;
    }
}
