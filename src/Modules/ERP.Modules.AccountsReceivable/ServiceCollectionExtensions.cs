// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.AccountsReceivable.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.AccountsReceivable;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsReceivableModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ArDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ArDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ar");
                });

            // Audit every AR write so the Separation-of-Duties engine has a real
            // activity trail to detect create/approve/post conflicts.
            options.AddInterceptors(new ERP.Modules.Platform.Infrastructure.AuditSaveChangesInterceptor(
                sp.GetRequiredService<ERP.Modules.Platform.Infrastructure.ICurrentUserService>(), sp));
        });

        services.AddScoped<ICreditLimitCheckService, CreditLimitCheckService>();

        // Expose the shared ERP.Core contract so the Order Management module can enforce
        // credit policy without a compile-time dependency on this module.
        services.AddScoped<ERP.Core.Common.ICreditLimitCheck, CreditLimitCheckService>();
        services.AddScoped<IStatementGenerationService, StatementGenerationService>();
        services.AddScoped<IFinanceChargeService, FinanceChargeService>();
        services.AddScoped<IAutoCashApplicationService, AutoCashApplicationService>();
        services.AddScoped<StatementGenerationJob>();
        services.AddScoped<FinanceChargeJob>();

        // Domain-event handler: posts AR invoice batches to the General Ledger
        // through the canonical posting contract. Registered as the closed
        // generic interface so the DomainEventDispatcher can resolve it.
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<InvoiceBatchPostedEvent>, InvoicePostedToGlHandler>();

        // Consumes Order Management shipment confirmations to generate the
        // customer invoice (Sales -> AR -> GL leg of the integrated flow).
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.OrderManagement.Domain.Events.ShipmentConfirmedEvent>, ShipmentConfirmedToArHandler>();

        // Consumes Order Management return (RMA) confirmations to generate a credit
        // memo and post it to GL (reverse leg of the Sales -> AR integration).
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.OrderManagement.Domain.Events.ReturnConfirmedEvent>, ReturnConfirmedToArHandler>();

        return services;
    }
}
