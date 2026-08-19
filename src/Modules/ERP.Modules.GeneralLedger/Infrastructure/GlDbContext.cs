// <copyright file="GlDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class GlDbContext : DispatchableDbContext
{
    public GlDbContext(DbContextOptions<GlDbContext> options) : base(options)
    {
    }

    public DbSet<JournalBatch> JournalBatches => Set<JournalBatch>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<RecurringTemplate> RecurringTemplates => Set<RecurringTemplate>();
    public DbSet<RecurringTemplateLine> RecurringTemplateLines => Set<RecurringTemplateLine>();
    public DbSet<AllocationRule> AllocationRules => Set<AllocationRule>();
    public DbSet<AllocationRuleLine> AllocationRuleLines => Set<AllocationRuleLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<ConsolidationRun> ConsolidationRuns => Set<ConsolidationRun>();
    public DbSet<IntercompanyMapping> IntercompanyMappings => Set<IntercompanyMapping>();
    public DbSet<YearEndCloseRun> YearEndCloseRuns => Set<YearEndCloseRun>();
    public DbSet<PostingSuspenseItem> PostingSuspenseItems => Set<PostingSuspenseItem>();
    public DbSet<BudgetTransfer> BudgetTransfers => Set<BudgetTransfer>();
    public DbSet<GlGainLoss> GlGainLosses => Set<GlGainLoss>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("gl");

        modelBuilder.Entity<JournalBatch>(e =>
        {
            e.ToTable("JournalBatches");
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.PostingDate).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.BatchNumber }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasMany(x => x.Lines).WithOne(l => l.JournalBatch).HasForeignKey(x => x.JournalBatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JournalEntryLine>(e =>
        {
            e.ToTable("JournalEntryLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Debit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Credit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Reference).HasMaxLength(255);
            e.Property(x => x.SegmentsJson).HasMaxLength(4000);
            e.Property(x => x.CurrencyId).IsRequired(false);
            e.Property(x => x.ExchangeRate).HasColumnType("decimal(18,6)").HasDefaultValue(1.0m);
            e.Property(x => x.ForeignDebit).HasColumnType("decimal(18,2)");
            e.Property(x => x.ForeignCredit).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.JournalBatchId);
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.CurrencyId);
            e.HasOne(l => l.JournalBatch).WithMany(b => b.Lines).HasForeignKey(l => l.JournalBatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurringTemplate>(e =>
        {
            e.ToTable("RecurringTemplates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Frequency).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.RecurringTemplateId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<RecurringTemplateLine>(e =>
        {
            e.ToTable("RecurringTemplateLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.FixedDebit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.FixedCredit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.VariablePct).HasColumnType("decimal(9,6)");
            e.Property(x => x.Reference).HasMaxLength(255);
        });

        modelBuilder.Entity<AllocationRule>(e =>
        {
            e.ToTable("AllocationRules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Method).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.AllocationRuleId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<AllocationRuleLine>(e =>
        {
            e.ToTable("AllocationRuleLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Percentage).HasColumnType("decimal(9,6)").IsRequired();
            e.Property(x => x.FixedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Reference).HasMaxLength(255);
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.ToTable("Budgets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.BudgetType).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.FiscalYearId });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BudgetLine>(e =>
        {
            e.ToTable("BudgetLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => new { x.BudgetId, x.PeriodNumber });
        });

        modelBuilder.Entity<ConsolidationRun>(e =>
        {
            e.ToTable("ConsolidationRuns");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => x.ParentCompanyId);
            e.HasIndex(x => x.FiscalPeriodId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<IntercompanyMapping>(e =>
        {
            e.ToTable("IntercompanyMappings");
            e.HasKey(x => x.Id);
            e.Property(x => x.FromAccountNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.ToAccountNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.FromCompanyId);
            e.HasIndex(x => x.ToCompanyId);
            e.HasIndex(x => x.IsActive);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<YearEndCloseRun>(e =>
        {
            e.ToTable("YearEndCloseRuns");
            e.HasKey(x => x.Id);
            e.Property(x => x.ClosedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.Property(x => x.TotalRevenue).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.TotalExpense).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.RetainedEarningsAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.FiscalYearId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<PostingSuspenseItem>(e =>
        {
            e.ToTable("PostingSuspenseItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceModule).HasMaxLength(50).IsRequired();
            e.Property(x => x.SourceReference).HasMaxLength(255).IsRequired();
            e.Property(x => x.ReasonCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.Property(x => x.Debit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Credit).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BudgetTransfer>(e =>
        {
            e.ToTable("BudgetTransfers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.BudgetId);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<GlGainLoss>(e =>
        {
            e.ToTable("GlGainLosses");
            e.HasKey(x => x.Id);
            e.Property(x => x.GainLossAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.FiscalPeriodId);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });
    }
}
