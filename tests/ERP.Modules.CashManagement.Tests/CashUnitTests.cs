using ERP.Modules.CashManagement.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.CashManagement.Tests;

public class CashUnitTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BankAccountId = Guid.NewGuid();

    [Fact]
    public void BankAccountLifecycleTransitionsCorrectly()
    {
        var account = CreateBankAccount();

        account.Status.Should().Be(BankAccountStatus.Active);
        account.CurrentBalance.Should().Be(5000m);

        account.Deactivate();
        account.Status.Should().Be(BankAccountStatus.Inactive);

        account.Activate();
        account.Status.Should().Be(BankAccountStatus.Active);

        account.Close();
        account.Status.Should().Be(BankAccountStatus.Closed);
    }

    [Fact]
    public void BankAccountAdjustBalanceUpdatesCurrentBalance()
    {
        var account = CreateBankAccount();

        account.AdjustBalance(-100m);
        account.CurrentBalance.Should().Be(4900m);

        account.AdjustBalance(50m);
        account.CurrentBalance.Should().Be(4950m);
    }

    [Fact]
    public void BankAccountUpdateBlockedWhenClosed()
    {
        var account = CreateBankAccount();
        account.Close();

        var act = () => account.Update("New Name", "00123", "987654321", "Bank", "USD", BankAccountType.Checking, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("*closed*");
    }

    [Fact]
    public void BankAccountAddContactBlockedWhenClosed()
    {
        var account = CreateBankAccount();
        account.Close();

        var act = () => account.AddContact("Contact", "555-0100", null, "Treasurer");
        act.Should().Throw<InvalidOperationException>().WithMessage("*closed*");
    }

    [Fact]
    public void BankAccountAddContactAppendsContact()
    {
        var account = CreateBankAccount();

        account.AddContact("Contact", "555-0100", null, "Treasurer");

        account.Contacts.Should().HaveCount(1);
        account.Contacts[0].Name.Should().Be("Contact");
        account.Contacts[0].BankAccountId.Should().Be(account.Id);
    }

    [Fact]
    public void DepositLifecycleTransitionsCorrectly()
    {
        var deposit = new Deposit(CompanyId, BankAccountId, "DEP-001", DateTimeOffset.UtcNow, "Test deposit");

        deposit.Status.Should().Be(DepositStatus.Draft);
        deposit.TotalAmount.Should().Be(0);

        deposit.AddLine(DepositLineSource.Manual, null, 100m, "Manual deposit");
        deposit.AddLine(DepositLineSource.ArCashReceipt, Guid.NewGuid(), 250m, "Cash receipt");

        deposit.TotalAmount.Should().Be(350m);

        deposit.Confirm();
        deposit.Status.Should().Be(DepositStatus.Confirmed);

        deposit.Clear();
        deposit.Status.Should().Be(DepositStatus.Cleared);
    }

    [Fact]
    public void DepositAddLineBlockedWhenNotDraft()
    {
        var deposit = new Deposit(CompanyId, BankAccountId, "DEP-002", DateTimeOffset.UtcNow, null);
        deposit.AddLine(DepositLineSource.Manual, null, 100m, null);
        deposit.Confirm();

        var act = () => deposit.AddLine(DepositLineSource.Manual, null, 100m, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Draft*");
    }

    [Fact]
    public void DepositConfirmBlockedWithoutLines()
    {
        var deposit = new Deposit(CompanyId, BankAccountId, "DEP-003", DateTimeOffset.UtcNow, null);

        var act = () => deposit.Confirm();
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one line*");
    }

    [Fact]
    public void DepositClearBlockedWhenNotConfirmed()
    {
        var deposit = new Deposit(CompanyId, BankAccountId, "DEP-004", DateTimeOffset.UtcNow, null);
        deposit.AddLine(DepositLineSource.Manual, null, 50m, null);

        var act = () => deposit.Clear();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Confirmed*");
    }

    [Fact]
    public void BankStatementLifecycleTransitionsCorrectly()
    {
        var statement = CreateStatement();

        statement.Status.Should().Be(BankStatementStatus.Imported);

        statement.MarkValidated();
        statement.Status.Should().Be(BankStatementStatus.Validated);

        statement.MarkReconciled();
        statement.Status.Should().Be(BankStatementStatus.Reconciled);

        statement.MarkLocked();
        statement.Status.Should().Be(BankStatementStatus.Locked);
    }

    [Fact]
    public void BankStatementValidateBlockedWhenNotImported()
    {
        var statement = CreateStatement();
        statement.MarkValidated();

        var act = () => statement.MarkValidated();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Imported*");
    }

    [Fact]
    public void BankStatementLockBlockedWhenNotReconciled()
    {
        var statement = CreateStatement();
        statement.MarkValidated();

        var act = () => statement.MarkLocked();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Reconciled*");
    }

    [Fact]
    public void BankStatementLineMatchClearAndUnmatchWorks()
    {
        var statement = CreateStatement();
        statement.AddLine(DateTimeOffset.UtcNow, -100m, "Vendor payment", "PMT-0001", "1001", 1000m);

        var line = statement.Lines[0];
        line.Status.Should().Be(BankStatementLineStatus.Unreconciled);

        line.MarkMatched(Guid.NewGuid(), BankMatchSource.ApPayment);
        line.Status.Should().Be(BankStatementLineStatus.Matched);
        line.MatchedSource.Should().Be(BankMatchSource.ApPayment);
        line.MatchedTransactionId.Should().NotBeNull();

        line.MarkCleared();
        line.Status.Should().Be(BankStatementLineStatus.Cleared);

        line.MarkUnmatched();
        line.Status.Should().Be(BankStatementLineStatus.Unreconciled);
        line.MatchedSource.Should().BeNull();
        line.MatchedTransactionId.Should().BeNull();
    }

    [Fact]
    public void BankStatementLineLockedBlocksFurtherChanges()
    {
        var statement = CreateStatement();
        statement.AddLine(DateTimeOffset.UtcNow, 100m, "Deposit", null, null, 100m);

        var line = statement.Lines[0];
        line.Lock();
        line.Status.Should().Be(BankStatementLineStatus.Locked);

        var match = () => line.MarkMatched(Guid.NewGuid(), BankMatchSource.Deposit);
        match.Should().Throw<InvalidOperationException>().WithMessage("*locked*");
    }

    [Fact]
    public void ReconciliationSessionLockRaisesBankReconciledEvent()
    {
        var session = CreateSession();

        session.RecordVariance(12.5m);
        var journalBatchId = Guid.NewGuid();
        session.AttachGlJournal(journalBatchId);
        session.Lock("tester");

        session.Status.Should().Be(ReconciliationStatus.Locked);
        session.LockedBy.Should().Be("tester");
        session.Variance.Should().Be(12.5m);
        session.GlJournalBatchId.Should().Be(journalBatchId);

        var events = session.DomainEvents.OfType<BankReconciledEvent>().ToList();
        events.Should().HaveCount(1);
        events[0].SessionId.Should().Be(session.Id);
        events[0].Variance.Should().Be(12.5m);
        events[0].GlJournalBatchId.Should().Be(journalBatchId);
        events[0].EventType.Should().Be("BankReconciled");
    }

    [Fact]
    public void ReconciliationSessionLockBlockedWhenAlreadyLocked()
    {
        var session = CreateSession();
        session.Lock("tester");

        var act = () => session.Lock("tester2");
        act.Should().Throw<InvalidOperationException>().WithMessage("*in-progress*");
    }

    private static BankAccount CreateBankAccount()
    {
        return new BankAccount(
            CompanyId,
            "1000",
            "Operating Checking",
            "0012345678",
            "987654321",
            "Test Bank",
            "USD",
            BankAccountType.Checking,
            5000m,
            null);
    }

    private static BankStatement CreateStatement()
    {
        return new BankStatement(
            CompanyId,
            BankAccountId,
            "STM-001",
            DateTimeOffset.UtcNow,
            1000m,
            1250m,
            "statement.csv",
            BankStatementFormat.Csv);
    }

    private static ReconciliationSession CreateSession()
    {
        var statement = CreateStatement();
        return new ReconciliationSession(
            CompanyId,
            BankAccountId,
            statement.Id,
            "RCON-001",
            statement.StatementDate,
            statement.BeginningBalance,
            statement.EndingBalance);
    }
}
