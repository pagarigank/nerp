// <copyright file="BomDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.BillOfMaterials.Infrastructure;

public class BomDbContext : DispatchableDbContext
{
    public BomDbContext(DbContextOptions<BomDbContext> options) : base(options)
    {
    }

    public DbSet<BomHeader> BomHeaders => Set<BomHeader>();
    public DbSet<BomComponentLine> BomComponentLines => Set<BomComponentLine>();
    public DbSet<WorkCenter> WorkCenters => Set<WorkCenter>();
    public DbSet<BuildOrder> BuildOrders => Set<BuildOrder>();
    public DbSet<BuildOrderLine> BuildOrderLines => Set<BuildOrderLine>();
    public DbSet<BomRevisionHistory> BomRevisionHistories => Set<BomRevisionHistory>();
    public DbSet<BomComponentSubstitution> BomComponentSubstitutions => Set<BomComponentSubstitution>();
    public DbSet<ComponentAllocation> ComponentAllocations => Set<ComponentAllocation>();
    public DbSet<EngineeringChangeNotice> EngineeringChangeNotices => Set<EngineeringChangeNotice>();
    public DbSet<BackflushRecord> BackflushRecords => Set<BackflushRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("bom");

        // BomHeader
        modelBuilder.Entity<BomHeader>(entity =>
        {
            entity.ToTable("BomHeaders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Revision).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AlternateCode).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.YieldPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.EstimatedMaterialCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.EstimatedLaborCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.EstimatedOverheadCost).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ParentItemId, e.Revision }).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Components)
                .WithOne()
                .HasForeignKey(c => c.BomHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Revisions)
                .WithOne()
                .HasForeignKey(r => r.BomHeaderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BomComponentLine
        modelBuilder.Entity<BomComponentLine>(entity =>
        {
            entity.ToTable("BomComponentLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QuantityPerParent).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ScrapFactor).HasColumnType("decimal(5,2)");
            entity.Property(e => e.EstimatedUnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => e.BomHeaderId);
            entity.HasIndex(e => e.ComponentItemId);
        });

        // WorkCenter
        modelBuilder.Entity<WorkCenter>(entity =>
        {
            entity.ToTable("WorkCenters");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Department).HasMaxLength(200);
            entity.Property(e => e.CapacityHoursPerDay).HasColumnType("decimal(5,2)");
            entity.Property(e => e.EfficiencyPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.CostRatePerHour).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        // BuildOrder
        modelBuilder.Entity<BuildOrder>(entity =>
        {
            entity.ToTable("BuildOrders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BuildNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuantityToBuild).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ActualYield).HasColumnType("decimal(5,2)");
            entity.Property(e => e.TotalMaterialCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TotalLaborCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TotalOverheadCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.BuildNumber }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BuildDate);
            entity.HasIndex(e => e.ParentItemId);

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.BuildOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BuildOrderLine
        modelBuilder.Entity<BuildOrderLine>(entity =>
        {
            entity.ToTable("BuildOrderLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QuantityRequired).HasColumnType("decimal(18,4)");
            entity.Property(e => e.QuantityIssued).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ExtendedCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.VarianceQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.VarianceCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => e.BuildOrderId);
            entity.HasIndex(e => e.ComponentItemId);
        });

        // BomRevisionHistory
        modelBuilder.Entity<BomRevisionHistory>(entity =>
        {
            entity.ToTable("BomRevisionHistories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Revision).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ChangeDescription).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ReasonForChange).HasMaxLength(500);

            entity.HasIndex(e => e.BomHeaderId);
        });

        // BomComponentSubstitution
        modelBuilder.Entity<BomComponentSubstitution>(entity =>
        {
            entity.ToTable("BomComponentSubstitutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CostVariance).HasColumnType("decimal(18,4)");
            entity.HasIndex(e => e.BomHeaderId);
            entity.HasIndex(e => e.ComponentLineId);
        });

        // ComponentAllocation
        modelBuilder.Entity<ComponentAllocation>(entity =>
        {
            entity.ToTable("ComponentAllocations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.FulfilledQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.BomHeaderId);
            entity.HasIndex(e => e.BuildOrderId);
            entity.HasIndex(e => e.ComponentItemId);
        });

        // EngineeringChangeNotice
        modelBuilder.Entity<EngineeringChangeNotice>(entity =>
        {
            entity.ToTable("EngineeringChangeNotices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EcnNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Reviewer).HasMaxLength(200);
            entity.Property(e => e.Approver).HasMaxLength(200);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasIndex(e => e.BomHeaderId);
            entity.HasIndex(e => new { e.CompanyId, e.EcnNumber }).IsUnique();
        });

        // BackflushRecord
        modelBuilder.Entity<BackflushRecord>(entity =>
        {
            entity.ToTable("BackflushRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityBuilt).HasColumnType("decimal(18,2)");
            entity.Property(e => e.StandardComponentCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualComponentCost).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.BuildOrderId);
            entity.HasIndex(e => e.BomHeaderId);
        });
    }
}
