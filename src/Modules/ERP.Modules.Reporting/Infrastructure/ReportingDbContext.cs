// <copyright file="ReportingDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Services;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Infrastructure;

public class ReportingDbContext : DispatchableDbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
    {
    }

    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<SavedQuery> SavedQueries => Set<SavedQuery>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<ReportSubscription> ReportSubscriptions => Set<ReportSubscription>();
    public DbSet<FinancialStatementLayout> FinancialStatementLayouts => Set<FinancialStatementLayout>();
    public DbSet<QuickQuery> QuickQueries => Set<QuickQuery>();
    public DbSet<ReportUsageLog> ReportUsageLogs => Set<ReportUsageLog>();
    public DbSet<SyncWatermark> SyncWatermarks => Set<SyncWatermark>();
    public DbSet<SyncRunLog> SyncRunLogs => Set<SyncRunLog>();
    public DbSet<SearchIndexEntry> SearchIndexEntries => Set<SearchIndexEntry>();
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();
    public DbSet<SearchIndexSyncState> SearchIndexSyncState => Set<SearchIndexSyncState>();
    public DbSet<DeliveryRetryEntry> DeliveryRetryEntries => Set<DeliveryRetryEntry>();
    public DbSet<ReportCategory> ReportCategories => Set<ReportCategory>();
    public DbSet<ReportParameterSet> ReportParameterSets => Set<ReportParameterSet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rpt");

        modelBuilder.Entity<ReportDefinition>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Module });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<SavedQuery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.QueryType).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(100);
            e.Property(x => x.CreatedByUser).HasMaxLength(256);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Name });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<DashboardWidget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DashboardId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.WidgetType).HasMaxLength(50).IsRequired();
            e.Property(x => x.DataSourceType).HasMaxLength(50);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.DashboardId });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ReportSubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ExportFormat).HasMaxLength(20).IsRequired();
            e.Property(x => x.ScheduleType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.LastRunStatus).HasMaxLength(50);
            e.Property(x => x.LastRunError).HasMaxLength(2000);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.ReportDefinitionId });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<FinancialStatementLayout>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.StatementType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Name });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<QuickQuery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            e.Property(x => x.CreatedByUser).HasMaxLength(256);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Name });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ReportUsageLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
            e.Property(x => x.ExecutedByUser).HasMaxLength(256);
            e.Property(x => x.ExportFormat).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.ExecutedOn);
            e.HasIndex(x => new { x.ReportDefinitionId, x.ExecutedOn });
        });

        modelBuilder.Entity<SyncWatermark>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceTable).HasMaxLength(200).IsRequired();
            e.Property(x => x.StagingTable).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.SourceTable).IsUnique();
        });

        modelBuilder.Entity<SyncRunLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceTable).HasMaxLength(200).IsRequired();
            e.Property(x => x.StagingTable).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.HasIndex(x => x.SourceTable);
            e.HasIndex(x => x.StartedOn);
        });

        modelBuilder.Entity<SearchIndexEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100).IsRequired();
            e.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
            e.Property(x => x.SearchText).HasMaxLength(4000);
            e.HasIndex(x => new { x.SourceType, x.SourceId }).IsUnique();
            e.HasIndex(x => x.Module);
            e.HasIndex(x => x.Category);
        });

        modelBuilder.Entity<SearchQueryLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Query).HasMaxLength(500).IsRequired();
            e.Property(x => x.ModuleFilter).HasMaxLength(50);
            e.Property(x => x.UserIdentity).HasMaxLength(256);
            e.HasIndex(x => x.Query);
            e.HasIndex(x => x.SearchedOn);
        });

        modelBuilder.Entity<SearchIndexSyncState>(e =>
        {
            e.HasKey(x => x.StringId);
            e.Property(x => x.StringId).HasMaxLength(100);
        });

        modelBuilder.Entity<DeliveryRetryEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ErrorMessage).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.SubscriptionId, x.Status });
            e.HasIndex(x => x.NextRetryOn);
        });

        modelBuilder.Entity<ReportCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Icon).HasMaxLength(100);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.ParentId });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ReportParameterSet>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.ReportDefinitionId });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });
    }
}
