// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.Integration.Application;
using ERP.Modules.Integration.Infrastructure;
using ERP.Modules.Integration.Infrastructure.Handlers;
using ERP.Modules.OrderManagement.Domain.Events;
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

        // Services
        services.AddSingleton<EdiX12Translator>();
        services.AddScoped<WebhookDispatchService>();
        services.AddScoped<BulkImportService>();
        services.AddScoped<InboundEdiProcessor>();
        services.AddScoped<OutboundEdiGenerator>();

        // Domain event handlers
        services.AddScoped<IDomainEventHandler<InvoiceBatchPostedEvent>, InvoicePostedToEdiHandler>();
        services.AddScoped<IDomainEventHandler<ShipmentConfirmedEvent>, ShipmentConfirmedToEdiHandler>();

        // Background jobs
        services.AddHostedService<WebhookRetryJob>();
        services.AddHostedService<CertificateExpiryAlertJob>();
        services.AddHostedService<EdiAckTimeoutMonitorJob>();
        services.AddHostedService<ImportJobMonitorJob>();

        return services;
    }
}
