using ERP.Modules.GeneralLedger.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.GeneralLedger.Tests;

public class JournalBatchTests
{
    private static JournalBatch CreateBatch()
    {
        return new JournalBatch(
            Guid.NewGuid(),
            "BATCH-001",
            "Test batch",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
    }

    [Fact]
    public void AddLineWithDebitAndCreditThrowsException()
    {
        var batch = CreateBatch();
        var accountId = Guid.NewGuid();

        var act = () => batch.AddLine(accountId, 100m, 100m);

        act.Should().Throw<ArgumentException>().WithMessage("Each line must have either a debit OR a credit amount, not both.");
    }

    [Fact]
    public void AddLineWithNeitherDebitNorCreditThrowsException()
    {
        var batch = CreateBatch();
        var accountId = Guid.NewGuid();

        var act = () => batch.AddLine(accountId, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("Each line must have either a debit OR a credit amount, not both.");
    }

    [Fact]
    public void AddLineWithNegativeDebitThrowsException()
    {
        var batch = CreateBatch();

        var act = () => batch.AddLine(Guid.NewGuid(), -10m, null);

        act.Should().Throw<ArgumentException>().WithMessage("Debit amount must be positive.*");
    }

    [Fact]
    public void AddLineWithNegativeCreditThrowsException()
    {
        var batch = CreateBatch();

        var act = () => batch.AddLine(Guid.NewGuid(), null, -10m);

        act.Should().Throw<ArgumentException>().WithMessage("Credit amount must be positive.*");
    }

    [Fact]
    public void SingleLineBatchIsNotBalanced()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);

        batch.IsBalanced().Should().BeFalse();
    }

    [Fact]
    public void BalancedBatchReturnsTrue()
    {
        var batch = CreateBatch();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        batch.AddLine(accountA, 100m, null);
        batch.AddLine(accountB, null, 100m);

        batch.IsBalanced().Should().BeTrue();
    }

    [Fact]
    public void UnbalancedBatchReturnsFalse()
    {
        var batch = CreateBatch();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        batch.AddLine(accountA, 100m, null);
        batch.AddLine(accountB, null, 50m);

        batch.IsBalanced().Should().BeFalse();
    }

    [Fact]
    public void ReleaseSetsStatusToBalanced()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);

        batch.Release();

        batch.Status.Should().Be(JournalBatchStatus.Balanced);
    }

    [Fact]
    public void ReleaseOnUnbalancedBatchThrowsException()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 50m);

        var act = () => batch.Release();

        act.Should().Throw<InvalidOperationException>().WithMessage("Batch must be balanced*");
    }

    [Fact]
    public void ReleaseOnEmptyBatchThrowsException()
    {
        var batch = CreateBatch();

        var act = () => batch.Release();

        act.Should().Throw<InvalidOperationException>().WithMessage("Batch must have at least two lines.");
    }

    [Fact]
    public void PostSetsStatusToPosted()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);
        batch.Release();

        batch.Post();

        batch.Status.Should().Be(JournalBatchStatus.Posted);
    }

    [Fact]
    public void PostOnDraftBatchThrowsException()
    {
        var batch = CreateBatch();

        var act = () => batch.Post();

        act.Should().Throw<InvalidOperationException>().WithMessage("Only a Balanced batch can be posted.");
    }

    [Fact]
    public void AddLineToPostedBatchThrowsException()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);
        batch.Release();
        batch.Post();

        var act = () => batch.AddLine(Guid.NewGuid(), 50m, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot modify a batch that is not in Draft status.");
    }

    [Fact]
    public void ReverseCorrectlySwapsDebitsAndCredits()
    {
        var batch = CreateBatch();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        batch.AddLine(accountA, 100m, null);
        batch.AddLine(accountB, null, 100m);
        batch.Release();
        batch.Post();

        var reversal = batch.Reverse("Test reversal");

        reversal.Status.Should().Be(JournalBatchStatus.Draft);
        reversal.Lines[0].Debit.Should().Be(0);
        reversal.Lines[0].Credit.Should().Be(100m);
        reversal.Lines[1].Debit.Should().Be(100m);
        reversal.Lines[1].Credit.Should().Be(0);
    }

    [Fact]
    public void ReverseUpdatesOriginalStatusToReversed()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);
        batch.Release();
        batch.Post();

        batch.Reverse("Test reversal");

        batch.Status.Should().Be(JournalBatchStatus.Reversed);
    }

    [Fact]
    public void ReverseOnUnpostedBatchThrowsException()
    {
        var batch = CreateBatch();

        var act = () => batch.Reverse("Test");

        act.Should().Throw<InvalidOperationException>().WithMessage("Only a Posted batch can be reversed.");
    }

    [Fact]
    public void ReverseWithEmptyReasonThrowsException()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);
        batch.Release();
        batch.Post();

        var act = () => batch.Reverse(string.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("A reversal reason is required.*");
    }

    [Fact]
    public void PostEmitsDomainEvent()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);
        batch.Release();

        batch.Post();

        var events = batch.DomainEvents;
        events.Should().Contain(e => e is JournalBatchPostedEvent);
    }

    [Fact]
    public void UpdateDescriptionChangesDescription()
    {
        var batch = CreateBatch();
        batch.UpdateDescription("Updated description");

        batch.Description.Should().Be("Updated description");
    }

    [Fact]
    public void UpdateDescriptionOnReleasedBatchThrowsException()
    {
        var batch = CreateBatch();
        batch.AddLine(Guid.NewGuid(), 100m, null);
        batch.AddLine(Guid.NewGuid(), null, 100m);
        batch.Release();

        var act = () => batch.UpdateDescription("Changed");

        act.Should().Throw<InvalidOperationException>();
    }
}
