// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsPayable.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.AccountsPayable;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsPayableModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ap");
                });

            // Audit every AP write so the Separation-of-Duties engine has a real
            // activity trail to detect create/approve/post conflicts.
            options.AddInterceptors(new ERP.Modules.Platform.Infrastructure.AuditSaveChangesInterceptor(
                sp.GetRequiredService<ERP.Modules.Platform.Infrastructure.ICurrentUserService>(), sp));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IThreeWayMatchService, ThreeWayMatchService>();
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.Purchasing.Domain.Events.GoodsReceivedEvent>, ERP.Modules.AccountsPayable.Infrastructure.Handlers.GoodsReceivedToApHandler>();
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.OrderManagement.Domain.Events.ShipmentConfirmedEvent>, ShipmentConfirmedToCommissionHandler>();
        services.AddScoped<IForm1099Service, Form1099Service>();
        services.AddScoped<IBackupWithholdingService, BackupWithholdingService>();
        services.AddScoped<IAchFileService, AchFileService>();
        services.AddScoped<IApPhase3Service, ApPhase3Service>();
        services.AddScoped<CashRequirementsJob>();
        services.AddScoped<AchFileJob>();

        return services;
    }
}
