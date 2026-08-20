// <copyright file="ApVoucherCreatorTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.Modules.Payroll.Tests;

/// <summary>
/// Unit tests for the expense-reimbursement -> AP voucher wiring (Phase 11 item #1101,
/// file: ApVoucherCreator.cs). These assert the two things the implementation must guarantee:
///  1. Distributions balance (each expense line debits the expense account; a single credit
///     to the AP liability account equals the report total) so the voucher passes AP validation.
///  2. The batch is Released (to Batched) before it is Posted.
/// Both are regression guards for the H3 change.
/// </summary>
public class ApVoucherCreatorTests
{
    /// <summary>Reimbursement builds balanced distributions (debits == credits) and releases before posting.</summary>
    [Fact]
    public async Task CreateReimbursementVoucherAsync_BuildsBalancedDistributions()
    {
        using var platform = BuildPlatformContext();
        using var ap = BuildApContext();
        var project = Guid.NewGuid();
        var task = Guid.NewGuid();
        var report = BuildReport((120m, project, task), (80m, project, task));

        var captured = new List<VoucherDistributionDto>();
        var batchOrder = new List<string>();

        var voucherService = new Mock<IVoucherService>();
        voucherService.Setup(s => s.CreateVoucherBatchAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoucherBatch(CompanyId, "EXP-REIMB", "desc", DateTimeOffset.UtcNow, FiscalPeriodId))
            .Callback(() => batchOrder.Add("Create"));
        voucherService.Setup(s => s.AddVoucherToBatchAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<VoucherType>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<IReadOnlyList<VoucherDistributionDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeVoucher())
            .Callback((Guid _, Guid _, VoucherType _, string _, DateTimeOffset _, DateTimeOffset _, decimal _, decimal _, string _, Guid? _, Guid? _, Guid? _, decimal _, decimal _, IReadOnlyList<VoucherDistributionDto> dists, CancellationToken _) => captured.AddRange(dists));
        voucherService.Setup(s => s.ReleaseBatchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoucherBatch(CompanyId, "EXP-REIMB", "desc", DateTimeOffset.UtcNow, FiscalPeriodId))
            .Callback(() => batchOrder.Add("Release"));
        voucherService.Setup(s => s.PostBatchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoucherBatch(CompanyId, "EXP-REIMB", "desc", DateTimeOffset.UtcNow, FiscalPeriodId))
            .Callback(() => batchOrder.Add("Post"));

        var creator = new ApVoucherCreator(ap, voucherService.Object, platform);

        var result = await creator.CreateReimbursementVoucherAsync(report, BuildEmployee());

        result.Should().NotBe(Guid.Empty);
        var debits = captured.Where(d => d.Debit.HasValue).ToList();
        var credits = captured.Where(d => d.Credit.HasValue).ToList();
        debits.Should().HaveCount(2, "each expense line debits the expense account");
        credits.Should().HaveCount(1, "a single balancing credit to AP liability");
        var expectedApLiability = (await platform.Accounts.SingleAsync(a => a.AccountNumber == "2000", CancellationToken.None)).Id;
        credits[0].AccountId.Should().Be(expectedApLiability, "credit posts to the AP liability account");
        credits[0].Credit.Should().Be(200m, "credit equals the report total");
        debits.Sum(d => d.Debit!.Value).Should().Be(credits.Sum(c => c.Credit!.Value), "distributions must balance");
        batchOrder.Should().Equal("Create", "Release", "Post");
    }

    /// <summary>When the employee has no AP vendor yet, one is created as the reimbursement payee.</summary>
    [Fact]
    public async Task CreateReimbursementVoucherAsync_CreatesEmployeeVendorWhenMissing()
    {
        using var platform = BuildPlatformContext();
        using var ap = BuildApContext();
        var report = BuildReport((50m, null, null));

        var voucherService = new Mock<IVoucherService>();
        voucherService.Setup(s => s.CreateVoucherBatchAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoucherBatch(CompanyId, "EXP-REIMB", "desc", DateTimeOffset.UtcNow, FiscalPeriodId));
        voucherService.Setup(s => s.AddVoucherToBatchAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<VoucherType>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<IReadOnlyList<VoucherDistributionDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeVoucher());
        voucherService.Setup(s => s.ReleaseBatchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoucherBatch(CompanyId, "EXP-REIMB", "desc", DateTimeOffset.UtcNow, FiscalPeriodId));
        voucherService.Setup(s => s.PostBatchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoucherBatch(CompanyId, "EXP-REIMB", "desc", DateTimeOffset.UtcNow, FiscalPeriodId));

        var creator = new ApVoucherCreator(ap, voucherService.Object, platform);

        await creator.CreateReimbursementVoucherAsync(report, BuildEmployee());

        ap.Vendors.Should().ContainSingle(v => v.VendorId == "EMP-E001", "employee vendor is created as the payee");
    }

    private static readonly Guid CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FiscalPeriodId = Guid.NewGuid();

    private static PlatformDbContext BuildPlatformContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new PlatformDbContext(options);
        ctx.Accounts.Add(new Account(CompanyId, "6100", "Employee Expenses", AccountType.Expense, NormalBalance.Debit, true));
        ctx.Accounts.Add(new Account(CompanyId, "2000", "Accounts Payable", AccountType.Liability, NormalBalance.Credit, true));
        ctx.FiscalPeriods.Add(new FiscalPeriod(
            Guid.NewGuid(), CompanyId, 8, "Aug-2026",
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero)));
        ctx.SaveChanges();
        return ctx;
    }

    private static ApDbContext BuildApContext()
    {
        var options = new DbContextOptionsBuilder<ApDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApDbContext(options);
    }

    private static Employee BuildEmployee()
        => new Employee(CompanyId, "E001", "Jane", "Doe", EmploymentType.Salary, DateTime.UtcNow.AddYears(-1));

    private static ExpenseReport BuildReport(params (decimal amount, Guid? project, Guid? task)[] lines)
    {
        var report = new ExpenseReport(CompanyId, Guid.NewGuid(), new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc));
        foreach (var (amount, project, task) in lines)
            report.AddLine(ExpenseType.Other, amount, new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc), "visit", project, task);
        report.Submit();
        return report;
    }

    private static Voucher MakeVoucher()
        => new Voucher(
            Guid.NewGuid(),
            Guid.NewGuid(),
            VoucherType.Invoice,
            "REIMB",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            200m,
            0m,
            "Expense reimbursement",
            null);
}
