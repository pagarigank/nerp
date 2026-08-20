// <copyright file="FieldServiceDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.FieldService.Domain.Entities;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.FieldService.Infrastructure;

public class FieldServiceDbContext : DispatchableDbContext
{
    public FieldServiceDbContext(DbContextOptions<FieldServiceDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceContract> ServiceContracts => Set<ServiceContract>();
    public DbSet<EquipmentAsset> EquipmentAssets => Set<EquipmentAsset>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<SkillCertification> SkillCertifications => Set<SkillCertification>();
    public DbSet<TechnicianSkill> TechnicianSkills => Set<TechnicianSkill>();
    public DbSet<SlaDefinition> SlaDefinitions => Set<SlaDefinition>();
    public DbSet<ServiceTerritory> ServiceTerritories => Set<ServiceTerritory>();
    public DbSet<ServiceRateCard> ServiceRateCards => Set<ServiceRateCard>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<PreventiveMaintenance> PreventiveMaintenances => Set<PreventiveMaintenance>();
    public DbSet<VanStock> VanStocks => Set<VanStock>();
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();
    public DbSet<ServiceCall> ServiceCalls => Set<ServiceCall>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderLine> WorkOrderLines => Set<WorkOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("fs");

        modelBuilder.Entity<ServiceContract>(e =>
        {
            e.ToTable("ServiceContracts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ContractNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ContractValue).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.CompanyId, x.ContractNumber }).IsUnique();
        });

        modelBuilder.Entity<EquipmentAsset>(e =>
        {
            e.ToTable("EquipmentAssets");
            e.HasKey(x => x.Id);
            e.Property(x => x.AssetTag).IsRequired().HasMaxLength(50);
            e.Property(x => x.SerialNumber).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.AssetTag }).IsUnique();
        });

        modelBuilder.Entity<Technician>(e =>
        {
            e.ToTable("Technicians");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<SkillCertification>(e =>
        {
            e.ToTable("SkillCertifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<TechnicianSkill>(e =>
        {
            e.ToTable("TechnicianSkills");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TechnicianId);
            e.HasIndex(x => x.SkillCertificationId);
        });

        modelBuilder.Entity<SlaDefinition>(e =>
        {
            e.ToTable("SlaDefinitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.EscalationTo).HasMaxLength(100);
            e.HasIndex(x => new { x.CompanyId, x.Priority }).IsUnique();
        });

        modelBuilder.Entity<ServiceTerritory>(e =>
        {
            e.ToTable("ServiceTerritories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Region).HasMaxLength(100);
            e.Property(x => x.ZipCoverage).HasMaxLength(500);
            e.Property(x => x.TravelCostPerMile).HasColumnType("decimal(9,4)");
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<ServiceRateCard>(e =>
        {
            e.ToTable("ServiceRateCards");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.LaborRatePerHour).HasColumnType("decimal(18,2)");
            e.Property(x => x.OvertimeRatePerHour).HasColumnType("decimal(18,2)");
            e.Property(x => x.TripCharge).HasColumnType("decimal(18,2)");
            e.Property(x => x.PartsMarkupPercent).HasColumnType("decimal(9,4)");
            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Estimate>(e =>
        {
            e.ToTable("Estimates");
            e.HasKey(x => x.Id);
            e.Property(x => x.EstimateNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.LaborEstimate).HasColumnType("decimal(18,2)");
            e.Property(x => x.PartsEstimate).HasColumnType("decimal(18,2)");
            e.Property(x => x.TravelEstimate).HasColumnType("decimal(18,2)");
            e.Property(x => x.TaxEstimate).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.CompanyId, x.EstimateNumber }).IsUnique();
        });

        modelBuilder.Entity<PreventiveMaintenance>(e =>
        {
            e.ToTable("PreventiveMaintenances");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<VanStock>(e =>
        {
            e.ToTable("VanStocks");
            e.HasKey(x => x.Id);
            e.Property(x => x.QuantityOnHand).HasColumnType("decimal(18,2)");
            e.Property(x => x.ReorderPoint).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.CompanyId, x.TechnicianId, x.ItemId }).IsUnique();
        });

        modelBuilder.Entity<WarrantyClaim>(e =>
        {
            e.ToTable("WarrantyClaims");
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.ClaimAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.CompanyId, x.ClaimNumber }).IsUnique();
        });

        modelBuilder.Entity<ServiceCall>(e =>
        {
            e.ToTable("ServiceCalls");
            e.HasKey(x => x.Id);
            e.Property(x => x.CallNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            e.Property(x => x.ResolutionSummary).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.CallNumber }).IsUnique();
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.ToTable("WorkOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.WorkOrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.LaborHours).HasColumnType("decimal(18,2)");
            e.Property(x => x.LaborCost).HasColumnType("decimal(18,2)");
            e.Property(x => x.PartsCost).HasColumnType("decimal(18,2)");
            e.Property(x => x.TravelCost).HasColumnType("decimal(18,2)");
            e.Property(x => x.Fees).HasColumnType("decimal(18,2)");
            e.Property(x => x.BillableTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Resolution).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.WorkOrderNumber }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.TechnicianId);
            e.HasIndex(x => x.ScheduledStart);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkOrderLine>(e =>
        {
            e.ToTable("WorkOrderLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).IsRequired().HasMaxLength(500);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitRate).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.CostAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.WorkOrderId);
            e.HasIndex(x => x.ItemId);
        });
    }
}
