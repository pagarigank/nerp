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
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<ITaxpayerProfileService, TaxpayerProfileService>();

        // Master data extension repositories
        services.AddScoped<IEInvoiceCustomerExtensionRepository, EInvoiceCustomerExtensionRepository>();
        services.AddScoped<IEInvoiceVendorExtensionRepository, EInvoiceVendorExtensionRepository>();
        services.AddScoped<IApiCredentialRepository, ApiCredentialRepository>();

        // Token cache (singleton — uses IMemoryCache for thread-safe caching)
        services.AddSingleton<IMyInvoisTokenCache, MyInvoisTokenCache>();

        // QR code, signing, and error mapping
        services.AddScoped<IQRCodeService, QRCodeService>();
        services.AddScoped<IXAdESSigningService, XAdESSigningService>();

        // Retry queue
        services.AddScoped<IEInvoiceRetryQueue, EInvoiceRetryQueue>();

        // Background jobs
        services.AddScoped<EInvoiceStatusPollingJob>();
        services.AddScoped<CertificateExpiryMonitorJob>();
        services.AddScoped<EInvoiceRetryJob>();

        return services;
    }
}