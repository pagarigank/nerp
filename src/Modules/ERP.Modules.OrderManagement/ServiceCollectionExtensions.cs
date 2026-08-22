// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.OrderManagement.Application.Services;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.OrderManagement.Infrastructure.Jobs;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.OrderManagement;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OmDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(OmDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "om");
                });

            // Audit every OM write so the Separation-of-Duties engine has a real
            // activity trail to detect create/approve/post conflicts on sales orders
            // and shipments.
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.OrderManagement.Domain.Events.ShipmentConfirmedEvent>, Infrastructure.Handlers.ShipmentConfirmedToOmHandler>();

        services.AddScoped<SalesReportService>();

        // Phase 8 background jobs (items 593-596). Registered like Purchasing's
        // LateDeliveryAlertJob / ReorderPointScanJob so a Hangfire recurring job
        // can invoke RunAsync per schedule; see module docs for recommended crons.
        services.AddScoped<IBackorderProcessingJob, BackorderProcessingJob>();
        services.AddScoped<ICommissionRunJob, CommissionRunJob>();
        services.AddScoped<ICreditHoldReviewJob, CreditHoldReviewJob>();
        services.AddScoped<IShipmentTrackingUpdateJob, ShipmentTrackingUpdateJob>();
        services.AddHttpClient("carrier-tracking");

        return services;
    }
}
