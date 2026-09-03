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
    }
}