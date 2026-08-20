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
    public DbSet<ProjectCostCategoryMapping> ProjectCostCategoryMappings => Set<ProjectCostCategoryMapping>();
    public DbSet<ProjectRoleRate> ProjectRoleRates => Set<ProjectRoleRate>();
    public DbSet<BudgetTemplate> BudgetTemplates => Set<BudgetTemplate>();
    public DbSet<EmployeeProjectAssignment> EmployeeProjectAssignments => Set<EmployeeProjectAssignment>();
    public DbSet<CostAllocationBatch> CostAllocationBatches => Set<CostAllocationBatch>();
    public DbSet<ProjectCommittedCost> ProjectCommittedCosts => Set<ProjectCommittedCost>();
    public DbSet<CostAdjustment> CostAdjustments => Set<CostAdjustment>();
    public DbSet<Subcontract> Subcontracts => Set<Subcontract>();
    public DbSet<SubcontractChangeOrder> SubcontractChangeOrders => Set<SubcontractChangeOrder>();
    public DbSet<SubcontractInvoice> SubcontractInvoices => Set<SubcontractInvoice>();
    public DbSet<SubcontractCompliance> SubcontractCompliances => Set<SubcontractCompliance>();
    public DbSet<LienWaiver> LienWaivers => Set<LienWaiver>();

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
            entity.Property(e => e.ContingencyAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReleasedContingency).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CurrencyCode).HasMaxLength(10);
            entity.Property(e => e.BillingHoldReason).HasMaxLength(500);
            entity.Property(e => e.EstimateAtCompletion).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AccruedLoss).HasColumnType("decimal(18,2)");

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
            entity.HasMany(e => e.Subcontracts).WithOne().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
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

        // ProjectCostCategoryMapping (job-costing overlay: cost category -> GL account)
        modelBuilder.Entity<ProjectCostCategoryMapping>(entity =>
        {
            entity.ToTable("ProjectCostCategoryMappings");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.CostCategory }).IsUnique();
        });

        // ProjectRoleRate
        modelBuilder.Entity<ProjectRoleRate>(entity =>
        {
            entity.ToTable("ProjectRoleRates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CostRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BillingRate).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.CompanyId);
        });

        // BudgetTemplate + lines
        modelBuilder.Entity<BudgetTemplate>(entity =>
        {
            entity.ToTable("BudgetTemplates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProjectType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.CompanyId);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.TemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BudgetTemplateLine>(entity =>
        {
            entity.ToTable("BudgetTemplateLines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.BudgetAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BudgetedHours).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.TemplateId);
        });

        // EmployeeProjectAssignment
        modelBuilder.Entity<EmployeeProjectAssignment>(entity =>
        {
            entity.ToTable("EmployeeProjectAssignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoleName).HasMaxLength(100);
            entity.Property(e => e.AllocationPercentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => e.ProjectId);
        });

        // CostAllocationBatch + lines
        modelBuilder.Entity<CostAllocationBatch>(entity =>
        {
            entity.ToTable("CostAllocationBatches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AllocationBase).HasMaxLength(50);
            entity.Property(e => e.TotalAllocated).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.CompanyId);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.BatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostAllocationLine>(entity =>
        {
            entity.ToTable("CostAllocationLines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.ProjectId);
        });

        // ProjectCommittedCost
        modelBuilder.Entity<ProjectCommittedCost>(entity =>
        {
            entity.ToTable("ProjectCommittedCosts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SourceReference).HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.ProjectId);
        });

        // CostAdjustment
        modelBuilder.Entity<CostAdjustment>(entity =>
        {
            entity.ToTable("CostAdjustments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ApprovedBy).HasMaxLength(200);
            entity.Property(e => e.AdjustmentAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.SourceProjectId);
            entity.HasIndex(e => e.Status);
        });

        // Subcontract + children
        modelBuilder.Entity<Subcontract>(entity =>
        {
            entity.ToTable("Subcontracts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubcontractNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Scope).HasMaxLength(2000);
            entity.Property(e => e.ContractAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RetainagePercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.BilledToDate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RetainageHeld).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.VendorId);
            entity.HasMany(e => e.ChangeOrders).WithOne().HasForeignKey(c => c.SubcontractId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Invoices).WithOne().HasForeignKey(i => i.SubcontractId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Compliance).WithOne().HasForeignKey(c => c.SubcontractId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.LienWaivers).WithOne().HasForeignKey(w => w.SubcontractId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubcontractChangeOrder>(entity =>
        {
            entity.ToTable("SubcontractChangeOrders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.HasIndex(e => e.SubcontractId);
        });

        modelBuilder.Entity<SubcontractInvoice>(entity =>
        {
            entity.ToTable("SubcontractInvoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RetainageRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.RetainageAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.SubcontractId);
        });

        modelBuilder.Entity<SubcontractCompliance>(entity =>
        {
            entity.ToTable("SubcontractCompliances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DocumentReference).HasMaxLength(200);
            entity.HasIndex(e => e.SubcontractId);
        });

        modelBuilder.Entity<LienWaiver>(entity =>
        {
            entity.ToTable("LienWaivers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WaiverType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.SubcontractId);
        });
    }
}
