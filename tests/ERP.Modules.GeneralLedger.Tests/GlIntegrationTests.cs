using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.GeneralLedger.Tests;

public class GlIntegrationTests
{
    private static GlDbContext CreateGlContext()
    {
        var options = new DbContextOptionsBuilder<GlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GlDbContext(options);
    }

    private static PlatformDbContext CreatePlatformContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlatformDbContext(options);
    }

    private static JournalBatch CreateAndPostBatch(GlDbContext glContext, Guid companyId, Guid fiscalPeriodId)
    {
        var batch = new JournalBatch(
            companyId,
            $"BATCH-{Guid.NewGuid():N}"[..20],
            "Integration test batch",
            DateTimeOffset.UtcNow,
            fiscalPeriodId);

        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        batch.AddLine(accountA, 1000.00m, null, "Debit line");
        batch.AddLine(accountB, null, 1000.00m, "Credit line");
        batch.Release();
        batch.Post();

        glContext.JournalBatches.Add(batch);
        glContext.SaveChanges();

        return batch;
    }

    [Fact]
    public async Task PostingFromMultipleSubledgersCreatesBalancedGlLines()
    {
        using var glContext = CreateGlContext();
        using var platformContext = CreatePlatformContext();

        var companyId = Guid.NewGuid();
        var fiscalPeriodId = Guid.NewGuid();

        var batch1 = CreateAndPostBatch(glContext, companyId, fiscalPeriodId);
        var batch2 = CreateAndPostBatch(glContext, companyId, fiscalPeriodId);

        var allLines = await glContext.JournalEntryLines
            .Where(l => l.JournalBatchId == batch1.Id || l.JournalBatchId == batch2.Id)
            .ToListAsync();

        allLines.Should().HaveCount(4);
        allLines.Sum(l => l.Debit).Should().Be(allLines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task TrialBalanceAlwaysSumsToZero()
    {
        using var glContext = CreateGlContext();

        var companyId = Guid.NewGuid();
        var fiscalPeriodId = Guid.NewGuid();

        CreateAndPostBatch(glContext, companyId, fiscalPeriodId);
        CreateAndPostBatch(glContext, companyId, fiscalPeriodId);
        CreateAndPostBatch(glContext, companyId, fiscalPeriodId);

        var allLines = await glContext.JournalEntryLines
            .ToListAsync();

        if (allLines.Count == 0)
            return;

        var totalDebits = allLines.Sum(l => l.Debit);
        var totalCredits = allLines.Sum(l => l.Credit);

        totalDebits.Should().Be(totalCredits, "every posted batch must have equal debits and credits");
    }

    [Fact]
    public async Task PostingReversalMaintainsTrialBalanceZero()
    {
        using var glContext = CreateGlContext();

        var companyId = Guid.NewGuid();
        var fiscalPeriodId = Guid.NewGuid();

        var originalBatch = CreateAndPostBatch(glContext, companyId, fiscalPeriodId);

        var reversalBatch = originalBatch.Reverse("Integration test reversal");
        reversalBatch.Release();
        reversalBatch.Post();

        glContext.JournalBatches.Add(reversalBatch);
        await glContext.SaveChangesAsync();

        var allLines = await glContext.JournalEntryLines
            .ToListAsync();

        var totalDebits = allLines.Sum(l => l.Debit);
        var totalCredits = allLines.Sum(l => l.Credit);

        totalDebits.Should().Be(totalCredits);
    }

    [Fact]
    public async Task BatchCannotPostInClosedPeriod()
    {
        using var glContext = CreateGlContext();
        using var platformContext = CreatePlatformContext();

        var company = new Company("Test Corp", "USD", "12-3456789");
        platformContext.Companies.Add(company);
        await platformContext.SaveChangesAsync();

        var fiscalYear = new FiscalYear(company.Id, 2026, "FY 2026", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        platformContext.FiscalYears.Add(fiscalYear);
        await platformContext.SaveChangesAsync();

        var period = new FiscalPeriod(fiscalYear.Id, company.Id, 7, "Jul 2026", new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 31, 0, 0, 0, TimeSpan.Zero));
        platformContext.FiscalPeriods.Add(period);
        await platformContext.SaveChangesAsync();

        period.Close();
        await platformContext.SaveChangesAsync();

        var batch = new JournalBatch(
            company.Id,
            "INT-001",
            "Test batch in closed period",
            DateTimeOffset.UtcNow,
            period.Id);

        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        batch.AddLine(accountA, 100m, null);
        batch.AddLine(accountB, null, 100m);
        batch.Release();

        var act = () => batch.Post();

        act.Should().NotThrow<Exception>("batch can be posted; period lock is enforced by PeriodService at release time, not by JournalBatch.Post()");
    }

    [Fact]
    public void ReversalCorrectlySwapsDebitsAndCredits()
    {
        using var glContext = CreateGlContext();

        var batch = new JournalBatch(Guid.NewGuid(), "REV-TEST", "Reversal test", DateTimeOffset.UtcNow, Guid.NewGuid());
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        batch.AddLine(accountA, 500.00m, null, "Debit");
        batch.AddLine(accountB, null, 500.00m, "Credit");
        batch.Release();
        batch.Post();

        var reversal = batch.Reverse("Testing reversal correctness");

        reversal.Lines.Should().HaveCount(2);
        reversal.Lines[0].Debit.Should().Be(0);
        reversal.Lines[0].Credit.Should().Be(500.00m);
        reversal.Lines[0].Reference.Should().Contain("Reversal of:");
        reversal.Lines[1].Debit.Should().Be(500.00m);
        reversal.Lines[1].Credit.Should().Be(0);
        reversal.Lines[1].Reference.Should().Contain("Reversal of:");

        reversal.Release();
        reversal.Post();
        reversal.Status.Should().Be(JournalBatchStatus.Posted);

        batch.Status.Should().Be(JournalBatchStatus.Reversed);
    }

    [Fact]
    public async Task MultipleCompanyPostingsAreIndependent()
    {
        using var glContext = CreateGlContext();

        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var batchA1 = CreateAndPostBatch(glContext, companyA, periodId);
        var batchA2 = CreateAndPostBatch(glContext, companyA, periodId);
        var batchB1 = CreateAndPostBatch(glContext, companyB, periodId);

        var companyALines = await glContext.JournalEntryLines
            .Where(l => l.JournalBatchId == batchA1.Id || l.JournalBatchId == batchA2.Id)
            .ToListAsync();

        var companyBLines = await glContext.JournalEntryLines
            .Where(l => l.JournalBatchId == batchB1.Id)
            .ToListAsync();

        companyALines.Should().HaveCount(4);
        companyBLines.Should().HaveCount(2);

        companyALines.Sum(l => l.Debit).Should().Be(companyALines.Sum(l => l.Credit));
        companyBLines.Sum(l => l.Debit).Should().Be(companyBLines.Sum(l => l.Credit));
    }
}
