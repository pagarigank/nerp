// <copyright file="Program.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, S1118

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsPayable;
using ERP.Modules.AccountsReceivable;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.BillOfMaterials;
using ERP.Modules.BillOfMaterials.Infrastructure.Jobs;
using ERP.Modules.CashManagement;
using ERP.Modules.CashManagement.Infrastructure;
using ERP.Modules.FieldService;
using ERP.Modules.GeneralLedger;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Inventory;
using ERP.Modules.Inventory.Application.BackgroundJobs;
using ERP.Modules.OrderManagement;
using ERP.Modules.OrderManagement.Infrastructure.Jobs;
using ERP.Modules.Payroll;
using ERP.Modules.Platform;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting;
using ERP.Modules.Purchasing;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Posting;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;

namespace ERP.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers(options =>
            {
                // Enforces super-admin (all companies) vs company-admin (own
                // company only) scoping on every company-targeted request.
                options.Filters.AddService<ERP.Modules.Platform.Infrastructure.CompanyAuthorizationFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.CustomSchemaIds(type => type.FullName ?? type.Name);
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "ERP API",
                Version = "v1",
                Description = "Modern Project-Centric ERP API"
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddHealthChecks()
            .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "sqlserver", failureStatus: null, tags: new[] { "ready" });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        builder.Services.AddAuthentication("LocalDev")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = builder.Configuration["Auth:Authority"];
                options.Audience = builder.Configuration["Auth:Audience"];
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
            })

            // Local / dev self-signed token scheme (username + password login).
            // Mirrors the Entra token shape so the same [Authorize] policies apply.
            .AddJwtBearer("LocalDev", options =>
            {
                var local = new ERP.Modules.Platform.Infrastructure.JwtTokenService(builder.Configuration);
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

                // Pin the classic JwtSecurityTokenHandler for the dev scheme. The
                // .NET 8 default JsonWebTokenHandler intermittently fails to parse
                // locally-issued tokens, and (more importantly) it strips custom
                // claim types from HttpContext.User. Company-scoped authorization
                // therefore resolves the user's allowed companies from the DB
                // (via the sub claim), not from token claims.
                options.UseSecurityTokenValidators = true;
#pragma warning disable CS0618 // SecurityTokenValidators is the reliable path for the dev scheme
                options.SecurityTokenValidators.Clear();
                options.SecurityTokenValidators.Add(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler());
#pragma warning restore CS0618
                options.TokenValidationParameters = local.LocalValidationParameters;
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var auth = context.Request.Headers["Authorization"].ToString();
                        var token = auth.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase)
                            ? auth.Substring(7).Trim()
                            : auth.Trim();
                        Log.Warning("LocalDev OnMessageReceived: header-len={Len} token-len={TLen} dots={Dots} token-prefix={Prefix}", auth.Length, token.Length, token.Count(c => c == '.'), token.Length > 12 ? token.Substring(0, 12) : token);
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            context.Token = token;
                        }

                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Log.Warning(context.Exception, "LocalDev token validation failed");
                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddSingleton<ERP.Modules.Platform.Infrastructure.JwtTokenService>();

        // Require an authenticated principal for every endpoint by default.
        // Controllers may opt out with [AllowAnonymous]. This enforces the
        // Cross-Cutting "Authorization check" DoD item that was previously
        // missing (auth middleware ran but no endpoint demanded a principal).
        // In the Test environment we relax this so the xUnit integration
        // harness (no bearer token) can exercise endpoints directly.
        if (!builder.Environment.IsEnvironment("Test"))
        {
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder("Bearer", "LocalDev")
                    .RequireAuthenticatedUser()
                    .Build();

                // Company-admin OR super-admin: permits management of a tenant's
                // own users / roles / settings. A company admin is identified by
                // the "company_admin" claim (scoped to their company); a super
                // admin by "super_admin" or a "*" company_scope claim.
                options.AddPolicy("CompanyAdminOrSuper", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => c.Type == "super_admin" && c.Value == "true")
                        || context.User.HasClaim(c => c.Type == "company_scope" && c.Value == "*")
                        || context.User.HasClaim(c => c.Type == "company_admin" && c.Value == "true")));
            });
        }
        else
        {
            builder.Services.AddAuthorization();
        }

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            options.AddPolicy("api", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/erp-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

        builder.Services.AddMemoryCache();
        
        var redisConnection = builder.Configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "ERP:";
            });
        }
        else
        {
            builder.Services.AddDistributedMemoryCache();
        }

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Program).Assembly,
                typeof(ERP.Core.Domain.Common.AggregateRoot).Assembly,
                typeof(ERP.Shared.Kernel.Events.DispatchableDbContext).Assembly,
                typeof(ERP.Modules.AccountsPayable.Infrastructure.ApDbContext).Assembly,
                typeof(ERP.Modules.AccountsReceivable.Infrastructure.ArDbContext).Assembly,
                typeof(ERP.Modules.CashManagement.Infrastructure.CashDbContext).Assembly,
                typeof(ERP.Modules.GeneralLedger.Infrastructure.GlDbContext).Assembly,
                typeof(ERP.Modules.Inventory.Infrastructure.InventoryDbContext).Assembly,
                typeof(ERP.Modules.Platform.Infrastructure.PlatformDbContext).Assembly,
                typeof(ERP.Modules.Purchasing.Infrastructure.PurchasingDbContext).Assembly);
        });
        builder.Services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly });

        // Domain-event dispatcher: resolves handlers (IDomainEventHandler<T>)
        // registered by each module and invokes them when a tracked aggregate
        // raises an event during SaveChanges.
        builder.Services.AddScoped<ERP.Core.Domain.Events.IDomainEventDispatcher, ERP.Shared.Kernel.Events.DomainEventDispatcher>();

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")!));
        builder.Services.AddHangfireServer();

        builder.Services.AddPlatformModule(builder.Configuration);
        builder.Services.AddGeneralLedgerModule(builder.Configuration);
        builder.Services.AddAccountsPayableModule(builder.Configuration);
        builder.Services.AddAccountsReceivableModule(builder.Configuration);
        builder.Services.AddCashManagementModule(builder.Configuration);
        builder.Services.AddPurchasingModule(builder.Configuration);
        builder.Services.AddInventoryModule(builder.Configuration);
        builder.Services.AddOrderManagementModule(builder.Configuration);
        builder.Services.AddBillOfMaterialsModule(builder.Configuration);
        builder.Services.AddProjectAccountingModule(builder.Configuration);
        builder.Services.AddPayrollModule(builder.Configuration);
        builder.Services.AddFieldServiceModule(builder.Configuration);

        var app = builder.Build();

        // Configure Hangfire recurring jobs
        using (var scope = app.Services.CreateScope())
        {
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
            recurringJobManager.AddOrUpdate<IExchangeRateProvider>(
                "exchange-rate-refresh",
                provider => provider.RefreshRatesAsync(CancellationToken.None),
                Cron.Daily(2, 0), // Run daily at 2:00 AM
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<BatchPostingQueueProcessor>(
                "batch-posting-queue",
                processor => processor.ProcessAsync(CancellationToken.None),
                "*/5 * * * *", // Every 5 minutes
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.GeneralLedger.Infrastructure.ConsolidationJob>(
                "consolidation-run",
                job => job.ExecuteScheduledConsolidationAsync(Guid.Empty, 0, 0, CancellationToken.None),
                Cron.Monthly(1, 3, 0), // 1st of month at 3:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.AccountsPayable.Infrastructure.CashRequirementsJob>(
                "cash-requirements",
                job => job.GenerateCashRequirementsAsync(CancellationToken.None),
                Cron.Daily(6, 0), // Daily at 6:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.AccountsPayable.Infrastructure.AchFileJob>(
                "ach-file-generation",
                job => job.GenerateAndTransmitAchFilesAsync(CancellationToken.None),
                Cron.Daily(7, 0), // Daily at 7:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<StatementGenerationJob>(
                "ar-statement-generation",
                job => job.GenerateStatementsAsync(Guid.Empty, CancellationToken.None),
                Cron.Daily(8, 0), // Daily at 8:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<FinanceChargeJob>(
                "ar-finance-charge",
                job => job.CalculateFinanceChargesAsync(Guid.Empty, 18.0m, CancellationToken.None),
                Cron.Monthly(1, 2, 0), // 1st of month at 2:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ICashPositionJob>(
                "cash-position",
                job => job.RunAsync(Guid.Empty, CancellationToken.None),
                Cron.Daily(5, 30), // Daily at 5:30 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<IOutstandingCheckAgingJob>(
                "outstanding-check-aging",
                job => job.RunAsync(Guid.Empty, Guid.Empty, CancellationToken.None),
                Cron.Weekly(DayOfWeek.Monday, 6, 0), // Weekly Monday 6:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<IBankStatementDownloadJob>(
                "cash-statement-download",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(4, 30), // Daily at 4:30 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<IReorderPointScanJob>(
                "purchasing-reorder-scan",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(2, 0), // Nightly at 2:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<IBackorderProcessingJob>(
                "om-backorder-processing",
                job => job.RunAsync(CancellationToken.None),
                Cron.Hourly, // Hourly: release backorders as inventory arrives
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<IBomValidationJob>(
                "bom-validation-nightly",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(2, 30), // Nightly at 2:30 AM UTC: cycles, inactive components, cost anomalies
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ICostRollupJob>(
                "bom-cost-rollup-weekly",
                job => job.RunAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Sunday, 3, 0), // Sundays 3:00 AM UTC: standard-cost recalculation
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.ProjectAccounting.Infrastructure.Jobs.ICostPostingProcessorJob>(
                "proj-cost-posting-processor",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(1, 0), // Daily at 1:00 AM UTC: project cost posting health report
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.ProjectAccounting.Infrastructure.Jobs.IAllocatorRunJob>(
                "proj-allocator-run",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(1, 30), // Daily at 1:30 AM UTC: apply burden/markup to unallocated costs
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.ProjectAccounting.Infrastructure.Jobs.IEacRecalculationJob>(
                "proj-eac-recalculation",
                job => job.RunAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Saturday, 4, 0), // Saturdays 4:00 AM UTC: EAC recalc + snapshot capture
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.ProjectAccounting.Infrastructure.Jobs.IWipScheduleGenerationJob>(
                "proj-wip-schedule",
                job => job.RunAsync(CancellationToken.None),
                "0 5 1 * *", // Monthly on the 1st at 5:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.ProjectAccounting.Infrastructure.Jobs.IGlReconciliationCheckJob>(
                "proj-gl-reconciliation-check",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(6, 0), // Daily at 6:00 AM UTC: alert on project-to-GL variance
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ERP.Modules.ProjectAccounting.Infrastructure.Jobs.IPerformanceAlertJob>(
                "proj-performance-alerts",
                job => job.RunAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Monday, 7, 0), // Mondays 7:00 AM UTC: over-budget / negative margin / slip alerts
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ICommissionRunJob>(
                "om-commission-run",
                job => job.RunAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Monday, 1, 0), // Mondays 1:00 AM UTC, prior ISO week
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ICreditHoldReviewJob>(
                "om-credit-hold-review",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(3, 0), // Daily at 3:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<IShipmentTrackingUpdateJob>(
                "om-shipment-tracking-update",
                job => job.RunAsync(CancellationToken.None),
                "0 */4 * * *", // Every 4 hours
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            // Inventory recurring jobs
            recurringJobManager.AddOrUpdate<ValuationSnapshotJob>(
                "inventory-valuation-snapshot",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(3, 0), // Daily at 3:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<SlowMovingJob>(
                "inventory-slow-moving",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Sunday, 4, 0), // Weekly Sunday 4:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ReorderAlertJob>(
                "inventory-reorder-alert",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(5, 0), // Daily at 5:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<LotExpirationAlertJob>(
                "inventory-lot-expiration-alert",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(6, 0), // Daily at 6:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<CostRecalculationJob>(
                "inventory-cost-recalculation",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Saturday, 2, 0), // Weekly Saturday 2:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            recurringJobManager.AddOrUpdate<ABCClassificationJob>(
                "inventory-abc-classification",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Monthly(1, 1, 0), // 1st of month at 1:00 AM UTC
                new Hangfire.RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API v1");
                c.RoutePrefix = "swagger";
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
            });
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("DefaultPolicy");

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("User", httpContext.User.Identity?.Name ?? "Anonymous");
            };
        });

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<CurrentUserMiddleware>();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers();
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });
        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            // Previously wide open (filter returned true). Now requires an
            // authenticated admin, except in the Test environment where the
            // integration harness drives the app without a signed-in principal.
            Authorization = new[] { new HangfireAuthorizationFilter(app.Environment) }
        });

        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // Apply any pending migrations on startup (idempotent) so the local DB
        // is always in sync with the model without a manual step. Every module
        // DbContext is migrated, not just Platform.
        foreach (var contextType in new[]
        {
            typeof(ERP.Modules.Platform.Infrastructure.PlatformDbContext),
            typeof(ERP.Modules.GeneralLedger.Infrastructure.GlDbContext),
            typeof(ERP.Modules.AccountsPayable.Infrastructure.ApDbContext),
            typeof(ERP.Modules.AccountsReceivable.Infrastructure.ArDbContext),
            typeof(ERP.Modules.CashManagement.Infrastructure.CashDbContext),
            typeof(ERP.Modules.Purchasing.Infrastructure.PurchasingDbContext),
            typeof(ERP.Modules.Inventory.Infrastructure.InventoryDbContext),
            typeof(ERP.Modules.OrderManagement.Infrastructure.OmDbContext),
            typeof(ERP.Modules.FieldService.Infrastructure.FieldServiceDbContext),
        })
        {
            using (var migrateScope = app.Services.CreateScope())
            {
                try
                {
                    var db = (Microsoft.EntityFrameworkCore.DbContext)migrateScope.ServiceProvider.GetRequiredService(contextType);
                    db.Database.MigrateAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Migration on startup failed for {Context}", contextType.Name);
                }
            }
        }

        // Local / dev seed: Admin role, demo user, sample companies + periods.
        if (app.Environment.IsDevelopment())
        {
            try
            {
                ERP.Modules.Platform.Infrastructure.DevSeed.SeedAsync(app.Services, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Dev seed failed");
            }
        }

        app.Run();
    }
}

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await this.next(context);
        }
    }
}

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this.next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            this.logger.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            IReadOnlyDictionary<string, string[]>? errors = null;
            if (ex is FluentValidation.ValidationException vex)
            {
                errors = vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            }

            var response = new ProblemDetailsResponse
            {
                Type = ex.GetType().Name,
                Title = ex switch
                {
                    FluentValidation.ValidationException => "Validation Failed",
                    UnauthorizedAccessException => "Unauthorized",
                    KeyNotFoundException => "Not Found",
                    Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => "Concurrency Conflict",
                    _ => "An error occurred"
                },
                Status = context.Response.StatusCode,
                Detail = ex.Message,
                Instance = context.Request.Path,
                TraceId = correlationId,
                Errors = errors
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _environment;

    public HangfireAuthorizationFilter(IWebHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public bool Authorize(DashboardContext context)
    {
        // The integration test harness runs without a signed-in principal.
        if (_environment.IsEnvironment("Test"))
            return true;

        var http = context.GetHttpContext();
        return http.User.Identity is { IsAuthenticated: true }
            && (http.User.IsInRole("Admin") || http.User.IsInRole("SystemAdmin"));
    }
}
