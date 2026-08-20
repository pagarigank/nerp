// <copyright file="CashDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public class CashDbContext : DispatchableDbContext
{
    public CashDbContext(DbContextOptions<CashDbContext> options) : base(options)
    {
    }

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankContact> BankContacts => Set<BankContact>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<DepositLine> DepositLines => Set<DepositLine>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public DbSet<ReconciliationSession> ReconciliationSessions => Set<ReconciliationSession>();
    public DbSet<BankTransfer> BankTransfers => Set<BankTransfer>();
    public DbSet<BankFee> BankFees => Set<BankFee>();
    public DbSet<NsfRecord> NsfRecords => Set<NsfRecord>();
    public DbSet<BankGlMapping> BankGlMappings => Set<BankGlMapping>();
    public DbSet<LockboxBatch> LockboxBatches => Set<LockboxBatch>();
    public DbSet<StaleCheckEscheatment> StaleCheckEscheatments => Set<StaleCheckEscheatment>();
    public DbSet<PositivePayDiscrepancy> PositivePayExceptions => Set<PositivePayDiscrepancy>();
    public DbSet<BankDuplicateLine> BankDuplicateLines => Set<BankDuplicateLine>();
    public DbSet<BankFeeAnalysis> BankFeeAnalyses => Set<BankFeeAnalysis>();
    public DbSet<PayrollCheckIssue> PayrollCheckIssues => Set<PayrollCheckIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("cash");

        modelBuilder.Entity<BankAccount>(e =>
        {
            e.ToTable("BankAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.RoutingNumber).HasMaxLength(50);
            e.Property(x => x.BankName).HasMaxLength(200);
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.AccountType).HasConversion<int>().IsRequired();
            e.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.CurrentBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Contacts).WithOne().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.AccountCode }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankContact>(e =>
        {
            e.ToTable("BankContacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Title).HasMaxLength(100);
            e.HasIndex(x => x.BankAccountId);
        });

        modelBuilder.Entity<Deposit>(e =>
        {
            e.ToTable("Deposits");
            e.HasKey(x => x.Id);
            e.Property(x => x.DepositNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Reference).HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.Ignore(x => x.TotalAmount);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.DepositNumber }).IsUnique();
            e.HasIndex(x => x.BankAccountId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<DepositLine>(e =>
        {
            e.ToTable("DepositLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Source).HasConversion<int>().IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.DepositId);
            e.HasIndex(x => x.SourceReferenceId);
        });

        modelBuilder.Entity<BankStatement>(e =>
        {
            e.ToTable("BankStatements");
            e.HasKey(x => x.Id);
            e.Property(x => x.StatementNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.BeginningBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.EndingBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Format).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BankStatementId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BankAccountId, x.StatementNumber }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankStatementLine>(e =>
        {
            e.ToTable("BankStatementLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.CheckNumber).HasMaxLength(50);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Balance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.MatchedSource).HasConversion<int>();
            e.HasIndex(x => x.BankStatementId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CheckNumber);
            e.HasIndex(x => x.MatchedTransactionId);
        });

        modelBuilder.Entity<ReconciliationSession>(e =>
        {
            e.ToTable("ReconciliationSessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.BeginningBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.EndingBalance).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Variance).HasColumnType("decimal(18,2)");
            e.Property(x => x.LockedBy).HasMaxLength(256);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.SessionNumber }).IsUnique();
            e.HasIndex(x => x.BankAccountId);
            e.HasIndex(x => x.BankStatementId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankTransfer>(e =>
        {
            e.ToTable("BankTransfers");
            e.HasKey(x => x.Id);
            e.Property(x => x.TransferNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Reference).HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.TransferNumber }).IsUnique();
            e.HasIndex(x => x.FromBankAccountId);
            e.HasIndex(x => x.ToBankAccountId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankFee>(e =>
        {
            e.ToTable("BankFees");
            e.HasKey(x => x.Id);
            e.Property(x => x.FeeNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.FeeType).HasConversion<int>().IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.FeeNumber }).IsUnique();
            e.HasIndex(x => x.BankAccountId);
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<NsfRecord>(e =>
        {
            e.ToTable("NsfRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.NsfNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.NsfFeeAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.BankReference).HasMaxLength(100);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.NsfNumber }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.CashReceiptId);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankGlMapping>(e =>
        {
            e.ToTable("BankGlMappings");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.BankAccountId }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<LockboxBatch>(e =>
        {
            e.ToTable("LockboxBatches");
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.Format).HasMaxLength(20);
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.TotalItems);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.LockboxBatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BatchNumber }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<LockboxItem>(e =>
        {
            e.ToTable("LockboxItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.CustomerName).HasMaxLength(200);
            e.Property(x => x.InvoiceNumber).HasMaxLength(100);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.LockboxBatchId);
        });

        modelBuilder.Entity<StaleCheckEscheatment>(e =>
        {
            e.ToTable("StaleCheckEscheatments");
            e.HasKey(x => x.Id);
            e.Property(x => x.CheckNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Payee).HasMaxLength(200);
            e.Property(x => x.State).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.CheckNumber });
            e.HasIndex(x => x.Status);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<PositivePayDiscrepancy>(e =>
        {
            e.ToTable("PositivePayExceptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.CheckNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.DecisionReason).HasMaxLength(500);
            e.Property(x => x.Decision).HasConversion<int>().IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.CheckNumber });
            e.HasIndex(x => x.Decision);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankDuplicateLine>(e =>
        {
            e.ToTable("BankDuplicateLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.CheckNumber).HasMaxLength(50);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.CheckNumber, x.Amount });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankFeeAnalysis>(e =>
        {
            e.ToTable("BankFeeAnalyses");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.TotalFees);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.AnalysisId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.Year, x.Month }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<BankFeeAnalysisLine>(e =>
        {
            e.ToTable("BankFeeAnalysisLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.FeeType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.HasIndex(x => x.AnalysisId);
        });

        modelBuilder.Entity<PayrollCheckIssue>(e =>
        {
            e.ToTable("PayrollCheckIssues");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
            e.Property(x => x.CheckNumber).HasMaxLength(50);
            e.Property(x => x.BankAccountLast4).HasMaxLength(4);
            e.Property(x => x.PayrollRunId).IsRequired();
            e.Property(x => x.CompanyId).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.PayrollRunId });
            e.HasIndex(x => x.IsReconciled);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });
    }
}
