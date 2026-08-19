// <copyright file="IntegrationTestBase.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Data.Common;
using Xunit;
using ERP.Core.Domain.Common;
using ERP.Modules.Platform;
using ERP.Modules.AccountsPayable;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.GeneralLedger;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Inventory;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.OrderManagement;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing;
using ERP.Modules.Purchasing.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ERP.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected SqlServerTestContainer SqlContainer { get; } = new();

    public async Task InitializeAsync()
    {
        await SqlContainer.InitializeAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = SqlContainer.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        services.AddDbContext<PlatformDbContext>(options =>
        {
            options.UseSqlServer(SqlContainer.GetConnectionString(), sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "platform");
            });
        });

        services.AddDbContext<GlDbContext>(options =>
        {
            options.UseSqlServer(SqlContainer.GetConnectionString(), sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(GlDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "gl");
            });
        });

        services.AddDbContext<ApDbContext>((sp, options) =>
        {
            options.UseSqlServer(SqlContainer.GetConnectionString(), sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ap");
            });

            // Keep the audit interceptor in parity with the real module registration
            // so the SoD engine has a real activity trail in the test harness.
            options.AddInterceptors(new ERP.Modules.Platform.Infrastructure.AuditSaveChangesInterceptor(
                sp.GetRequiredService<ERP.Modules.Platform.Infrastructure.ICurrentUserService>(), sp));
        });

        services.AddScoped(typeof(ERP.Modules.Platform.Infrastructure.IRepository<>), typeof(ERP.Modules.Platform.Infrastructure.Repository<>));
        services.AddScoped<ERP.Modules.Platform.Infrastructure.IUnitOfWork, ERP.Modules.Platform.Infrastructure.UnitOfWork>();
        services.AddScoped<ERP.Modules.GeneralLedger.Infrastructure.IUnitOfWork, ERP.Modules.GeneralLedger.Infrastructure.UnitOfWork>();
        services.AddScoped<ERP.Modules.AccountsPayable.Infrastructure.IUnitOfWork, ERP.Modules.AccountsPayable.Infrastructure.UnitOfWork>();

        // Impersonatable current-user so tests can exercise SoD / audit-author
        // scenarios (the real app resolves this from the auth middleware).
        services.AddScoped<ERP.Modules.Platform.Infrastructure.ICurrentUserService, ERP.Modules.Platform.Infrastructure.CurrentUserService>();

        services.AddPlatformModule(configuration);
        services.AddGeneralLedgerModule(configuration);
        services.AddAccountsPayableModule(configuration);
        services.AddAccountsReceivableModule(configuration);
        services.AddInventoryModule(configuration);
        services.AddPurchasingModule(configuration);
        services.AddOrderManagementModule(configuration);

        // Domain-event dispatcher + handlers (registered in ERP.Api/Program.cs
        // for the real app; the integration harness builds its own container, so
        // register it here too, otherwise DispatchableDbContext has no dispatcher
        // and sub-ledger -> GL events are never consumed).
        services.AddScoped<ERP.Core.Domain.Events.IDomainEventDispatcher, ERP.Shared.Kernel.Events.DomainEventDispatcher>();

        ServiceProvider = services.BuildServiceProvider();

        // Apply migrations
        await ApplyMigrationsAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = ServiceProvider.CreateScope();

        // Several module DbContexts share Platform reference entities (e.g.
        // Account), so each independently generates a migration that creates the
        // same platform.Accounts table. When all module migrations are applied to
        // one database this produces benign "already exists" collisions. We apply
        // per-context and tolerate those so every module's own tables (JournalBatches,
        // InvoiceBatches, etc.) still get created.
        var contexts = new DbContext[]
        {
            scope.ServiceProvider.GetRequiredService<PlatformDbContext>(),
            scope.ServiceProvider.GetRequiredService<GlDbContext>(),
            scope.ServiceProvider.GetRequiredService<ApDbContext>(),
            scope.ServiceProvider.GetRequiredService<ArDbContext>(),
            scope.ServiceProvider.GetRequiredService<InventoryDbContext>(),
            scope.ServiceProvider.GetRequiredService<PurchasingDbContext>(),
            scope.ServiceProvider.GetRequiredService<OmDbContext>(),
        };

        foreach (var context in contexts)
        {
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception ex) when (IsBenignMigrationCollision(ex))
            {
                // Redundant object/column already created by another module's
                // migration; the tables this context needs are already present.
            }
        }
    }

    private static bool IsBenignMigrationCollision(Exception ex)
    {
        // SQL Server: 2705 = column already exists, 2714 = object already exists,
        // 1913 = could not create constraint (object already exists).
        var sql = ex as Microsoft.Data.SqlClient.SqlException;
        if (sql is not null)
        {
            foreach (Microsoft.Data.SqlClient.SqlError error in sql.Errors)
            {
                if (error.Number is 2705 or 2714 or 1913)
                    return true;
            }
        }

        return ex.InnerException is not null && IsBenignMigrationCollision(ex.InnerException);
    }

    public async Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        await SqlContainer.DisposeAsync();
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected async Task ExecuteInTransactionAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        var strategy = scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.BeginTransactionAsync();
            try
            {
                await action(scope.ServiceProvider);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = ServiceProvider.CreateScope();
        var strategy = scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.BeginTransactionAsync();
            try
            {
                var result = await action(scope.ServiceProvider);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    protected async Task CleanDatabaseAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.GetDbConnection();
        
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        // Ordered child-before-parent so FK constraints don't cause silent DELETE
        // failures (which previously leaked rows across tests).
        var tables = new[] {
            "ap.PaymentLines", "ap.Payments", "ap.VoucherDistributions", "ap.Vouchers", "ap.VoucherBatches", "ap.Vendors", "ap.PaymentTerms",
            "gl.JournalEntryLines", "gl.JournalBatches", "gl.BudgetLines", "gl.Budgets", "gl.AllocationRuleLines", "gl.AllocationRules", "gl.RecurringTemplateLines", "gl.RecurringTemplates",
            "ar.InvoiceLines", "ar.CashReceiptApplications", "ar.Invoices", "ar.InvoiceBatches", "ar.CashReceipts", "ar.Customers",
            "cash.DepositLines", "cash.BankTransactions", "cash.Deposits", "cash.BankAccounts",
            "inv.ItemAlternateCodes", "inv.ItemUnitOfMeasureConversions", "inv.ItemCostLayers", "inv.ItemGLAccountDefaults", "inv.ItemStocks", "inv.ItemVendorAssignments", "inv.InventoryTransactions", "inv.InventoryValuationSnapshots", "inv.SlowMovingAlerts", "inv.ReorderAlerts", "inv.ReorderSuggestionLines", "inv.ReorderSuggestions", "inv.CycleCountLines", "inv.CycleCounts", "inv.PhysicalCountLines", "inv.PhysicalCounts", "inv.LandedCostAllocations", "inv.ItemRevaluations", "inv.NegativeInventoryOverrides", "inv.ItemReservations", "inv.ItemExpirations", "inv.ItemQuarantines", "inv.ItemMovements", "inv.ABCClassifications", "inv.Lots", "inv.SerialNumbers", "inv.WarehouseBins", "inv.Warehouses", "inv.ItemCategories", "inv.Items",
            "pur.ReceiptLines", "pur.PurchaseOrderLines", "pur.Requisitions", "pur.RequisitionLines", "pur.PurchaseOrders", "pur.Receipts", "pur.Vendors",
            "platform.PendingAuditLogs", "platform.AuditLogs", "platform.ApprovalActions", "platform.ApprovalRequests", "platform.ApprovalSteps", "platform.ApprovalWorkflows", "platform.SoDConflicts", "platform.SoDRules", "platform.ExchangeRates", "platform.Currencies", "platform.NumberSequences", "platform.SegmentValues", "platform.SegmentTypes", "platform.FiscalPeriods", "platform.FiscalYears", "platform.Companies" };

        foreach (var table in tables)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM [{table}]";
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Table might not exist, ignore
            }
        }
    }
}