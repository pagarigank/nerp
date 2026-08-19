// <copyright file="ArToGlPostingTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.AccountsReceivable;

/// <summary>
/// Proves AR invoices now post to the General Ledger through the canonical
/// posting contract (architecture.md §5.1). Until this handler existed, AR
/// invoices raised InvoiceBatchPostedEvent but nothing consumed it, so revenue
/// and the AR control account were never reflected in the GL.
/// </summary>
public class ArToGlPostingTests : IntegrationTestBase
{
    [Fact]
    public async Task PostInvoiceBatch_ShouldCreateBalancedPostedGlJournalBatch()
    {
        await CleanDatabaseAsync();

        var company = new Company($"ARGL-{Guid.NewGuid():N}", "AR->GL Posting Co", "USD", null, null, null);
        var fiscalYear = new FiscalYear(company.Id, 2026, "FY2026", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var period = new FiscalPeriod(fiscalYear.Id, company.Id, 1, "2026-01", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        var batchNumber = $"INV-{Guid.NewGuid():N}";

        // Seed (and commit) company, fiscal period and chart-of-accounts first so
        // the period-open guard can read them. The post (and the AR->GL handler it
        // triggers) runs outside an ambient transaction below, because the handler
        // saves on a separate DbContext/connection (GL) that must not be entangled
        // with the test's open transaction.
        Guid arControlAccountId = Guid.Empty;
        Guid revenueAccountId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);
            await platform.FiscalYears.AddAsync(fiscalYear);
            await platform.FiscalPeriods.AddAsync(period);

            // The AR handler debits the AR control account (number 1200) and
            // credits the revenue account. Insert them directly via raw SQL.
            arControlAccountId = Guid.NewGuid();
            revenueAccountId = Guid.NewGuid();
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '1200', 'Accounts Receivable', 0, 0, 1, SYSUTCDATETIME(), 'system');",
                arControlAccountId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '4000', 'Sales Revenue', 3, 1, 1, SYSUTCDATETIME(), 'system');",
                revenueAccountId, company.Id);

            // The GL JournalLine.AccountId FK targets gl.Account (GL's own Account
            // table), so mirror the same account rows there as well.
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '1200', 'Accounts Receivable', 0, 0, 1, SYSUTCDATETIME(), 'system');",
                arControlAccountId, company.Id);
            await platform.Database.ExecuteSqlRawAsync(
                "INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedOn, CreatedBy) " +
                "VALUES ({0}, {1}, '4000', 'Sales Revenue', 3, 1, 1, SYSUTCDATETIME(), 'system');",
                revenueAccountId, company.Id);
        });

        // Post outside an ambient transaction (see note above).
        using (var postScope = ServiceProvider.CreateScope())
        {
            var ar = postScope.ServiceProvider.GetRequiredService<ArDbContext>();
            var batch = new InvoiceBatch(company.Id, batchNumber, "Product sale", new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), period.Id);
            var invoice = batch.AddInvoice(Guid.NewGuid(), "INV-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), "Widget sale", null, null, null);
            invoice.AddLine(revenueAccountId, "Widget", 1m, 500m, 0m, 0m);

            batch.Release();
            batch.Post();

            ar.InvoiceBatches.Add(batch);
            await ar.SaveChangesAsync();
        }

        // Assert (separate read): a balanced, posted GL JournalBatch exists.
        var glBatchNumber = $"INV-{batchNumber}";
        var glBatch = await ExecuteInTransactionAsync(async sp =>
        {
            var gl = sp.GetRequiredService<GlDbContext>();
            return await gl.JournalBatches
                .Where(b => b.BatchNumber == glBatchNumber)
                .Include(b => b.Lines)
                .FirstOrDefaultAsync();
        });

        glBatch.Should().NotBeNull("posting an AR invoice must create a GL journal batch");
        glBatch!.Status.Should().Be(JournalBatchStatus.Posted, "the GL batch must be posted");
        glBatch.Lines.Should().HaveCount(2, "Dr AR control + Cr revenue");
        glBatch.IsBalanced().Should().BeTrue("the GL batch must balance (debits = credits)");
        glBatch.Lines.Sum(l => l.Debit).Should().Be(500m);
        glBatch.Lines.Sum(l => l.Credit).Should().Be(500m);
    }
}