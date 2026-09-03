// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.MalaysianTax;

/// <summary>
/// Extension methods to register the Malaysian Tax module.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMalaysianTaxModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Tax DbContext
        services.AddDbContext<MalaysianTaxDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(MalaysianTaxDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "tax");
                });
        });

        // Register repositories
        services.AddScoped<ITaxCodeRepository, TaxCodeRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<ICompanySstRegistrationRepository, CompanySstRegistrationRepository>();
        services.AddScoped<ITaxDeterminationRuleRepository, TaxDeterminationRuleRepository>();

        // Register repositories
        services.AddScoped<ISstReturnRepository, SstReturnRepository>();

        // Register services
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<ISstReturnService, SstReturnService>();

        return services;
    }
}