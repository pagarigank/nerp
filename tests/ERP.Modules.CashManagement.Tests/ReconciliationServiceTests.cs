using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.CashManagement.Tests;

public class ReconciliationServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BankAccountId = Guid.NewGuid();

    [Fact]
    public async Task CreateSessionCreatesInProgressSession()
    {
        using var cashContext = CreateCashContext();
        using var apContext = new ApDbContext(CreateOptions<ApDbContext>());
        using var arContext = new ArDbContext(CreateOptions<ArDbContext>());
        using var platformContext = new PlatformDbContext(CreateOptions<PlatformDbContext>());
        var service = CreateService(cashContext, apContext, arContext, platformContext);

        var statement = AddStatement(cashContext);

        var session = await service.CreateSessionAsync(statement.Id, "RCON-001", "tester");

        session.Should().NotBeNull();
        session.SessionNumber.Should().Be("RCON-001");
        session.BankStatementId.Should().Be(statement.Id);
        session.BeginningBalance.Should().Be(1000m);
        session.EndingBalance.Should().Be(1250m);
        session.Status.Should().Be(ReconciliationStatus.InProgress);
    }

    [Fact]
    public async Task CreateSessionRejectsLockedStatement()
    {
        using var cashContext = CreateCashContext();
        using var apContext = new ApDbContext(CreateOptions<ApDbContext>());
        using var arContext = new ArDbContext(CreateOptions<ArDbContext>());
        using var platformContext = new PlatformDbContext(CreateOptions<PlatformDbContext>());
        var service = CreateService(cashContext, apContext, arContext, platformContext);

        var statement = AddStatement(cashContext);
        statement.MarkReconciled();
        statement.MarkLocked();
        await cashContext.SaveChangesAsync();

        var act = async () => await service.CreateSessionAsync(statement.Id, "RCON-002", "tester");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*locked*");
    }

    [Fact]
    public async Task RunAutoMatchReturnsEmptyWhenNoCandidatesExist()
    {
        using var cashContext = CreateCashContext();
        using var apContext = new ApDbContext(CreateOptions<ApDbContext>());
        using var arContext = new ArDbContext(CreateOptions<ArDbContext>());
        using var platformContext = new PlatformDbContext(CreateOptions<PlatformDbContext>());
        var service = CreateService(cashContext, apContext, arContext, platformContext);

        var statement = AddStatement(cashContext);

        var session = await service.CreateSessionAsync(statement.Id, "RCON-003", "tester");

        var results = await service.RunAutoMatchAsync(session.Id);

        results.Should().BeEmpty();
    }

    private static ReconciliationService CreateService(
        CashDbContext cashContext,
        ApDbContext apContext,
        ArDbContext arContext,
        PlatformDbContext platformContext)
    {
        return new ReconciliationService(
            cashContext,
            apContext,
            arContext,
            platformContext,
            new AutoMatchService(),
            new NoOpPostingPublisher());
    }

    private static DbContextOptions<T> CreateOptions<T>()
        where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static CashDbContext CreateCashContext()
    {
        var options = new DbContextOptionsBuilder<CashDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CashDbContext(options);
    }

    private static BankStatement AddStatement(CashDbContext context)
    {
        var statement = new BankStatement(
            CompanyId,
            BankAccountId,
            "STM-001",
            new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero),
            1000m,
            1250m,
            "statement.csv",
            BankStatementFormat.Csv);
        statement.AddLine(new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero), -100m, "Payment", "PMT-0001", "1001", 1000m);
        statement.AddLine(new DateTimeOffset(2025, 1, 20, 0, 0, 0, TimeSpan.Zero), 250m, "Deposit", "CR-0001", null, 1250m);

        context.BankStatements.Add(statement);
        context.SaveChanges();
        return statement;
    }

    private sealed class NoOpPostingPublisher : IPostingEventPublisher
    {
        public Task<Guid> PublishAsync(CanonicalPostingEvent postingEvent, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid());
    }
}
