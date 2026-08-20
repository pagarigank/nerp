// <copyright file="PurchasingDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Infrastructure;

public class PurchasingDbContext : DispatchableDbContext
{
    public PurchasingDbContext(DbContextOptions<PurchasingDbContext> options) : base(options)
    {
    }

    public DbSet<Requisition> Requisitions => Set<Requisition>();

    public DbSet<RequisitionLine> RequisitionLines => Set<RequisitionLine>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();

    public DbSet<VendorItem> VendorItems => Set<VendorItem>();

    public DbSet<VendorItemHistory> VendorItemHistories => Set<VendorItemHistory>();

    public DbSet<BuyerAgent> BuyerAgents => Set<BuyerAgent>();

    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();

    public DbSet<FOBTerm> FOBTerms => Set<FOBTerm>();

    public DbSet<RequisitionTemplate> RequisitionTemplates => Set<RequisitionTemplate>();

    public DbSet<RequisitionTemplateLine> RequisitionTemplateLines => Set<RequisitionTemplateLine>();

    public DbSet<PurchaseOrderTemplate> PurchaseOrderTemplates => Set<PurchaseOrderTemplate>();

    public DbSet<PurchaseOrderTemplateLine> PurchaseOrderTemplateLines => Set<PurchaseOrderTemplateLine>();

    public DbSet<VendorQuote> VendorQuotes => Set<VendorQuote>();

    public DbSet<ReceiptWithoutPO> ReceiptsWithoutPO => Set<ReceiptWithoutPO>();

    public DbSet<ReceiptWithoutPOLine> ReceiptWithoutPOLines => Set<ReceiptWithoutPOLine>();

    public DbSet<OverReceiptApproval> OverReceiptApprovals => Set<OverReceiptApproval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("pur");

        // Requisition
        modelBuilder.Entity<Requisition>(entity =>
        {
            entity.ToTable("Requisitions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RequisitionNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.RequisitionNumber })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RequisitionLine
        modelBuilder.Entity<RequisitionLine>(entity =>
        {
            entity.ToTable("RequisitionLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.EstimatedUnitPrice)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.QuantityConverted)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(e => new { e.RequisitionId, e.LineNumber })
                .IsUnique();
        });

        // PurchaseOrder
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PONumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ShipToName)
                .HasMaxLength(200);

            entity.Property(e => e.ShipToAddress)
                .HasMaxLength(500);

            entity.Property(e => e.BuyerNotes)
                .HasMaxLength(1000);

            entity.Property(e => e.VendorReference)
                .HasMaxLength(100);

            entity.Property(e => e.CancellationReason)
                .HasMaxLength(500);

            entity.Property(e => e.BlanketAmountLimit)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.ReleasedAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.FreightAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.FreightTaxAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.CompanyId, e.PONumber })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseOrderLine
        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("PurchaseOrderLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.QuantityReceived)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.QuantityInvoiced)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.CancellationReason)
                .HasMaxLength(500);

            entity.Property(e => e.TaxCode)
                .HasMaxLength(20);

            entity.Property(e => e.TaxRate)
                .HasColumnType("decimal(8,4)");

            entity.HasIndex(e => new { e.PurchaseOrderId, e.LineNumber })
                .IsUnique();
        });

        // Receipt
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.ToTable("Receipts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReceiptNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ReceivedBy)
                .HasMaxLength(100);

            entity.Property(e => e.PackingSlipNumber)
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.ReversalReason)
                .HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.ReceiptNumber })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReceiptLine
        modelBuilder.Entity<ReceiptLine>(entity =>
        {
            entity.ToTable("ReceiptLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.QuantityReceived)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.LotNumber)
                .HasMaxLength(50);

            entity.Property(e => e.SerialNumber)
                .HasMaxLength(50);

            entity.Property(e => e.InspectionNotes)
                .HasMaxLength(500);

            entity.HasIndex(e => new { e.ReceiptId, e.LineNumber })
                .IsUnique();
        });

        // VendorItem
        modelBuilder.Entity<VendorItem>(entity =>
        {
            entity.ToTable("VendorItems");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.VendorItemCode)
                .HasMaxLength(50);

            entity.Property(e => e.VendorDescription)
                .HasMaxLength(500);

            entity.Property(e => e.Cost)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.MinimumOrderQuantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.LastPurchasePrice)
                .HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.VendorId, e.ItemId })
                .IsUnique();

            entity.HasMany(e => e.History)
                .WithOne()
                .HasForeignKey(h => h.VendorItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VendorItemHistory
        modelBuilder.Entity<VendorItemHistory>(entity =>
        {
            entity.ToTable("VendorItemHistory");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PreviousCost)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.NewCost)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.Notes)
                .HasMaxLength(500);
        });

        // BuyerAgent
        modelBuilder.Entity<BuyerAgent>(entity =>
        {
            entity.ToTable("BuyerAgents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BuyerCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.Property(e => e.Phone)
                .HasMaxLength(50);

            entity.Property(e => e.ApprovalLimit)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.BuyerCode)
                .IsUnique();
        });

        // ShippingMethod
        modelBuilder.Entity<ShippingMethod>(entity =>
        {
            entity.ToTable("ShippingMethods");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CarrierName)
                .HasMaxLength(200);

            entity.Property(e => e.CarrierAccountNumber)
                .HasMaxLength(50);

            entity.Property(e => e.StandardLeadTimeDays)
                .HasColumnType("decimal(8,2)");

            entity.HasIndex(e => e.Code)
                .IsUnique();
        });

        // FOBTerm
        modelBuilder.Entity<FOBTerm>(entity =>
        {
            entity.ToTable("FOBTerms");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.FreightResponsibility)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.RiskTransferPoint)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.Code)
                .IsUnique();
        });

        // RequisitionTemplate
        modelBuilder.Entity<RequisitionTemplate>(entity =>
        {
            entity.ToTable("RequisitionTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TemplateCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.TemplateName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.TemplateCode })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RequisitionTemplateLine
        modelBuilder.Entity<RequisitionTemplateLine>(entity =>
        {
            entity.ToTable("RequisitionTemplateLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.DefaultQuantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(e => new { e.TemplateId, e.LineNumber })
                .IsUnique();
        });

        // PurchaseOrderTemplate
        modelBuilder.Entity<PurchaseOrderTemplate>(entity =>
        {
            entity.ToTable("PurchaseOrderTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TemplateCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.TemplateName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.BlanketAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.AmountUsed)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.CompanyId, e.TemplateCode })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseOrderTemplateLine
        modelBuilder.Entity<PurchaseOrderTemplateLine>(entity =>
        {
            entity.ToTable("PurchaseOrderTemplateLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.DefaultQuantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.TemplateId, e.LineNumber })
                .IsUnique();
        });

        // VendorQuote (RFQ / quote workflow)
        modelBuilder.Entity<VendorQuote>(entity =>
        {
            entity.ToTable("VendorQuotes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RfxNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.QuoteNumber)
                .HasMaxLength(50);

            entity.Property(e => e.QuoteFreight)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.CompanyId, e.RfxNumber })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.VendorQuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VendorQuoteLine
        modelBuilder.Entity<VendorQuoteLine>(entity =>
        {
            entity.ToTable("VendorQuoteLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);
        });

        // ReceiptWithoutPO (spec §6: Receipt-without-PO workflow)
        modelBuilder.Entity<ReceiptWithoutPO>(entity =>
        {
            entity.ToTable("ReceiptsWithoutPO");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReceiptNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ReceivedBy)
                .HasMaxLength(100);

            entity.Property(e => e.PackingSlipNumber)
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.ReversalReason)
                .HasMaxLength(500);

            entity.HasIndex(e => new { e.CompanyId, e.ReceiptNumber })
                .IsUnique();

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReceiptWithoutPOLine
        modelBuilder.Entity<ReceiptWithoutPOLine>(entity =>
        {
            entity.ToTable("ReceiptsWithoutPOLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ItemId)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.QuantityReceived)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,4)");

            entity.HasIndex(e => new { e.ReceiptId, e.LineNumber })
                .IsUnique();
        });

        // OverReceiptApproval (spec §6: Over-receipt exception approval workflow)
        modelBuilder.Entity<OverReceiptApproval>(entity =>
        {
            entity.ToTable("OverReceiptApprovals");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReceiptNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.OrderedQuantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.ReceivedQuantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.OverReceiptTolerance)
                .HasColumnType("decimal(8,4)");

            entity.HasIndex(e => e.ReceiptId);
        });

        modelBuilder.Entity("ERP.Modules.Purchasing.Domain.Entities.ReceiptWithoutPO", b =>
        {
            b.Navigation("Lines");
        });
    }
}
