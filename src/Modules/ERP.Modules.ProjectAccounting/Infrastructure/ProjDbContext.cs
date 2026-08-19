// <copyright file="ProjDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure;

public class ProjDbContext : DispatchableDbContext
{
    public ProjDbContext(DbContextOptions<ProjDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<CostTransaction> CostTransactions => Set<CostTransaction>();
    public DbSet<ChangeOrder> ChangeOrders => Set<ChangeOrder>();
    public DbSet<ContractLine> ContractLines => Set<ContractLine>();
    public DbSet<BillingSchedule> BillingSchedules => Set<BillingSchedule>();
    public DbSet<ProjectAllocationRule> ProjectAllocationRules => Set<ProjectAllocationRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("proj");

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ProjectManager).HasMaxLength(200);
            entity.Property(e => e.ContractValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalBudget).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RevisedBudget).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CostsToDate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RevenueToDate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PercentComplete).HasColumnType("decimal(5,2)");
            entity.Property(e => e.RetainagePercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.RetainageHeld).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.CompanyId, e.ProjectCode }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CustomerId);

            entity.HasMany(e => e.Tasks).WithOne().HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.BudgetLines).WithOne().HasForeignKey(b => b.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.CostTransactions).WithOne().HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ChangeOrders).WithOne().HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ContractLines).WithOne().HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.BillingSchedules).WithOne().HasForeignKey(b => b.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.AllocationRules).WithOne().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectTask
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.ToTable("ProjectTasks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TaskCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BudgetedHours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BudgetedCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualHours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PercentComplete).HasColumnType("decimal(5,2)");

            entity.HasIndex(e => new { e.ProjectId, e.TaskCode }).IsUnique();
        });

        // BudgetLine
        modelBuilder.Entity<BudgetLine>(entity =>
        {
            entity.ToTable("BudgetLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BudgetAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BudgetedHours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualHours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CommittedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.TaskId);
        });

        // CostTransaction
        modelBuilder.Entity<CostTransaction>(entity =>
        {
            entity.ToTable("CostTransactions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Hours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BurdenAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BillableAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SourceReference).HasMaxLength(100);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsBilled);
        });

        // ChangeOrder
        modelBuilder.Entity<ChangeOrder>(entity =>
        {
            entity.ToTable("ChangeOrders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ApprovedBy).HasMaxLength(200);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
        });

        // ContractLine
        modelBuilder.Entity<ContractLine>(entity =>
        {
            entity.ToTable("ContractLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContractAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitQuantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FeePercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.NotToExceed).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BilledAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PercentComplete).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => e.ProjectId);
        });

        // BillingSchedule
        modelBuilder.Entity<BillingSchedule>(entity =>
        {
            entity.ToTable("BillingSchedules");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PercentCompleteTrigger).HasColumnType("decimal(5,2)");

            entity.HasIndex(e => e.ProjectId);
        });

        // ProjectAllocationRule
        modelBuilder.Entity<ProjectAllocationRule>(entity =>
        {
            entity.ToTable("ProjectAllocationRules");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MarkupPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.OverheadPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.ProjectId);
        });
    }
}
