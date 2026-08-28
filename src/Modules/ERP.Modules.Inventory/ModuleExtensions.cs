// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Application.Services;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Inventory.Infrastructure.Handlers;
using ERP.Modules.OrderManagement.Domain.Events;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Inventory;

public static class ModuleExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "inv"));
            options.AddInterceptors(new ERP.Modules.Platform.Infrastructure.AuditSaveChangesInterceptor(
                sp.GetRequiredService<ERP.Modules.Platform.Infrastructure.ICurrentUserService>(), sp));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<CostingService>();
        services.AddScoped<InventoryReportService>();
        services.AddScoped<LotSerialTrackingService>();
        services.AddScoped<ERP.Core.Common.IInventoryAvailability, InventoryAvailabilityService>();
        services.AddScoped<ERP.Core.Common.IInventoryReorderSource, InventoryReorderSource>();
        services.AddScoped<ERP.Core.Common.IInventoryItemLookup, InventoryItemLookup>();
        services.AddScoped<ERP.Core.Common.IComponentReservationService, ComponentReservationService>();

        // Inventory -> GL posting consumer (consumes InventoryTransactionPostedEvent
        // through the canonical posting contract, mirroring the AP/AR handlers).
        services.AddScoped<IDomainEventHandler<InventoryTransactionPostedEvent>, InventoryPostedToGlHandler>();

        // Inventory -> Stock ledger: maintain perpetual ItemStock on-hand balances
        // for receipt / issue / adjustment / transfer transactions.
        services.AddScoped<IDomainEventHandler<InventoryTransactionPostedEvent>, InventoryPostedToStockHandler>();

        // Purchasing -> Inventory integration: a posted goods receipt creates the
        // corresponding inventory receipt transactions (and, in turn, GL postings).
        services.AddScoped<IDomainEventHandler<GoodsReceivedEvent>, GoodsReceivedToInventoryHandler>();

        // Sales -> Inventory integration: a confirmed shipment relieves inventory
        // (COGS) and feeds the Inventory -> GL posting handler.
        services.AddScoped<IDomainEventHandler<ShipmentConfirmedEvent>, ShipmentConfirmedToInventoryHandler>();

        // Sales -> Inventory integration: a confirmed sales order allocates (reserves)
        // stock so concurrent orders cannot oversell the same inventory.
        services.AddScoped<IDomainEventHandler<SalesOrderConfirmedEvent>, SalesOrderConfirmedToInventoryHandler>();

        // Sales -> Inventory integration (reverse leg): a confirmed customer return (RMA)
        // restocks the returned items back into inventory.
        services.AddScoped<IDomainEventHandler<ReturnConfirmedEvent>, ReturnConfirmedToInventoryHandler>();

        return services;
    }
}

public interface IRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Delete(T entity);
}

public class Repository<T> : IRepository<T>
    where T : class
{
    private readonly InventoryDbContext _context;

    public Repository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<T>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _context;

    public UnitOfWork(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
