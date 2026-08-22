// <copyright file="OmDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Infrastructure;

public class OmDbContext : DispatchableDbContext
{
    public OmDbContext(DbContextOptions<OmDbContext> options) : base(options)
    {
    }

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<SalesRep> SalesReps => Set<SalesRep>();
    public DbSet<SalesTerritory> SalesTerritories => Set<SalesTerritory>();
    public DbSet<SalesOrderType> SalesOrderTypes => Set<SalesOrderType>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<TaxExemptionCertificate> TaxExemptionCertificates => Set<TaxExemptionCertificate>();
    public DbSet<BlanketSalesOrder> BlanketSalesOrders => Set<BlanketSalesOrder>();
    public DbSet<BlanketRelease> BlanketReleases => Set<BlanketRelease>();
    public DbSet<BackorderSubstitutionOffer> BackorderSubstitutionOffers => Set<BackorderSubstitutionOffer>();
    public DbSet<ReturnToVendor> ReturnToVendors => Set<ReturnToVendor>();
    public DbSet<SalesOrderNote> SalesOrderNotes => Set<SalesOrderNote>();
    public DbSet<SalesOrderChangeHistory> SalesOrderChangeHistories => Set<SalesOrderChangeHistory>();
    public DbSet<CommissionRun> CommissionRuns => Set<CommissionRun>();
    public DbSet<CommissionRunLine> CommissionRunLines => Set<CommissionRunLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("om");

        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.ToTable("SalesOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.ShipToAddress).HasMaxLength(500);
            e.Property(x => x.BillToAddress).HasMaxLength(500);
            e.Property(x => x.PaymentTermId).HasMaxLength(50);
            e.Property(x => x.SalesRepId).HasMaxLength(50);
            e.Property(x => x.ShippingMethod).HasMaxLength(50);
            e.Property(x => x.CustomerPoNumber).HasMaxLength(50);
            e.HasMany(x => x.Lines).WithOne(l => l.SalesOrder).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);

            // Reference-data links to OM masters (Phase 8 masters wiring).
            e.HasOne<SalesOrderType>().WithMany().HasForeignKey(x => x.SalesOrderTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TaxCode>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TaxExemptionCertificate>().WithMany().HasForeignKey(x => x.TaxExemptionCertificateId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.SalesOrderTypeId);
            e.HasIndex(x => x.TaxCodeId);
            e.HasIndex(x => x.TaxExemptionCertificateId);

            e.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<SalesOrderLine>(e =>
        {
            e.ToTable("SalesOrderLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.UnitOfMeasure).HasMaxLength(10).IsRequired();
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.DiscountPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.TaxPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.ShippedQuantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.AllocatedFreight).HasColumnType("decimal(18,2)").IsRequired();
            e.Ignore(x => x.ExtendedPrice);
            e.Ignore(x => x.DiscountAmount);
            e.Ignore(x => x.TaxAmount);
            e.Ignore(x => x.LineTotal);
            e.HasIndex(x => x.SalesOrderId);
            e.HasIndex(x => x.ItemId);
        });

        modelBuilder.Entity<Shipment>(e =>
        {
            e.ToTable("Shipments");
            e.HasKey(x => x.Id);
            e.Property(x => x.ShipmentNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.Carrier).HasMaxLength(100);
            e.Property(x => x.TrackingNumber).HasMaxLength(100);
            e.Property(x => x.FreightCost).HasColumnType("decimal(18,2)").IsRequired();
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.ShipmentNumber }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.SalesOrderId);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ShipmentLine>(e =>
        {
            e.ToTable("ShipmentLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.UnitOfMeasure).HasMaxLength(10).IsRequired();
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.DiscountPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.TaxPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.HasIndex(x => x.ShipmentId);
            e.HasIndex(x => x.ItemId);
            e.HasIndex(x => x.SalesOrderLineId);
        });

        modelBuilder.Entity<Return>(e =>
        {
            e.ToTable("Returns");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.ReasonCode).HasMaxLength(50);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.ApprovedBy).HasMaxLength(256);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.ReturnNumber }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.ShipmentId);
            e.HasIndex(x => x.SalesOrderId);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ReturnLine>(e =>
        {
            e.ToTable("ReturnLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.UnitOfMeasure).HasMaxLength(10).IsRequired();
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.DiscountPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.TaxPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.RestockDisposition).HasMaxLength(50);
            e.Ignore(x => x.ExtendedPrice);
            e.Ignore(x => x.DiscountAmount);
            e.Ignore(x => x.TaxAmount);
            e.Ignore(x => x.LineTotal);
            e.HasIndex(x => x.ReturnId);
            e.HasIndex(x => x.ItemId);
        });

        modelBuilder.Entity<ShippingMethod>(e =>
        {
            e.ToTable("ShippingMethods");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.Carrier).HasMaxLength(100);
            e.Property(x => x.BaseCost).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.TrackingUrlTemplate).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<SalesRep>(e =>
        {
            e.ToTable("SalesReps");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.CommissionRate).HasColumnType("decimal(9,4)").IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<SalesTerritory>(e =>
        {
            e.ToTable("SalesTerritories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Region).HasMaxLength(100);
            e.Property(x => x.DefaultCommissionRate).HasColumnType("decimal(9,4)").IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<SalesOrderType>(e =>
        {
            e.ToTable("SalesOrderTypes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.TypeCode).HasConversion<int>().IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<PricingRule>(e =>
        {
            e.ToTable("PricingRules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.Scope).HasConversion<int>().IsRequired();
            e.Property(x => x.DiscountPercent).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.UnitPriceOverride).HasColumnType("decimal(18,6)");
            e.Property(x => x.MinimumQuantity).HasColumnType("decimal(18,4)");
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.ItemId);
            e.HasIndex(x => x.ItemCategoryId);
        });

        modelBuilder.Entity<TaxCode>(e =>
        {
            e.ToTable("TaxCodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.Jurisdiction).HasMaxLength(100).IsRequired();
            e.Property(x => x.Rate).HasColumnType("decimal(9,4)").IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasIndex(x => x.Jurisdiction);
        });

        modelBuilder.Entity<TaxExemptionCertificate>(e =>
        {
            e.ToTable("TaxExemptionCertificates");
            e.HasKey(x => x.Id);
            e.Property(x => x.CertificateNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Jurisdiction).HasMaxLength(100).IsRequired();
            e.Property(x => x.ExemptItemsDescription).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Jurisdiction);
        });

        modelBuilder.Entity<BlanketSalesOrder>(e =>
        {
            e.ToTable("BlanketSalesOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.TotalQuantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.TotalValue).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10);
            e.HasMany(x => x.Releases).WithOne().HasForeignKey(x => x.BlanketOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<BlanketRelease>(e =>
        {
            e.ToTable("BlanketReleases");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.Value).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Reference).HasMaxLength(100);
            e.HasIndex(x => x.BlanketOrderId);
        });

        modelBuilder.Entity<BackorderSubstitutionOffer>(e =>
        {
            e.ToTable("BackorderSubstitutionOffers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.ApprovedUnitPrice).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.SalesOrderId });
            e.HasIndex(x => x.SalesOrderLineId);
        });

        modelBuilder.Entity<ReturnToVendor>(e =>
        {
            e.ToTable("ReturnToVendors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.UnitCost).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.Reference).HasMaxLength(100);
            e.HasIndex(x => x.ReturnId);
            e.HasIndex(x => x.VendorId);
        });

        modelBuilder.Entity<SalesOrderNote>(e =>
        {
            e.ToTable("SalesOrderNotes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.Property(x => x.NoteType).HasMaxLength(50).IsRequired();
            e.Property(x => x.AttachmentLink).HasMaxLength(1000);
            e.Property(x => x.CreatedBy).HasMaxLength(100);
            e.HasIndex(x => new { x.CompanyId, x.SalesOrderId });
        });

        modelBuilder.Entity<SalesOrderChangeHistory>(e =>
        {
            e.ToTable("SalesOrderChangeHistories");
            e.HasKey(x => x.Id);
            e.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.ChangeType).HasMaxLength(50).IsRequired();
            e.Property(x => x.FieldName).HasMaxLength(100);
            e.Property(x => x.OldValue).HasMaxLength(1000);
            e.Property(x => x.NewValue).HasMaxLength(1000);
            e.Property(x => x.ReasonCode).HasMaxLength(50);
            e.HasIndex(x => new { x.CompanyId, x.SalesOrderId });
        });

        modelBuilder.Entity<CommissionRun>(e =>
        {
            e.ToTable("CommissionRuns");
            e.HasKey(x => x.Id);
            e.Property(x => x.RunNumber).HasMaxLength(50).IsRequired();
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CommissionRunId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.PeriodStart).IsUnique();
            e.HasIndex(x => x.RunNumber).IsUnique();
        });

        modelBuilder.Entity<CommissionRunLine>(e =>
        {
            e.ToTable("CommissionRunLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.SalesRepCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.RevenueBase).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.CommissionRate).HasColumnType("decimal(9,4)").IsRequired();
            e.Property(x => x.CommissionAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CommissionRunId);

            // One commission line per rep per period — makes duplicate runs impossible at the store level.
            e.HasIndex(x => new { x.PeriodStart, x.SalesRepId }).IsUnique();
        });
    }
}
