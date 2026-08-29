// <copyright file="ArDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class ArDbContext : DispatchableDbContext
{
    public ArDbContext(DbContextOptions<ArDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InvoiceBatch> InvoiceBatches => Set<InvoiceBatch>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<CreditDebitMemo> CreditDebitMemos => Set<CreditDebitMemo>();
    public DbSet<CashReceipt> CashReceipts => Set<CashReceipt>();
    public DbSet<CashReceiptApplication> CashReceiptApplications => Set<CashReceiptApplication>();
    public DbSet<FinanceCharge> FinanceCharges => Set<FinanceCharge>();
    public DbSet<Statement> Statements => Set<Statement>();
    public DbSet<CollectionNote> CollectionNotes => Set<CollectionNote>();
    public DbSet<DunningTemplate> DunningTemplates => Set<DunningTemplate>();
    public DbSet<DoubtfulAccountAllowance> DoubtfulAccountAllowances => Set<DoubtfulAccountAllowance>();
    public DbSet<ResaleCertificate> ResaleCertificates => Set<ResaleCertificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ar");

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomerId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.TaxId).HasMaxLength(50);
            e.Property(x => x.CreditLimit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.TaxExemptCertificate).HasMaxLength(100);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.Ignore(x => x.CurrentBalance);
            e.HasIndex(x => new { x.CompanyId, x.CustomerId }).IsUnique();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.Name);
            e.Property(x => x.SalesRepId).HasColumnName("SalesRepId");
            e.Property(x => x.TaxCodeId).HasColumnName("TaxCodeId");
            e.Property(x => x.TaxExemptionCertificateId).HasColumnName("TaxExemptionCertificateId");
            e.HasIndex(x => x.SalesRepId);
            e.HasIndex(x => x.TaxCodeId);
            e.HasIndex(x => x.TaxExemptionCertificateId);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<InvoiceBatch>(e =>
        {
            e.ToTable("InvoiceBatches");
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.PostingDate).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Invoices).WithOne().HasForeignKey(x => x.InvoiceBatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BatchNumber }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.ToTable("Invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.TotalPaid).HasColumnType("decimal(18,2)").IsRequired();
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.BalanceDue);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.InvoiceBatchId);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => new { x.CustomerId, x.InvoiceNumber });
        });

        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.ToTable("InvoiceLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.Debit);
            e.Ignore(x => x.Credit);
            e.HasOne<Invoice>().WithMany(x => x.Lines).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<CreditDebitMemo>().WithMany(x => x.Lines).HasForeignKey(x => x.CreditDebitMemoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.InvoiceId);
            e.HasIndex(x => x.CreditDebitMemoId);
            e.HasIndex(x => x.AccountId);
        });

        modelBuilder.Entity<CreditDebitMemo>(e =>
        {
            e.ToTable("CreditDebitMemos");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.MemoType).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.Debit);
            e.Ignore(x => x.Credit);
            e.HasIndex(x => x.InvoiceBatchId);
            e.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<CashReceipt>(e =>
        {
            e.ToTable("CashReceipts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceiptReference).HasMaxLength(50).IsRequired();
            e.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.Ignore(x => x.AppliedAmount);
            e.Ignore(x => x.UnappliedAmount);
            e.HasMany(x => x.Applications).WithOne().HasForeignKey(x => x.CashReceiptId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.ReceiptReference }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<CashReceiptApplication>(e =>
        {
            e.ToTable("CashReceiptApplications");
            e.HasKey(x => x.Id);
            e.Property(x => x.AppliedAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CashReceiptId);
            e.HasIndex(x => x.InvoiceId);
        });

        modelBuilder.Entity<FinanceCharge>(e =>
        {
            e.ToTable("FinanceCharges");
            e.HasKey(x => x.Id);
            e.Property(x => x.ChargeNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.ChargeAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.AnnualRate).HasColumnType("decimal(9,6)").IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.ChargeNumber).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Statement>(e =>
        {
            e.ToTable("Statements");
            e.HasKey(x => x.Id);
            e.Property(x => x.StatementNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.StatementNumber).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<CollectionNote>(e =>
        {
            e.ToTable("CollectionNotes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Author).HasMaxLength(256).IsRequired();
            e.Property(x => x.Type).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.RelatedDocumentNumber).HasMaxLength(100);
            e.HasMany(x => x.Activities).WithOne().HasForeignKey(x => x.CollectionNoteId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.AssignedTo);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<CollectionNoteActivity>(e =>
        {
            e.ToTable("CollectionNoteActivities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Author).HasMaxLength(256).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ActivityType).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.CollectionNoteId);
        });

        modelBuilder.Entity<DunningTemplate>(e =>
        {
            e.ToTable("DunningTemplates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Bucket).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.Bucket);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<DoubtfulAccountAllowance>(e =>
        {
            e.ToTable("DoubtfulAccountAllowances");
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.PostedBy).HasMaxLength(256);
            e.HasMany(x => x.Buckets).WithOne().HasForeignKey(x => x.AllowanceRunId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<AllowanceByBucket>(e =>
        {
            e.ToTable("AllowanceByBuckets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasConversion<int>().IsRequired();
            e.Property(x => x.OutstandingBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.ReserveRate).HasColumnType("decimal(9,6)").IsRequired();
            e.Property(x => x.EstimatedAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.AllowanceRunId);
        });

        modelBuilder.Entity<ResaleCertificate>(e =>
        {
            e.ToTable("ResaleCertificates");
            e.HasKey(x => x.Id);
            e.Property(x => x.CertificateNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.IssuedState).HasMaxLength(2).IsRequired();
            e.Property(x => x.DocumentReference).HasMaxLength(500);
            e.HasIndex(x => new { x.CustomerId, x.CertificateNumber }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.ExpiryDate);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });
    }
}
