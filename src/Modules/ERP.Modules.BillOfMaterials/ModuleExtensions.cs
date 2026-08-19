// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.BillOfMaterials;

public static class ModuleExtensions
{
    public static IServiceCollection AddBillOfMaterialsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BomDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "bom"));
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        services.AddScoped(typeof(IBomRepository<>), typeof(BomRepository<>));
        services.AddScoped<IBomUnitOfWork, BomUnitOfWork>();

        return services;
    }
}

public interface IBomRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}

public class BomRepository<T> : IBomRepository<T> where T : class
{
    private readonly BomDbContext _context;
    public BomRepository(BomDbContext context) => _context = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Set<T>().ToListAsync(cancellationToken);

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<T>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public void Update(T entity) => _context.Set<T>().Update(entity);
    public void Delete(T entity) => _context.Set<T>().Remove(entity);
}

public interface IBomUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class BomUnitOfWork : IBomUnitOfWork
{
    private readonly BomDbContext _context;
    public BomUnitOfWork(BomDbContext context) => _context = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
