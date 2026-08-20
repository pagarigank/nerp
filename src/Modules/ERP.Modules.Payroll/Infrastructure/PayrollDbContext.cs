// <copyright file="PayrollDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Infrastructure;

public class PayrollDbContext : DispatchableDbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeCompensation> EmployeeCompensations => Set<EmployeeCompensation>();
    public DbSet<EmployeePayCode> EmployeePayCodes => Set<EmployeePayCode>();
    public DbSet<PayCode> PayCodes => Set<PayCode>();
    public DbSet<UnionCertifiedProfile> UnionCertifiedProfiles => Set<UnionCertifiedProfile>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetLine> TimesheetLines => Set<TimesheetLine>();
    public DbSet<PayrollCalendar> PayrollCalendars => Set<PayrollCalendar>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();
    public DbSet<Garnishment> Garnishments => Set<Garnishment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("pay");

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SsnEncrypted).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.DefaultRole).HasMaxLength(100);
            entity.Property(e => e.AllocationPercentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => new { e.CompanyId, e.EmployeeCode }).IsUnique();
        });

        modelBuilder.Entity<EmployeeCompensation>(entity =>
        {
            entity.ToTable("EmployeeCompensations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PayRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OvertimeRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DoubleTimeRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalaryAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<EmployeePayCode>(entity =>
        {
            entity.ToTable("EmployeePayCodes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OverrideRate).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.PayCodeId);
        });

        modelBuilder.Entity<PayCode>(entity =>
        {
            entity.ToTable("PayCodes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GlAccountNumber).HasMaxLength(20);
            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<UnionCertifiedProfile>(entity =>
        {
            entity.ToTable("UnionCertifiedProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TradeClassification).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Jurisdiction).HasMaxLength(100);
            entity.Property(e => e.UnionLocal).HasMaxLength(100);
            entity.Property(e => e.PrevailingWageRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FringeBenefitRate).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => new { e.CompanyId, e.TradeClassification, e.Jurisdiction });
        });

        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.ToTable("Timesheets");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.TotalHours);
            entity.Ignore(e => e.TotalRegularHours);
            entity.Ignore(e => e.TotalOvertimeHours);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.TimesheetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimesheetLine>(entity =>
        {
            entity.ToTable("TimesheetLines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Hours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Rate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TradeClassification).HasMaxLength(100);
            entity.HasIndex(e => e.TimesheetId);
            entity.HasIndex(e => e.ProjectId);
        });

        modelBuilder.Entity<PayrollCalendar>(entity =>
        {
            entity.ToTable("PayrollCalendars");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EmployerFicaRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.EmployeeFicaRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.FutaRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.SutaRate).HasColumnType("decimal(9,6)");
            entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.ToTable("PayrollRuns");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.TotalGross);
            entity.Ignore(e => e.TotalEmployeeTax);
            entity.Ignore(e => e.TotalDeductions);
            entity.Ignore(e => e.TotalNet);
            entity.Ignore(e => e.TotalEmployerTax);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollRunLine>(entity =>
        {
            entity.ToTable("PayrollRunLines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegularHours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RegularRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OvertimeRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GrossPay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EmployeeTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Deductions).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EmployerTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NetPay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrevailingWageRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FringeRate).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.FringeCost);
            entity.Ignore(e => e.TotalPrevailingRate);
            entity.Property(e => e.TradeClassification).HasMaxLength(100);
            entity.HasIndex(e => e.PayrollRunId);
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<Garnishment>(entity =>
        {
            entity.ToTable("Garnishments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CaseNumber).HasMaxLength(100);
            entity.Property(e => e.DisposableIncomePercent).HasColumnType("decimal(9,4)");
            entity.Property(e => e.FixedAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.IsActive });
        });
    }
}
