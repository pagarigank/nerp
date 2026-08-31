// <copyright file="PlatformDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Infrastructure;

public class PlatformDbContext : DispatchableDbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<SegmentType> SegmentTypes => Set<SegmentType>();
    public DbSet<SegmentValue> SegmentValues => Set<SegmentValue>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<SoDRule> SoDRules => Set<SoDRule>();
    public DbSet<SoDConflict> SoDConflicts => Set<SoDConflict>();
    public DbSet<PendingAuditLog> PendingAuditLogs => Set<PendingAuditLog>();
    public DbSet<ValidatedCombination> ValidatedCombinations => Set<ValidatedCombination>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<ApprovalEscalationPolicy> ApprovalEscalationPolicies => Set<ApprovalEscalationPolicy>();
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    public DbSet<UserAccessRequest> UserAccessRequests => Set<UserAccessRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");

        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
            e.Property(x => x.BaseCurrency).HasMaxLength(10).IsRequired();
            e.Property(x => x.TaxId).HasMaxLength(50);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.ParentCompanyId).IsRequired(false);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.ParentCompanyId);
            e.HasOne(x => x.ParentCompany)
                .WithMany(x => x.ChildCompanies)
                .HasForeignKey(x => x.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<FiscalYear>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Year }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<FiscalPeriod>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.FiscalYearId, x.PeriodNumber }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<SegmentType>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<SegmentValue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(30).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.SegmentTypeId, x.Value }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.AccountNumber }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Currency>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Symbol).HasMaxLength(10).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ExchangeRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FromCurrency).HasMaxLength(10).IsRequired();
            e.Property(x => x.ToCurrency).HasMaxLength(10).IsRequired();
            e.Property(x => x.Rate).HasPrecision(18, 8);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.FromCurrency, x.ToCurrency, x.EffectiveDate });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<NumberSequence>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Prefix).HasMaxLength(20);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.Permissions)
                .WithOne()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Page).HasMaxLength(256).IsRequired();
            e.Property(x => x.Code).HasMaxLength(256).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasMany(x => x.Roles)
                .WithOne()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CompanyId).IsRequired(false);
            e.HasIndex(x => new { x.UserId, x.RoleId, x.CompanyId }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.PerformedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.Property(x => x.OldValues).HasMaxLength(4000);
            e.Property(x => x.NewValues).HasMaxLength(4000);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.PerformedOn);
            e.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<ApprovalWorkflow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.ThresholdAmount).HasPrecision(18, 2);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.Module, x.DocumentType });
            e.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ApprovalStep>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.MinAmount).HasPrecision(18, 2);
            e.Property(x => x.MaxAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.WorkflowId, x.StepOrder });
        });

        modelBuilder.Entity<ApprovalRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.DocumentNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.Module, x.DocumentType, x.Status });
            e.HasIndex(x => x.DocumentId);
            e.HasMany(x => x.Actions)
                .WithOne()
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ApprovalAction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ActionedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.Comments).HasMaxLength(2000);
            e.HasIndex(x => new { x.RequestId, x.ActionedOn });
        });

        modelBuilder.Entity<SoDRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.ActionA).HasMaxLength(50).IsRequired();
            e.Property(x => x.ActionB).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.DocumentType).HasMaxLength(100);
            e.Property(x => x.ThresholdAmount).HasPrecision(18, 2);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.Module, x.ActionA, x.ActionB }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<SoDConflict>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(256).IsRequired();
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.ConflictType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Resolution).HasMaxLength(2000);
            e.Property(x => x.ResolvedBy).HasMaxLength(256);
            e.HasIndex(x => x.DetectedOn);
            e.HasIndex(x => new { x.UserId, x.Resolved });
        });

        modelBuilder.Entity<PendingAuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.PerformedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.Property(x => x.OldValues).HasMaxLength(4000);
            e.Property(x => x.NewValues).HasMaxLength(4000);
            e.ToTable("PendingAuditLogs");
        });

        modelBuilder.Entity<ValidatedCombination>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CombinationKey).HasMaxLength(500).IsRequired();
            e.Property(x => x.SegmentValuesJson).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.CombinationKey }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<FiscalYear>(e =>
        {
            e.Property(x => x.CalendarType).HasConversion<int>().IsRequired();
            e.Property(x => x.YearEndType).HasConversion<int>().IsRequired();
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.OwnerUserId).HasMaxLength(256).IsRequired();
            e.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.KeyPrefix).HasMaxLength(16).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
            e.HasIndex(x => x.KeyHash).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ApprovalDelegation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Module).HasMaxLength(50);
            e.Property(x => x.DocumentType).HasMaxLength(100);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.DelegatorUserId, x.DelegateUserId, x.IsActive });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<ApprovalEscalationPolicy>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.WorkflowId, x.StepOrder });
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<HolidayCalendar>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.Date }).IsUnique();
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });

        modelBuilder.Entity<UserAccessRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.RequestedRole).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(50);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.ReviewNotes).HasMaxLength(1000);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.Property(x => x.DeletedBy).HasMaxLength(256);
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Email);
            e.HasQueryFilter(x => !x.DeletedOn.HasValue);
        });
    }
}
