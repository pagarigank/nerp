// <copyright file="EInvoiceDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Database context for e-Invoice module.
/// Tables are in the 'int' schema.
/// </summary>
public class EInvoiceDbContext : DbContext
{
    public EInvoiceDbContext(DbContextOptions<EInvoiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaxpayerProfile> TaxpayerProfiles => Set<TaxpayerProfile>();
    public DbSet<EInvoiceDocument> EInvoiceDocuments => Set<EInvoiceDocument>();
    public DbSet<DigitalCertificate> DigitalCertificates => Set<DigitalCertificate>();
    public DbSet<EInvoiceCustomerExtension> EInvoiceCustomerExtensions => Set<EInvoiceCustomerExtension>();
    public DbSet<EInvoiceVendorExtension> EInvoiceVendorExtensions => Set<EInvoiceVendorExtension>();
    public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();
    public DbSet<EInvoiceStatusHistory> EInvoiceStatusHistories => Set<EInvoiceStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("int");

        // TaxpayerProfile configuration
        modelBuilder.Entity<TaxpayerProfile>(entity =>
        {
            entity.HasIndex(e => e.TaxIdentificationNumber).IsUnique();
        });

        // EInvoiceDocument configuration
        modelBuilder.Entity<EInvoiceDocument>(entity =>
        {
            entity.HasIndex(e => e.MyInvoisUuid).IsUnique();
            entity.HasIndex(e => new { e.SourceDocumentId, e.SourceDocumentType });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasIndex(e => new { e.IsSandbox, e.Status });
        });

        // DigitalCertificate configuration
        modelBuilder.Entity<DigitalCertificate>(entity =>
        {
            entity.HasIndex(e => e.Thumbprint);
            entity.HasIndex(e => new { e.NotAfter, e.IsActive });
        });

        // EInvoiceCustomerExtension configuration (schema: ar)
        modelBuilder.Entity<EInvoiceCustomerExtension>(entity =>
        {
            entity.ToTable("einvoice_customer_extension", "ar");
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.TaxIdentificationNumber);
        });

        // EInvoiceVendorExtension configuration (schema: ap)
        modelBuilder.Entity<EInvoiceVendorExtension>(entity =>
        {
            entity.ToTable("einvoice_vendor_extension", "ap");
            entity.HasIndex(e => e.VendorId);
            entity.HasIndex(e => e.TaxIdentificationNumber);
        });

        // ApiCredential configuration
        modelBuilder.Entity<ApiCredential>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.Environment });
            entity.HasIndex(e => e.IsActive);
        });

        // EInvoiceStatusHistory configuration
        modelBuilder.Entity<EInvoiceStatusHistory>(entity =>
        {
            entity.HasIndex(e => e.EInvoiceDocumentId);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.EInvoiceDocumentId, e.OccurredAt });
        });
    }
}