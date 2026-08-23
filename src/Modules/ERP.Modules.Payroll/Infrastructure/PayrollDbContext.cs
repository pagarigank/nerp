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
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<ExpenseReportLine> ExpenseReportLines => Set<ExpenseReportLine>();
    public DbSet<DeductionBenefit> DeductionBenefits => Set<DeductionBenefit>();
    public DbSet<EmployeeDeductionBenefit> EmployeeDeductionBenefits => Set<EmployeeDeductionBenefit>();
    public DbSet<W4Record> W4Records => Set<W4Record>();
    public DbSet<WageBaseLimit> WageBaseLimits => Set<WageBaseLimit>();
    public DbSet<WorkersCompClassCode> WorkersCompClassCodes => Set<WorkersCompClassCode>();
    public DbSet<TaxTable> TaxTables => Set<TaxTable>();
    public DbSet<TaxJurisdiction> TaxJurisdictions => Set<TaxJurisdiction>();
    public DbSet<EmployeeTaxProfile> EmployeeTaxProfiles => Set<EmployeeTaxProfile>();
    public DbSet<PtoLedger> PtoLedgers => Set<PtoLedger>();
    public DbSet<ManualCheck> ManualChecks => Set<ManualCheck>();
    public DbSet<PayrollCheck> PayrollChecks => Set<PayrollCheck>();
    public DbSet<DirectDeposit> DirectDeposits => Set<DirectDeposit>();
    public DbSet<CompanyPayrollSetup> CompanyPayrollSetups => Set<CompanyPayrollSetup>();
    public DbSet<PtoPolicy> PtoPolicies => Set<PtoPolicy>();
    public DbSet<NewHireReportingConfig> NewHireReportingConfigs => Set<NewHireReportingConfig>();
    public DbSet<AchReturn> AchReturns => Set<AchReturn>();
    public DbSet<TaxDepositSchedule> TaxDepositSchedules => Set<TaxDepositSchedule>();

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
            entity.Property(e => e.AddressLine1).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateCode).HasMaxLength(10);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
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
            entity.HasIndex(e => e.ApprovalRequestId);
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
            entity.HasOne(e => e.PayrollRun)
                .WithMany(r => r.Lines)
                .HasForeignKey(e => e.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<ExpenseReport>(entity =>
        {
            entity.ToTable("ExpenseReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<ExpenseReportLine>(entity =>
        {
            entity.ToTable("ExpenseReportLines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.GlAccountNumber).HasMaxLength(20);
            entity.Property(e => e.MileageMiles).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MileageRate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.PerDiemDays).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PerDiemRate).HasColumnType("decimal(18,4)");
            entity.HasIndex(e => e.ExpenseReportId);
            entity.HasIndex(e => e.ProjectId);
        });

        modelBuilder.Entity<DeductionBenefit>(entity =>
        {
            entity.ToTable("DeductionBenefits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.DefaultRate).HasColumnType("decimal(9,4)");
            entity.Property(e => e.GlAccountNumber).HasMaxLength(20);
            entity.HasIndex(e => e.CompanyId);
        });

        modelBuilder.Entity<EmployeeDeductionBenefit>(entity =>
        {
            entity.ToTable("EmployeeDeductionBenefits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Percent).HasColumnType("decimal(9,4)");
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.DeductionBenefitId);
        });

        modelBuilder.Entity<W4Record>(entity =>
        {
            entity.ToTable("W4Records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdditionalWithholding).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherIncome).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Deductions).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<WageBaseLimit>(entity =>
        {
            entity.ToTable("WageBaseLimits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LimitAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SurtaxThreshold).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => new { e.CompanyId, e.Year, e.Type });
        });

        modelBuilder.Entity<WorkersCompClassCode>(entity =>
        {
            entity.ToTable("WorkersCompClassCodes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClassCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.State).HasMaxLength(10);
            entity.Property(e => e.RatePer100).HasColumnType("decimal(9,4)");
            entity.Property(e => e.ExperienceModification).HasColumnType("decimal(9,4)");
            entity.HasIndex(e => e.CompanyId);
        });

        modelBuilder.Entity<TaxTable>(entity =>
        {
            entity.ToTable("TaxTables");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StateCode).HasMaxLength(10);
            entity.Property(e => e.StandardDeduction).HasColumnType("decimal(18,2)");
            entity.HasMany(e => e.Brackets).WithOne().HasForeignKey("TaxTableId").OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CompanyId, e.Year, e.Level, e.StateCode, e.FilingStatus });
        });

        modelBuilder.Entity<TaxBracket>(entity =>
        {
            entity.ToTable("TaxBrackets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.LowerBound).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UpperBound).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FixedAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.TaxTableId);
        });

        modelBuilder.Entity<TaxJurisdiction>(entity =>
        {
            entity.ToTable("TaxJurisdictions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StateCode).HasMaxLength(10);
            entity.Property(e => e.ReciprocalWithState).HasMaxLength(10);
            entity.Property(e => e.LocalRate).HasColumnType("decimal(9,6)");
            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<EmployeeTaxProfile>(entity =>
        {
            entity.ToTable("EmployeeTaxProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResidentState).HasMaxLength(10);
            entity.Property(e => e.WorkState).HasMaxLength(10);
            entity.Property(e => e.AdditionalFederalWithholding).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AdditionalStateWithholding).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => new { e.CompanyId, e.EmployeeId }).IsUnique();
        });

        modelBuilder.Entity<PtoLedger>(entity =>
        {
            entity.ToTable("PtoLedgers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccrualRate).HasColumnType("decimal(9,4)");
            entity.Property(e => e.MaxAccrual).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CarryoverLimit).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Accrued).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Used).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<PtoTransaction>(entity =>
        {
            entity.ToTable("PtoTransactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Hours).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.PtoLedgerId);
        });

        modelBuilder.Entity<ManualCheck>(entity =>
        {
            entity.ToTable("ManualChecks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GrossPay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NetPay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Reason).HasMaxLength(300);
            entity.Property(e => e.CheckNumber).HasMaxLength(30);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<PayrollCheck>(entity =>
        {
            entity.ToTable("PayrollChecks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NetPay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CheckNumber).HasMaxLength(30);
            entity.Property(e => e.AchTraceNumber).HasMaxLength(50);
            entity.HasIndex(e => e.PayrollRunId);
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<DirectDeposit>(entity =>
        {
            entity.ToTable("DirectDeposits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BankName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoutingNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AccountNumberEncrypted).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AccountType).HasMaxLength(20);
            entity.Property(e => e.AllocationPercentage).HasColumnType("decimal(9,4)");
            entity.Property(e => e.FixedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrenoteSentOn);
            entity.Property(e => e.VerifiedOn);
            entity.HasIndex(e => new { e.CompanyId, e.EmployeeId });
        });

        modelBuilder.Entity<CompanyPayrollSetup>(entity =>
        {
            entity.ToTable("CompanyPayrollSetups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ein).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FederalTaxId).HasMaxLength(20);
            entity.Property(e => e.StateTaxId).HasMaxLength(20);
            entity.Property(e => e.SutaState).HasMaxLength(10);
            entity.Property(e => e.EftpsPin).HasMaxLength(20);
            entity.Property(e => e.DepositSchedule).HasMaxLength(20);
            entity.Property(e => e.SocialSecurityRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.MedicareRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.FutaRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.SutaRate).HasColumnType("decimal(9,6)");
            entity.Property(e => e.OpenAccrualAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OpenAccrualEmployerTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OpenAccrualBatchRef).HasMaxLength(50);
            entity.HasIndex(e => e.CompanyId).IsUnique();
        });

        modelBuilder.Entity<PtoPolicy>(entity =>
        {
            entity.ToTable("PtoPolicies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccrualRate).HasColumnType("decimal(9,4)");
            entity.Property(e => e.AccrualBasis).HasMaxLength(20);
            entity.Property(e => e.MaxAccrual).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CarryoverLimit).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CashOutRate).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<NewHireReportingConfig>(entity =>
        {
            entity.ToTable("NewHireReportingConfigs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StateCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.AgencyName).HasMaxLength(200);
            entity.Property(e => e.TransmissionMethod).HasMaxLength(50);
            entity.Property(e => e.SftpEndpoint).HasMaxLength(300);
            entity.Property(e => e.AgencyId).HasMaxLength(50);
            entity.HasIndex(e => new { e.CompanyId, e.StateCode }).IsUnique();
        });

        modelBuilder.Entity<AchReturn>(entity =>
        {
            entity.ToTable("AchReturns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TraceNumber).HasMaxLength(50);
            entity.Property(e => e.ReturnCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReturnAction).HasMaxLength(20);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.PayrollRunId);
        });

        modelBuilder.Entity<TaxDepositSchedule>(entity =>
        {
            entity.ToTable("TaxDepositSchedules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaxType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Agency).HasMaxLength(50);
            entity.Property(e => e.EstimatedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DepositedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Frequency).HasMaxLength(20);
            entity.Property(e => e.FormType).HasMaxLength(20);
            entity.Property(e => e.PaidVoucherId);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.DepositDate);
        });
    }
}
