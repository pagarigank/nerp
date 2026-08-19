// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.GeneralLedger.Infrastructure.Posting;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.GeneralLedger;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeneralLedgerModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GlDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(GlDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "gl");
                });

            options.AddInterceptors(new ERP.Modules.Platform.Infrastructure.AuditSaveChangesInterceptor(
                sp.GetRequiredService<ERP.Modules.Platform.Infrastructure.ICurrentUserService>(), sp));
        });

        services.AddScoped(typeof(ERP.Modules.GeneralLedger.Infrastructure.IRepository<>), typeof(ERP.Modules.GeneralLedger.Infrastructure.Repository<>));
        services.AddScoped<ERP.Modules.GeneralLedger.Infrastructure.IUnitOfWork, ERP.Modules.GeneralLedger.Infrastructure.UnitOfWork>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IRevaluationService, RevaluationService>();
        services.AddScoped<IConsolidationService, ConsolidationService>();
        services.AddScoped<IGlPeriodCloseService, GlPeriodCloseService>();
        services.AddScoped<BatchPostingQueueProcessor>();
        services.AddScoped<ConsolidationJob>();

        // Canonical posting pipeline (architecture.md §5.1): sub-ledgers publish
        // CanonicalPostingEvents through IPostingEventPublisher; this module is the
        // only consumer and materializes them as posted JournalBatches in GL.
        services.AddScoped<IPostingEventConsumer, GlPostingEventConsumer>();
        services.AddScoped<IPostingEventPublisher, InProcessPostingEventPublisher>();

        return services;
    }
}
