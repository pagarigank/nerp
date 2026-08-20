// <copyright file="ModuleExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Payroll;

public static class ModuleExtensions
{
    public static IServiceCollection AddPayrollModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PayrollDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "pay"));
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUserService>(), sp));
        });

        services.AddScoped<IPayrollUnitOfWork, PayrollUnitOfWork>();

        return services;
    }
}

public interface IPayrollUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PayrollUnitOfWork : IPayrollUnitOfWork
{
    private readonly PayrollDbContext _context;
    public PayrollUnitOfWork(PayrollDbContext context) => _context = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
