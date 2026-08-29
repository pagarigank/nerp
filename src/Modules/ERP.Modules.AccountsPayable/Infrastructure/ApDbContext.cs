// <copyright file="ApDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class ApDbContext : DispatchableDbContext
{
    public ApDbContext(DbContextOptions<ApDbContext> options) : base(options)
    {
    }

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorBankAccount> VendorBankAccounts => Set<VendorBankAccount>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    public DbSet<VoucherBatch> VoucherBatches => Set<VoucherBatch>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherDistribution> VoucherDistributions => Set<VoucherDistribution>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentLine> PaymentLines => Set<PaymentLine>();
    public DbSet<GoodsReceiptMatch> GoodsReceiptMatches => Set<GoodsReceiptMatch>();
    public DbSet<CommissionAccrual> CommissionAccruals => Set<CommissionAccrual>();
    public DbSet<DuplicateInvoiceCheck> DuplicateInvoiceChecks => Set<DuplicateInvoiceCheck>();
    public DbSet<VendorW9> VendorW9Records => Set<VendorW9>();
    public DbSet<VendorBankVerification> VendorBankVerifications => Set<VendorBankVerification>();
    public DbSet<CashDiscountCapture> CashDiscountCaptures => Set<CashDiscountCapture>();
    public DbSet<StaleCheckEscheatment> StaleCheckEscheatments => Set<StaleCheckEscheatment>();
    public DbSet<GrirAccrual> GrirAccruals => Set<GrirAccrual>();
    public DbSet<VendorStatement> VendorStatements => Set<VendorStatement>();
    public DbSet<Ap1099Classification> Ap1099Classifications => Set<Ap1099Classification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ap");

        modelBuilder.Entity<PaymentTerm>(e =>
        {
            e.ToTable("PaymentTerms");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.DueDays).IsRequired();
            e.Property(x => x.DiscountDays).IsRequired();
            e.Property(x => x.DiscountPercent).HasColumnType("decimal(9,6)").IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.ToTable("Vendors");
            e.HasKey(x => x.Id);
            e.Property(x => x.VendorId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.TaxId).HasMaxLength(50);
            e.Property(x => x.Form1099Category).HasConversion<int>();
            e.Property(x => x.BackupWithholdingRate).HasColumnType("decimal(18,2)");
            e.Property(x => x.InsuranceCarrier).HasMaxLength(200);
            e.Property(x => x.InsurancePolicyNumber).HasMaxLength(100);
            e.Property(x => x.DiversityClassification).HasMaxLength(100);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.BankAccounts).WithOne().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.VendorId }).IsUnique();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.Name);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<VendorBankAccount>(e =>
        {
            e.ToTable("VendorBankAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.BankName).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.RoutingNumber).HasMaxLength(50);
            e.HasIndex(x => x.VendorId);
        });

        modelBuilder.Entity<VoucherBatch>(e =>
        {
            e.ToTable("VoucherBatches");
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.PostingDate).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Vouchers).WithOne(v => v.VoucherBatch).HasForeignKey(x => x.VoucherBatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BatchNumber }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Voucher>(e =>
        {
            e.ToTable("Vouchers");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.VoucherType).HasConversion<int>().IsRequired();
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Form1099Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.BackupWithholdingAmount).HasColumnType("decimal(18,2)");
            e.HasMany(x => x.Distributions).WithOne(d => d.Voucher).HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.VoucherBatchId);
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => new { x.VendorId, x.InvoiceNumber });
        });

        modelBuilder.Entity<VoucherDistribution>(e =>
        {
            e.ToTable("VoucherDistributions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Debit).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Credit).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.VoucherId);
            e.HasIndex(x => x.AccountId);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentReference).HasMaxLength(50).IsRequired();
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.PaymentMethod).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.Ignore(x => x.TotalAmount);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.PaymentReference }).IsUnique();
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<PaymentLine>(e =>
        {
            e.ToTable("PaymentLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.AppliedAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.PaymentId);
            e.HasIndex(x => x.VoucherId);
        });

        modelBuilder.Entity<GoodsReceiptMatch>(e =>
        {
            e.ToTable("GoodsReceiptMatches");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.ItemId).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.UnitOfMeasure).HasMaxLength(20);
            e.Property(x => x.QuantityReceived).HasColumnType("decimal(18,4)").IsRequired();
            e.HasIndex(x => x.ReceiptId);
            e.HasIndex(x => x.PurchaseOrderId);
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.OverReceiptFlag);
        });

        modelBuilder.Entity<CommissionAccrual>(e =>
        {
            e.ToTable("CommissionAccruals");
            e.HasKey(x => x.Id);
            e.Property(x => x.ShipmentNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.SalesOrderNumber).HasMaxLength(50);
            e.Property(x => x.BaseAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.CommissionRate).HasColumnType("decimal(9,6)").IsRequired();
            e.Property(x => x.CommissionAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.SalesRepId);
            e.HasIndex(x => x.ShipmentId);
            e.HasIndex(x => x.VendorId);
        });

        modelBuilder.Entity<DuplicateInvoiceCheck>(e =>
        {
            e.ToTable("DuplicateInvoiceChecks");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.InvoiceNumber);
        });

        modelBuilder.Entity<VendorW9>(e =>
        {
            e.ToTable("VendorW9Records");
            e.HasKey(x => x.Id);
            e.Property(x => x.TaxId).HasMaxLength(50).IsRequired();
            e.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
            e.Property(x => x.TinMatchStatus).HasMaxLength(50);
            e.HasIndex(x => x.VendorId);
        });

        modelBuilder.Entity<VendorBankVerification>(e =>
        {
            e.ToTable("VendorBankVerifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.RoutingNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.AccountNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.VendorBankAccountId);
        });

        modelBuilder.Entity<CashDiscountCapture>(e =>
        {
            e.ToTable("CashDiscountCaptures");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.DiscountAvailable).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.DiscountTaken).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.VoucherId);
        });

        modelBuilder.Entity<StaleCheckEscheatment>(e =>
        {
            e.ToTable("StaleCheckEscheatments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.PaymentId);
        });

        modelBuilder.Entity<GrirAccrual>(e =>
        {
            e.ToTable("GrirAccruals");
            e.HasKey(x => x.Id);
            e.Property(x => x.AccrualAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.FiscalPeriodId);
        });

        modelBuilder.Entity<VendorStatement>(e =>
        {
            e.ToTable("VendorStatements");
            e.HasKey(x => x.Id);
            e.Property(x => x.StatementNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.StatementTotal).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.VendorStatementId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.VendorId);
        });

        modelBuilder.Entity<VendorStatementLine>(e =>
        {
            e.ToTable("VendorStatementLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reference).HasMaxLength(100).IsRequired();
            e.Property(x => x.StatementAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.BookAmount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => x.VendorStatementId);
        });

        modelBuilder.Entity<Ap1099Classification>(e =>
        {
            e.ToTable("Ap1099Classifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.FormType).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.VendorId);
            e.HasIndex(x => x.TaxYear);
        });
    }
}
