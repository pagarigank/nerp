// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Infrastructure;
using ERP.Modules.Purchasing.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Purchasing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPurchasingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PurchasingDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "pur"));
            options.AddInterceptors(new ERP.Modules.Platform.Infrastructure.AuditSaveChangesInterceptor(
                sp.GetRequiredService<ERP.Modules.Platform.Infrastructure.ICurrentUserService>(), sp));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRequisitionToPOService, RequisitionToPOService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IReorderPointScanJob, ReorderPointScanJob>();

        // Consumes Order Management sales-order confirmations to auto-create DropShip
        // purchase orders (Sales -> Purchasing leg of the integrated flow).
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.OrderManagement.Domain.Events.SalesOrderConfirmedEvent>, SalesOrderConfirmedToPoHandler>();

        return services;
    }
}
