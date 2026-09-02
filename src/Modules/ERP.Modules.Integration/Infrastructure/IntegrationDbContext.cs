// <copyright file="IntegrationDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Integration.Domain.Entities;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Integration.Infrastructure;

public class IntegrationDbContext : DispatchableDbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options)
    {
    }

    public DbSet<TradingPartner> TradingPartners => Set<TradingPartner>();
    public DbSet<EdiDocumentMapping> EdiDocumentMappings => Set<EdiDocumentMapping>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportJobLine> ImportJobLines => Set<ImportJobLine>();
    public DbSet<EdiTransactionLog> EdiTransactionLogs => Set<EdiTransactionLog>();
    public DbSet<EdiAck> EdiAcks => Set<EdiAck>();
    public DbSet<FileArchivalPolicy> FileArchivalPolicies => Set<FileArchivalPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("int");

        modelBuilder.Entity<TradingPartner>(e =>
        {
            e.ToTable("TradingPartners");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.PartnerCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.IsaId).HasMaxLength(15);
            e.Property(x => x.GsSenderId).HasMaxLength(15);
            e.Property(x => x.GsReceiverId).HasMaxLength(15);
            e.Property(x => x.Protocol).HasMaxLength(50);
            e.Property(x => x.CertificateThumbprint).HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.PartnerCode }).IsUnique();
        });

        modelBuilder.Entity<EdiDocumentMapping>(e =>
        {
            e.ToTable("EdiDocumentMappings");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentType).IsRequired().HasMaxLength(10);
            e.Property(x => x.DocumentName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Version).HasMaxLength(10);
            e.Property(x => x.Direction).IsRequired().HasMaxLength(10);
            e.Property(x => x.MappingJson).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.DocumentType, x.Direction });
        });

        modelBuilder.Entity<WebhookSubscription>(e =>
        {
            e.ToTable("WebhookSubscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            e.Property(x => x.TargetUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.Secret).HasMaxLength(500);
            e.Property(x => x.PayloadFormat).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.EventType });
        });

        modelBuilder.Entity<WebhookDeliveryLog>(e =>
        {
            e.ToTable("WebhookDeliveryLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            e.Property(x => x.TargetUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.ResponseBody).HasMaxLength(4000);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DeliveredOn);
        });

        modelBuilder.Entity<ImportJob>(e =>
        {
            e.ToTable("ImportJobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.JobName).IsRequired().HasMaxLength(200);
            e.Property(x => x.FileFormat).IsRequired().HasMaxLength(10);
            e.Property(x => x.Destination).IsRequired().HasMaxLength(100);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.FileName).HasMaxLength(200);
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.CompanyId, x.CreatedOn });
        });

        modelBuilder.Entity<ImportJobLine>(e =>
        {
            e.ToTable("ImportJobLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowData).IsRequired();
            e.Property(x => x.ValidationStatus).IsRequired().HasMaxLength(20);
            e.Property(x => x.Errors).HasMaxLength(4000);
            e.HasIndex(x => new { x.ImportJobId, x.RowNumber });
        });

        modelBuilder.Entity<EdiTransactionLog>(e =>
        {
            e.ToTable("EdiTransactionLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.IsaControlNumber).HasMaxLength(15);
            e.Property(x => x.GsControlNumber).HasMaxLength(15);
            e.Property(x => x.StControlNumber).HasMaxLength(15);
            e.Property(x => x.DocumentType).IsRequired().HasMaxLength(10);
            e.Property(x => x.Direction).IsRequired().HasMaxLength(10);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.PartnerCode).HasMaxLength(50);
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.Property(x => x.RawContent).HasMaxLength(8000);
            e.HasIndex(x => x.DocumentType);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ProcessedOn);
        });

        modelBuilder.Entity<EdiAck>(e =>
        {
            e.ToTable("EdiAcks");
            e.HasKey(x => x.Id);
            e.Property(x => x.AckType).IsRequired().HasMaxLength(10);
            e.Property(x => x.OriginalIsaControlNumber).HasMaxLength(15);
            e.Property(x => x.OriginalGsControlNumber).HasMaxLength(15);
            e.Property(x => x.OriginalStControlNumber).HasMaxLength(15);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.RawContent).HasMaxLength(8000);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ProcessedOn);
        });

        modelBuilder.Entity<FileArchivalPolicy>(e =>
        {
            e.ToTable("FileArchivalPolicies");
            e.HasKey(x => x.Id);
            e.Property(x => x.IntegrationType).IsRequired().HasMaxLength(50);
            e.Property(x => x.RetentionDays).IsRequired();
            e.Property(x => x.ArchiveLocation).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.IntegrationType }).IsUnique();
        });
    }
}
