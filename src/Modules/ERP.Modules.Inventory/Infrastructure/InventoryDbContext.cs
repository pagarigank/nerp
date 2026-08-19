// <copyright file="InventoryDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure;

public class InventoryDbContext : DispatchableDbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();

    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();

    public DbSet<ItemAlternateCode> ItemAlternateCodes => Set<ItemAlternateCode>();

    public DbSet<ItemUnitOfMeasureConversion> ItemUnitOfMeasureConversions => Set<ItemUnitOfMeasureConversion>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<WarehouseBin> WarehouseBins => Set<WarehouseBin>();

    public DbSet<ItemStock> ItemStocks => Set<ItemStock>();

    public DbSet<ItemSubstitution> ItemSubstitutions => Set<ItemSubstitution>();

    public DbSet<KitComponent> KitComponents => Set<KitComponent>();

    public DbSet<PutAwayPickingRule> PutAwayPickingRules => Set<PutAwayPickingRule>();

    public DbSet<ConsignmentStock> ConsignmentStocks => Set<ConsignmentStock>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<ItemCostLayer> ItemCostLayers => Set<ItemCostLayer>();

    public DbSet<Lot> Lots => Set<Lot>();

    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();

    public DbSet<ItemVendorAssignment> ItemVendorAssignments => Set<ItemVendorAssignment>();

    public DbSet<ItemGLAccountDefaults> ItemGLAccountDefaults => Set<ItemGLAccountDefaults>();

    public DbSet<CycleCount> CycleCounts => Set<CycleCount>();

    public DbSet<CycleCountLine> CycleCountLines => Set<CycleCountLine>();

    public DbSet<PhysicalCount> PhysicalCounts => Set<PhysicalCount>();

    public DbSet<PhysicalCountLine> PhysicalCountLines => Set<PhysicalCountLine>();

    public DbSet<LandedCostAllocation> LandedCostAllocations => Set<LandedCostAllocation>();

    public DbSet<LandedCostAllocationLine> LandedCostAllocationLines => Set<LandedCostAllocationLine>();

    public DbSet<LandedCost> LandedCosts => Set<LandedCost>();

    public DbSet<ItemRevaluation> ItemRevaluations => Set<ItemRevaluation>();

    public DbSet<ItemRevaluationLine> ItemRevaluationLines => Set<ItemRevaluationLine>();

    public DbSet<NegativeInventoryOverride> NegativeInventoryOverrides => Set<NegativeInventoryOverride>();

    public DbSet<ReorderSuggestion> ReorderSuggestions => Set<ReorderSuggestion>();

    public DbSet<ReorderSuggestionLine> ReorderSuggestionLines => Set<ReorderSuggestionLine>();

    public DbSet<ItemReservation> ItemReservations => Set<ItemReservation>();

    public DbSet<ItemExpiration> ItemExpirations => Set<ItemExpiration>();

    public DbSet<ItemExpirationAlert> ItemExpirationAlerts => Set<ItemExpirationAlert>();

    public DbSet<ItemQuarantine> ItemQuarantines => Set<ItemQuarantine>();

    public DbSet<QuarantineDisposition> QuarantineDispositions => Set<QuarantineDisposition>();

    public DbSet<ItemMovement> ItemMovements => Set<ItemMovement>();

    public DbSet<ReorderAlert> ReorderAlerts => Set<ReorderAlert>();

    public DbSet<InventoryValuationSnapshot> InventoryValuationSnapshots => Set<InventoryValuationSnapshot>();

    public DbSet<SlowMovingAlert> SlowMovingAlerts => Set<SlowMovingAlert>();

    public DbSet<LotExpirationAlert> LotExpirationAlerts => Set<LotExpirationAlert>();

    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("inv");

        // Item
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LongDescription).HasMaxLength(1000);
            entity.Property(e => e.BaseUnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StandardCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReorderPoint).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReorderQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.SafetyStock).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ABCClass).HasMaxLength(1);

            entity.HasIndex(e => new { e.CompanyId, e.ItemCode }).IsUnique();
            entity.HasIndex(e => e.ItemCategoryId);

            entity.HasMany(e => e.AlternateCodes)
                .WithOne()
                .HasForeignKey(a => a.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UOMConversions)
                .WithOne()
                .HasForeignKey(u => u.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemCategory
        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.ToTable("ItemCategories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.CategoryCode }).IsUnique();
        });

        // ItemAlternateCode
        modelBuilder.Entity<ItemAlternateCode>(entity =>
        {
            entity.ToTable("ItemAlternateCodes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AlternateCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);

            entity.HasIndex(e => new { e.ItemId, e.CodeType, e.AlternateCode }).IsUnique();
        });

        // ItemUnitOfMeasureConversion
        modelBuilder.Entity<ItemUnitOfMeasureConversion>(entity =>
        {
            entity.ToTable("ItemUnitOfMeasureConversions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FromUOM).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ToUOM).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ConversionFactor).HasColumnType("decimal(18,6)");

            entity.HasIndex(e => new { e.ItemId, e.FromUOM, e.ToUOM }).IsUnique();
        });

        // UnitOfMeasure (global UOM master)
        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.ToTable("UnitOfMeasures");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CompanyId).IsRequired();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.BaseUOM).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FactorToBase).HasColumnType("decimal(18,6)");

            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        // Warehouse
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.WarehouseCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.WarehouseName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.WarehouseCode }).IsUnique();
        });

        // WarehouseBin
        modelBuilder.Entity<WarehouseBin>(entity =>
        {
            entity.ToTable("WarehouseBins");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BinCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Aisle).HasMaxLength(20);
            entity.Property(e => e.Rack).HasMaxLength(20);
            entity.Property(e => e.Shelf).HasMaxLength(20);

            entity.HasIndex(e => new { e.WarehouseId, e.BinCode }).IsUnique();
        });

        // ItemStock
        modelBuilder.Entity<ItemStock>(entity =>
        {
            entity.ToTable("ItemStocks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AllocatedQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.OnOrderQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.LotId).HasColumnType("uniqueidentifier");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId, e.BinId, e.LotId }).IsUnique();
        });

        // InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ExtendedCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.ReferenceNumber);
        });

        // ItemCostLayer
        modelBuilder.Entity<ItemCostLayer>(entity =>
        {
            entity.ToTable("ItemCostLayers");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.RemainingQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId, e.ReceivedDate });
            entity.HasIndex(e => e.LotId);
        });

        // Lot
        modelBuilder.Entity<Lot>(entity =>
        {
            entity.ToTable("Lots");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LotNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.VendorLotNumber).HasMaxLength(100);

            entity.HasIndex(e => new { e.ItemId, e.WarehouseId, e.LotNumber }).IsUnique();
        });

        // SerialNumber
        modelBuilder.Entity<SerialNumber>(entity =>
        {
            entity.ToTable("SerialNumbers");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SerialNo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WarrantyInfo).HasMaxLength(500);

            entity.HasIndex(e => new { e.ItemId, e.SerialNo }).IsUnique();
        });

        // ItemVendorAssignment
        modelBuilder.Entity<ItemVendorAssignment>(entity =>
        {
            entity.ToTable("ItemVendorAssignments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VendorItemCode).HasMaxLength(100);
            entity.Property(e => e.VendorDescription).HasMaxLength(200);
            entity.Property(e => e.VendorCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MinimumOrderQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.ItemId, e.VendorId }).IsUnique();
            entity.HasIndex(e => e.VendorId);
        });

        // ItemGLAccountDefaults
        modelBuilder.Entity<ItemGLAccountDefaults>(entity =>
        {
            entity.ToTable("ItemGLAccountDefaults");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ItemId).IsUnique();
        });

        // CycleCount
        modelBuilder.Entity<CycleCount>(entity =>
        {
            entity.ToTable("CycleCounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CountNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.CountNumber }).IsUnique();
            entity.HasIndex(e => e.WarehouseId);
            entity.HasIndex(e => e.CountDate);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.CycleCountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CycleCountLine
        modelBuilder.Entity<CycleCountLine>(entity =>
        {
            entity.ToTable("CycleCountLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LotNumber).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.SystemQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CountedQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => e.CycleCountId);
            entity.HasIndex(e => e.ItemId);
        });

        // PhysicalCount
        modelBuilder.Entity<PhysicalCount>(entity =>
        {
            entity.ToTable("PhysicalCounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CountNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.CountNumber }).IsUnique();
            entity.HasIndex(e => e.WarehouseId);
            entity.HasIndex(e => e.CountDate);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.PhysicalCountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PhysicalCountLine
        modelBuilder.Entity<PhysicalCountLine>(entity =>
        {
            entity.ToTable("PhysicalCountLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LotNumber).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.SystemQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CountedQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => e.PhysicalCountId);
            entity.HasIndex(e => e.ItemId);
        });

        // LandedCostAllocation
        modelBuilder.Entity<LandedCostAllocation>(entity =>
        {
            entity.ToTable("LandedCostAllocations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AllocationNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.AllocationNumber }).IsUnique();
            entity.HasIndex(e => e.ReceiptTransactionId);
            entity.HasIndex(e => e.AllocationDate);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.LandedCostAllocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LandedCostAllocationLine
        modelBuilder.Entity<LandedCostAllocationLine>(entity =>
        {
            entity.ToTable("LandedCostAllocationLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.QuantityReceived).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => e.LandedCostAllocationId);
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.LandedCostId);
        });

        // LandedCost
        modelBuilder.Entity<LandedCost>(entity =>
        {
            entity.ToTable("LandedCosts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CostCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.CostCode }).IsUnique();
            entity.HasIndex(e => e.VendorId);
            entity.HasIndex(e => e.CostDate);
            entity.HasIndex(e => e.Status);
        });

        // ItemRevaluation
        modelBuilder.Entity<ItemRevaluation>(entity =>
        {
            entity.ToTable("ItemRevaluations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RevaluationNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.TotalAdjustmentValue).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.RevaluationNumber }).IsUnique();
            entity.HasIndex(e => e.RevaluationDate);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.RevaluationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemRevaluationLine
        modelBuilder.Entity<ItemRevaluationLine>(entity =>
        {
            entity.ToTable("ItemRevaluationLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReasonCode).HasMaxLength(50);

            entity.Property(e => e.CurrentQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CurrentStandardCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.NewStandardCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AdjustmentValue).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => e.RevaluationId);
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.WarehouseId);
        });

        // NegativeInventoryOverride
        modelBuilder.Entity<NegativeInventoryOverride>(entity =>
        {
            entity.ToTable("NegativeInventoryOverrides");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.ApprovalNotes).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.Property(e => e.RequestedQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedBy);
            entity.HasIndex(e => e.ApprovedBy);
        });

        // ReorderSuggestion
        modelBuilder.Entity<ReorderSuggestion>(entity =>
        {
            entity.ToTable("ReorderSuggestions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SuggestionNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.SuggestionNumber }).IsUnique();
            entity.HasIndex(e => e.SuggestionDate);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.ReorderSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReorderSuggestionLine
        modelBuilder.Entity<ReorderSuggestionLine>(entity =>
        {
            entity.ToTable("ReorderSuggestionLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VendorId).HasMaxLength(100);
            entity.Property(e => e.Priority).HasMaxLength(20);

            entity.Property(e => e.CurrentOnHand).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CurrentAllocated).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReorderPoint).HasColumnType("decimal(18,4)");
            entity.Property(e => e.SafetyStock).HasColumnType("decimal(18,4)");
            entity.Property(e => e.LeadTimeDemand).HasColumnType("decimal(18,4)");
            entity.Property(e => e.SuggestedOrderQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.EstimatedStockoutDate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.VendorCost).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => e.ReorderSuggestionId);
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.WarehouseId);
            entity.HasIndex(e => e.Status);
        });

        // ItemReservation
        modelBuilder.Entity<ItemReservation>(entity =>
        {
            entity.ToTable("ItemReservations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.LotNumber).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReleasedQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.SourceType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpirationDate);
        });

        // ItemExpiration
        modelBuilder.Entity<ItemExpiration>(entity =>
        {
            entity.ToTable("ItemExpirations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.LotId);
            entity.HasIndex(e => e.SerialNumberId);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Alerts)
                .WithOne()
                .HasForeignKey(a => a.ItemExpirationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemExpirationAlert
        modelBuilder.Entity<ItemExpirationAlert>(entity =>
        {
            entity.ToTable("ItemExpirationAlerts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);

            entity.HasIndex(e => e.ItemExpirationId);
            entity.HasIndex(e => e.AlertDate);
            entity.HasIndex(e => e.AlertType);
            entity.HasIndex(e => e.IsAcknowledged);
        });

        // ItemQuarantine
        modelBuilder.Entity<ItemQuarantine>(entity =>
        {
            entity.ToTable("ItemQuarantines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReleaseReason).HasMaxLength(500);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.BinId);
            entity.HasIndex(e => e.LotId);
            entity.HasIndex(e => e.SerialNumberId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.QuarantineDate);
            entity.HasIndex(e => e.QuarantinedBy);

            entity.HasMany(e => e.Dispositions)
                .WithOne()
                .HasForeignKey(d => d.QuarantineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuarantineDisposition
        modelBuilder.Entity<QuarantineDisposition>(entity =>
        {
            entity.ToTable("QuarantineDispositions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => e.QuarantineId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.DispositionDate);
            entity.HasIndex(e => e.PerformedBy);
        });

        // ItemMovement
        modelBuilder.Entity<ItemMovement>(entity =>
        {
            entity.ToTable("ItemMovements");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.MovementDate);
            entity.HasIndex(e => e.MovementType);
            entity.HasIndex(e => e.ReferenceNumber);
            entity.HasIndex(e => e.LotId);
            entity.HasIndex(e => e.SerialNumberId);
            entity.HasIndex(e => e.CreatedBy);
        });

        // ReorderAlert
        modelBuilder.Entity<ReorderAlert>(entity =>
        {
            entity.ToTable("ReorderAlerts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);

            entity.Property(e => e.CurrentOnHand).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReorderPoint).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.AlertDate);
            entity.HasIndex(e => e.IsAcknowledged);
        });

        // InventoryValuationSnapshot
        modelBuilder.Entity<InventoryValuationSnapshot>(entity =>
        {
            entity.ToTable("InventoryValuationSnapshots");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.StandardCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AverageCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.StandardValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AverageValue).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId, e.SnapshotDate }).IsUnique();
            entity.HasIndex(e => e.SnapshotDate);
        });

        // SlowMovingAlert
        modelBuilder.Entity<SlowMovingAlert>(entity =>
        {
            entity.ToTable("SlowMovingAlerts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);

            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.WarehouseId });
            entity.HasIndex(e => e.AlertDate);
            entity.HasIndex(e => e.IsAcknowledged);
        });

        // LotExpirationAlert
        modelBuilder.Entity<LotExpirationAlert>(entity =>
        {
            entity.ToTable("LotExpirationAlerts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);

            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.CompanyId, e.LotId });
            entity.HasIndex(e => e.AlertDate);
            entity.HasIndex(e => e.AlertType);
            entity.HasIndex(e => e.IsAcknowledged);
        });

        // Item physical-attribute decimals
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.Weight).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Length).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Width).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Height).HasColumnType("decimal(18,4)");
        });

        // ItemSubstitution
        modelBuilder.Entity<ItemSubstitution>(entity =>
        {
            entity.ToTable("ItemSubstitutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ApprovedBy).HasMaxLength(100);
            entity.Property(e => e.RejectedBy).HasMaxLength(100);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasIndex(e => new { e.CompanyId, e.ItemId });

            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SubstituteItem)
                .WithMany()
                .HasForeignKey(e => e.SubstituteItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // KitComponent
        modelBuilder.Entity<KitComponent>(entity =>
        {
            entity.ToTable("KitComponents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(10);
            entity.Property(e => e.QuantityPerKit).HasColumnType("decimal(18,4)");
            entity.HasIndex(e => new { e.CompanyId, e.KitItemId });

            entity.HasOne(e => e.KitItem)
                .WithMany()
                .HasForeignKey(e => e.KitItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ComponentItem)
                .WithMany()
                .HasForeignKey(e => e.ComponentItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PutAwayPickingRule
        modelBuilder.Entity<PutAwayPickingRule>(entity =>
        {
            entity.ToTable("PutAwayPickingRules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CompanyId, e.WarehouseId, e.BinId });
        });

        // ConsignmentStock
        modelBuilder.Entity<ConsignmentStock>(entity =>
        {
            entity.ToTable("ConsignmentStocks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(10);
            entity.Property(e => e.QuantityOnHand).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ConsignmentCost).HasColumnType("decimal(18,4)");
            entity.HasIndex(e => new { e.CompanyId, e.VendorId, e.ItemId, e.WarehouseId });
        });
    }
}