// <copyright file="ServiceCollectionExtensions.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.EInvoice;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEInvoiceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EInvoiceDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(EInvoiceDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "einv");
                });
        });

        // MyInvois API client and related services
        services.AddHttpClient<IMyInvoisApiClient, MyInvoisApiClient>();
        services.AddScoped<IEInvoiceService, EInvoiceService>();

        return services;
    }
}