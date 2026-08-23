// <copyright file="ApVoucherCreator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Infrastructure;

/// <summary>
/// Cross-module wiring (Phase 11 item #1101): turns an approved expense reimbursement
/// into an Accounts-Payable voucher with the employee recorded as the vendor payee.
/// Mirrors the guidance in the project task list — expense reimbursement -> AP voucher (employee as
/// payee) so the reimbursement flows through the normal AP payment run and GL posting.
/// Payroll holds a one-way reference to AP (AP does not reference Payroll, so no cycle).
/// </summary>
public sealed class ApVoucherCreator
{
    private readonly ApDbContext _apContext;
    private readonly IVoucherService _voucherService;
    private readonly PlatformDbContext _platformContext;
    private readonly PayrollDbContext _payrollContext;

    public ApVoucherCreator(
        ApDbContext apContext,
        IVoucherService voucherService,
        PlatformDbContext platformContext,
        PayrollDbContext payrollContext)
    {
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _payrollContext = payrollContext ?? throw new ArgumentNullException(nameof(payrollContext));
    }

    public async Task<Guid> CreateReimbursementVoucherAsync(
        ExpenseReport report,
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        // Resolve (or lazily create) an AP vendor representing this employee, keyed by a
        // deterministic vendor code so repeated reimbursements reuse the same payee.
        var vendorCode = $"EMP-{employee.EmployeeCode}";
        var vendor = await _apContext.Vendors
            .FirstOrDefaultAsync(v => v.VendorId == vendorCode, cancellationToken);
        if (vendor is null)
        {
            vendor = new Vendor(
                vendorCode,
                $"{employee.FirstName} {employee.LastName}",
                $"{employee.FirstName} {employee.LastName}",
                null,
                null,
                null,
                true);
            _apContext.Vendors.Add(vendor);
            await _apContext.SaveChangesAsync(cancellationToken);
        }

        // Resolve the fiscal period for the reimbursement date.
        var reimbDate = report.SubmittedAt ?? report.ReportDate;
        var period = await _platformContext.FiscalPeriods
            .OrderByDescending(p => p.EndDate)
            .FirstOrDefaultAsync(p => p.CompanyId == report.CompanyId && p.StartDate <= reimbDate && p.EndDate >= reimbDate, cancellationToken)
            ?? await _platformContext.FiscalPeriods
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefaultAsync(p => p.CompanyId == report.CompanyId, cancellationToken);
        if (period is null)
            throw new InvalidOperationException("No fiscal period found for the reimbursement company.");

        // Build one AP distribution per expense line, debiting the employee-expense account,
        // plus a single balancing credit to the Accounts Payable control account (the vendor payable).
        var expenseAccountId = await ResolveExpenseAccountAsync(report.CompanyId, cancellationToken);
        var apLiabilityId = await ResolveApLiabilityAccountAsync(report.CompanyId, cancellationToken);
        var distributions = new List<VoucherDistributionDto>();
        foreach (var line in report.Lines)
        {
            distributions.Add(new VoucherDistributionDto(expenseAccountId, line.Amount, null, line.ProjectId, line.TaskId));
        }

        distributions.Add(new VoucherDistributionDto(apLiabilityId, null, report.TotalAmount, null, null));

        var batch = await _voucherService.CreateVoucherBatchAsync(
            report.CompanyId,
            $"EXP-REIMB-{report.Id:N}",
            $"Expense reimbursement {report.Id:N} for {employee.FirstName} {employee.LastName}",
            new DateTimeOffset(reimbDate),
            period.Id,
            cancellationToken);

        var voucher = await _voucherService.AddVoucherToBatchAsync(
            batch.Id,
            vendor.Id,
            VoucherType.Invoice,
            $"REIMB-{report.Id:N}",
            new DateTimeOffset(reimbDate),
            new DateTimeOffset(reimbDate).AddDays(30),
            report.TotalAmount,
            0m,
            $"Expense reimbursement for report {report.Id:N}",
            null,
            null,
            null,
            0m,
            0m,
            distributions,
            cancellationToken);

        // Release (to Batched) then post so the AP voucher is recorded as a posted payable.
        await _voucherService.ReleaseBatchAsync(batch.Id, cancellationToken);
        await _voucherService.PostBatchAsync(batch.Id, cancellationToken);
        return voucher.Id;
    }

    /// <summary>
    /// Phase 11 liability payment: creates ONE posted AP voucher per tax-agency/benefit
    /// vendor for a payroll liability settlement. Resolves (or lazily creates) the agency
    /// vendor by code convention ('EFTPS-FED' federal, 'DOR-{state}' state revenue
    /// agencies, 'BENEFIT-REMIT' benefit-plan vendors). The voucher debits the
    /// payroll-liability GL account and credits the AP control account, so GL relief
    /// happens through the normal AP posting path; the AP payment run settles cash.
    /// </summary>
    public async Task<Guid> CreateLiabilityPaymentVoucherAsync(
        Guid companyId,
        string vendorCode,
        string vendorName,
        decimal amount,
        DateTime paymentDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vendorCode))
            throw new ArgumentException("Vendor code is required.", nameof(vendorCode));
        if (amount <= 0m)
            throw new ArgumentException("Liability payment amount must be positive.", nameof(amount));

        var vendor = await _apContext.Vendors
            .FirstOrDefaultAsync(v => v.VendorId == vendorCode, cancellationToken);
        if (vendor is null)
        {
            vendor = new Vendor(vendorCode, vendorName, vendorName, null, null, null, true);
            _apContext.Vendors.Add(vendor);
            await _apContext.SaveChangesAsync(cancellationToken);
        }

        var period = await ResolveFiscalPeriodAsync(companyId, paymentDate, cancellationToken);
        if (period is null)
            throw new InvalidOperationException("No fiscal period found for the liability payment company.");

        var payrollLiabilityId = await ResolvePayrollLiabilityAccountAsync(companyId, cancellationToken);
        var apControlId = await ResolveApLiabilityAccountAsync(companyId, cancellationToken);

        var distributions = new List<VoucherDistributionDto>
        {
            new(payrollLiabilityId, amount, null, null, null),
            new(apControlId, null, amount, null, null),
        };

        var invoiceNumber = $"TAXPMT-{paymentDate:yyyyMMdd}-{vendorCode}";
        var batchNumber = $"PAY-LIAB-{Guid.NewGuid():N}";

        var batch = await _voucherService.CreateVoucherBatchAsync(
            companyId,
            batchNumber,
            $"Payroll liability payment {vendorCode}",
            new DateTimeOffset(paymentDate),
            period.Id,
            cancellationToken);

        var voucher = await _voucherService.AddVoucherToBatchAsync(
            batch.Id,
            vendor.Id,
            VoucherType.Invoice,
            invoiceNumber,
            new DateTimeOffset(paymentDate),
            new DateTimeOffset(paymentDate).AddDays(30),
            amount,
            0m,
            $"Payroll liability payment to {vendorName}",
            null,
            null,
            null,
            0m,
            0m,
            distributions,
            cancellationToken);

        await _voucherService.ReleaseBatchAsync(batch.Id, cancellationToken);
        await _voucherService.PostBatchAsync(batch.Id, cancellationToken);
        return voucher.Id;
    }

    private async Task<Guid> ResolvePayrollLiabilityAccountAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var setupAccount = await _payrollContext.CompanyPayrollSetups
            .Where(s => s.CompanyId == companyId)
            .Select(s => (Guid?)s.PayrollLiabilityAccountId)
            .FirstOrDefaultAsync(cancellationToken);
        if (setupAccount.HasValue && setupAccount.Value != Guid.Empty)
            return setupAccount.Value;

        foreach (var code in new[] { "2200", "2210", "2220" })
        {
            var accountId = await _platformContext.Accounts
                .Where(a => a.CompanyId == companyId && a.AccountNumber == code)
                .Select(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountId != Guid.Empty)
                return accountId;
        }

        var any = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.AccountNumber)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (any == Guid.Empty)
            throw new InvalidOperationException("No payroll liability account available for liability payment.");
        return any;
    }

    private async Task<FiscalPeriod?> ResolveFiscalPeriodAsync(Guid companyId, DateTime transactionDate, CancellationToken cancellationToken)
    {
        var date = new DateTimeOffset(transactionDate);
        return await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.StartDate <= date && p.EndDate >= date)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> ResolveExpenseAccountAsync(Guid companyId, CancellationToken cancellationToken)
    {
        // Prefer a dedicated employee-expense account; fall back to the standard wage/expense bucket.
        foreach (var code in new[] { "6100", "7290", "6000" })
        {
            var accountId = await _platformContext.Accounts
                .Where(a => a.CompanyId == companyId && a.AccountNumber == code)
                .Select(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountId != Guid.Empty)
                return accountId;
        }

        // Last resort: any account for the company.
        var any = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.AccountNumber)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (any == Guid.Empty)
            throw new InvalidOperationException("No GL account available for expense reimbursement.");
        return any;
    }

    private async Task<Guid> ResolveApLiabilityAccountAsync(Guid companyId, CancellationToken cancellationToken)
    {
        // The Accounts Payable control account holds the vendor payable credit.
        foreach (var code in new[] { "2000", "2010", "2100" })
        {
            var accountId = await _platformContext.Accounts
                .Where(a => a.CompanyId == companyId && a.AccountNumber == code)
                .Select(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountId != Guid.Empty)
                return accountId;
        }

        var any = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.AccountNumber)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (any == Guid.Empty)
            throw new InvalidOperationException("No AP liability account available for expense reimbursement.");
        return any;
    }
}
