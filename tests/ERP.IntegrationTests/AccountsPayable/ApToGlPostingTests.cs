// <copyright file="ApToGlPostingTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.AccountsPayable;

/// <summary>
/// Proves Accounts Payable vouchers post to the General Ledger through the
/// canonical posting contract (architecture.md §5.1). The AP poster raises
/// VoucherBatchPostedEvent; the GL consumer turns it into a balanced, posted
/// JournalBatch. This is the authoritative P0 integration test.
/// </summary>
public class ApToGlPostingTests : IntegrationTestBase
{
    [Fact]
    public async Task PostVoucherBatch_ShouldCreateBalancedPostedGlJournalBatch()
    {
        await CleanDatabaseAsync();

        var company = new Company($"APGL-{Guid.NewGuid():N}", "AP->GL Posting Co", "USD", null, null, null);
        var fiscalYear = new FiscalYear(company.Id, 2026, "FY2026", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var period = new FiscalPeriod(fiscalYear.Id, company.Id, 1, "2026-01", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        var vendor = new Vendor($"V-{Guid.NewGuid():N}", "Test Vendor", "Test Vendor Legal", "TAX-123", null, null, true);
        var batchNumber = $"VCH-{Guid.NewGuid():N}";

        // Seed (and commit) company, fiscal period, chart-of-accounts and vendor.
        // We commit the seed first so the period-open guard can read it; the post
        // itself is run outside an ambient transaction (below) because it triggers
        // the GL consumer, which saves on a separate DbContext/connection and must
        // not be entangled with the test's open transaction.
        Guid expenseAccountId = Guid.Empty;
        Guid cashAccountId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);
            await platform.FiscalYears.AddAsync(fiscalYear);
            await platform.FiscalPeriods.AddAsync(period);

            expenseAccountId = Guid.NewGuid();
            cashAccountId = Guid.NewGuid();
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '6000', 'Office Expense', 4, 0, 1, SYSUTCDATETIME(), 'system');",
                expenseAccountId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '1000', 'Cash', 0, 0, 1, SYSUTCDATETIME(), 'system');",
                cashAccountId, company.Id);

            // The GL JournalLine.AccountId FK targets gl.Account, so mirror the rows.
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '6000', 'Office Expense', 4, 0, 1, SYSUTCDATETIME(), 'system');",
                expenseAccountId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '1000', 'Cash', 0, 0, 1, SYSUTCDATETIME(), 'system');",
                cashAccountId, company.Id);

            var ap = sp.GetRequiredService<ApDbContext>();
            await ap.Vendors.AddAsync(vendor);
            await platform.SaveChangesAsync();
        });

        // Post outside an ambient transaction so the GL consumer's separate
        // connection is not entangled with the test's open transaction.
        using (var postScope = ServiceProvider.CreateScope())
        {
            var voucherService = postScope.ServiceProvider.GetRequiredService<IVoucherService>();
            var batch = await voucherService.CreateVoucherBatchAsync(
                company.Id, batchNumber, "Office supplies invoice", new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), period.Id);

            await voucherService.AddVoucherToBatchAsync(
                batch.Id,
                vendor.Id,
                VoucherType.Invoice,
                "INV-100",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(30),
                1000m,
                0m,
                "Office supplies",
                null,
                null,
                null,
                0m,
                0m,
                new[]
                {
                    new VoucherDistributionDto(expenseAccountId, 1000m, null, null, null),
                    new VoucherDistributionDto(cashAccountId, null, 1000m, null, null)
                });

            await voucherService.ReleaseBatchAsync(batch.Id);
            await voucherService.PostBatchAsync(batch.Id);
        }

        // Assert (separate read): a balanced, posted GL JournalBatch exists.
        var glBatchNumber = $"VCH-{batchNumber}";
        var glBatch = await ExecuteInTransactionAsync(async sp =>
        {
            var gl = sp.GetRequiredService<GlDbContext>();
            return await gl.JournalBatches
                .Where(b => b.BatchNumber == glBatchNumber)
                .Include(b => b.Lines)
                .FirstOrDefaultAsync();
        });

        glBatch.Should().NotBeNull("posting an AP voucher must create a GL journal batch");
        glBatch!.Status.Should().Be(JournalBatchStatus.Posted, "the GL batch must be posted, not left in draft");
        glBatch.Lines.Should().HaveCount(2, "the two voucher distributions become two GL lines");
        glBatch.IsBalanced().Should().BeTrue("the GL batch must balance (debits = credits)");
        glBatch.Lines.Sum(l => l.Debit).Should().Be(1000m);
        glBatch.Lines.Sum(l => l.Credit).Should().Be(1000m);
    }
}