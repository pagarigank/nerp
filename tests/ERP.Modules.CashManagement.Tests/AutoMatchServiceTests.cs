using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.CashManagement.Tests;

public class AutoMatchServiceTests
{
    private static readonly DateTimeOffset Date = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero);

    private readonly AutoMatchService _service = new AutoMatchService();

    [Fact]
    public void ExactAmountAndDateYieldsExactConfidence()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.ApPayment, "PMT-0001", 100m, Date, "1001", "AP Payment"));

        var result = _service.MatchLine(Guid.NewGuid(), 100m, Date, "PMT-0001", "1001", candidates);

        result.Candidate.Should().NotBeNull();
        result.Score.Should().Be(120);
        result.Confidence.Should().Be(MatchConfidence.Exact);
    }

    [Fact]
    public void ExactAmountOutsideDateWindowYieldsProbable()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.ApPayment, "PMT-0001", 100m, Date, "1001", "AP Payment"));

        var result = _service.MatchLine(Guid.NewGuid(), 100m, Date.AddDays(5), null, null, candidates);

        result.Score.Should().Be(AutoMatchService.ExactAmountScore);
        result.Confidence.Should().Be(MatchConfidence.Probable);
    }

    [Fact]
    public void PennyToleranceYieldsProbableConfidence()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.Deposit, "DEP-001", 100m, Date, null, "Deposit"));

        var result = _service.MatchLine(Guid.NewGuid(), 100.01m, Date, null, null, candidates);

        result.Score.Should().Be(AutoMatchService.PennyToleranceScore + 5);
        result.Confidence.Should().Be(MatchConfidence.Probable);
    }

    [Fact]
    public void DollarToleranceWithoutDateYieldsManualReview()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.Deposit, "DEP-002", 100m, Date, null, "Deposit"));

        var result = _service.MatchLine(Guid.NewGuid(), 100.50m, Date.AddDays(10), null, null, candidates);

        result.Score.Should().Be(AutoMatchService.DollarToleranceScore);
        result.Confidence.Should().Be(MatchConfidence.ManualReview);
    }

    [Fact]
    public void NoCandidateWhenAmountDiffersByMoreThanOneDollar()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.ApPayment, "PMT-0001", 100m, Date, "1001", "AP Payment"));

        var result = _service.MatchLine(Guid.NewGuid(), 102m, Date, null, null, candidates);

        result.Score.Should().Be(0);
        result.Confidence.Should().Be(MatchConfidence.None);
        result.Candidate.Should().BeNull();
    }

    [Fact]
    public void ReferenceNumberMatchAddsBonusScore()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.ArCashReceipt, "CR-0001", 250m, Date, null, "AR Receipt"));

        var result = _service.MatchLine(Guid.NewGuid(), 250m, Date.AddDays(1), "CR-0001", null, candidates);

        result.Score.Should().Be(AutoMatchService.ExactAmountScore + 5 + 15);
        result.Confidence.Should().Be(MatchConfidence.Exact);
    }

    [Fact]
    public void CheckNumberMatchAddsBonusScore()
    {
        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.ApPayment, "PMT-0003", 75m, Date, "4003", "AP Payment"));

        var result = _service.MatchLine(Guid.NewGuid(), 75m, Date, "PMT-0003", "4003", candidates);

        result.Score.Should().Be(AutoMatchService.ExactAmountScore + 5 + 15);
        result.Confidence.Should().Be(MatchConfidence.Exact);
    }

    [Fact]
    public void AmountScoreThresholdsAreCorrect()
    {
        AutoMatchService.AmountScore(100m, 100m).Should().Be(AutoMatchService.ExactAmountScore);
        AutoMatchService.AmountScore(100m, 100.01m).Should().Be(AutoMatchService.PennyToleranceScore);
        AutoMatchService.AmountScore(100m, 100.50m).Should().Be(AutoMatchService.DollarToleranceScore);
        AutoMatchService.AmountScore(100m, 101.50m).Should().Be(0);
    }

    [Fact]
    public void ConfidenceThresholdsAreCorrect()
    {
        AutoMatchService.GetConfidence(120).Should().Be(MatchConfidence.Exact);
        AutoMatchService.GetConfidence(105).Should().Be(MatchConfidence.Exact);
        AutoMatchService.GetConfidence(85).Should().Be(MatchConfidence.Probable);
        AutoMatchService.GetConfidence(60).Should().Be(MatchConfidence.ManualReview);
        AutoMatchService.GetConfidence(0).Should().Be(MatchConfidence.None);
    }

    [Fact]
    public void MatchAllRemovesMatchedCandidateFromPool()
    {
        var statement = new BankStatement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "STM-001",
            Date,
            0,
            200,
            "stmt.csv",
            BankStatementFormat.Csv);
        statement.AddLine(Date, 100m, "First", null, null, 100);
        statement.AddLine(Date, 100m, "Second", null, null, 200);

        var candidates = Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.Deposit, "DEP-001", 100m, Date, null, "Deposit"));

        var results = _service.MatchAll(statement.Lines, candidates);

        results.Should().HaveCount(2);
        results[0].Candidate.Should().NotBeNull();
        results[0].Confidence.Should().Be(MatchConfidence.Exact);
        results[1].Candidate.Should().BeNull();
        results[1].Confidence.Should().Be(MatchConfidence.None);
    }

    [Fact]
    public void MatchAllWithNoLinesOrNoCandidatesReturnsEmpty()
    {
        var statement = new BankStatement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "STM-002",
            Date,
            0,
            0,
            "stmt.csv",
            BankStatementFormat.Csv);

        _service.MatchAll(statement.Lines, Candidates()).Should().BeEmpty();
        _service.MatchAll(statement.Lines, Candidates(new AutoMatchCandidate(Guid.NewGuid(), BankMatchSource.Deposit, "DEP-001", 100m, Date, null, null))).Should().BeEmpty();
    }

    private static List<AutoMatchCandidate> Candidates(params AutoMatchCandidate[] candidates) => [.. candidates];
}
