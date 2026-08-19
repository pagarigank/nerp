// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Modules.ProjectAccounting.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.ProjectAccounting;

public static class ModuleExtensions
{
    public static IServiceCollection AddProjectAccountingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProjDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "proj"));
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        services.AddScoped<IProjUnitOfWork, ProjUnitOfWork>();
        services.AddScoped<ERP.Modules.ProjectAccounting.Domain.Services.IProjectAllocator, ERP.Modules.ProjectAccounting.Domain.Services.ProjectAllocator>();

        // Dual-posting consumers: project cost -> GL, and inventory issue against a
        // project -> project ledger cost transaction. (Phase 10 critical gap.)
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.ProjectAccounting.Domain.Events.ProjectCostPostedEvent>, CostTransactionDualPostingHandler>();
        services.AddScoped<ERP.Core.Domain.Common.IDomainEventHandler<ERP.Modules.Inventory.Domain.Events.InventoryTransactionPostedEvent>, InventoryPostedToProjectHandler>();

        return services;
    }
}

public interface IProjUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class ProjUnitOfWork : IProjUnitOfWork
{
    private readonly ProjDbContext _context;
    public ProjUnitOfWork(ProjDbContext context) => _context = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
