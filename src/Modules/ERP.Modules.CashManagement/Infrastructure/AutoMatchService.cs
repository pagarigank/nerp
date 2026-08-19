// <copyright file="AutoMatchService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;

namespace ERP.Modules.CashManagement.Infrastructure;

public enum MatchConfidence
{
    None = 0,
    ManualReview = 1,
    Probable = 2,
    Exact = 3,
}

public record AutoMatchCandidate(
    Guid Id,
    BankMatchSource Source,
    string Reference,
    decimal Amount,
    DateTimeOffset Date,
    string? CheckNumber,
    string? Description);

public record AutoMatchResult(
    Guid StatementLineId,
    decimal StatementAmount,
    AutoMatchCandidate? Candidate,
    int Score,
    MatchConfidence Confidence);

public interface IAutoMatchService
{
    AutoMatchResult MatchLine(
        Guid statementLineId,
        decimal statementAmount,
        DateTimeOffset statementDate,
        string? referenceNumber,
        string? checkNumber,
        IReadOnlyList<AutoMatchCandidate> candidates);

    IReadOnlyList<AutoMatchResult> MatchAll(
        IReadOnlyList<BankStatementLine> statementLines,
        IReadOnlyList<AutoMatchCandidate> candidates);
}

public class AutoMatchService : IAutoMatchService
{
    public const int ExactAmountScore = 100;
    public const int PennyToleranceScore = 95;
    public const int DollarToleranceScore = 80;
    private const int DateWindowScore = 5;
    private const int ReferenceScore = 15;

    public AutoMatchResult MatchLine(
        Guid statementLineId,
        decimal statementAmount,
        DateTimeOffset statementDate,
        string? referenceNumber,
        string? checkNumber,
        IReadOnlyList<AutoMatchCandidate> candidates)
    {
        AutoMatchCandidate? bestCandidate = null;
        var bestScore = 0;

        foreach (var candidate in candidates)
        {
            var score = Score(candidate, statementAmount, statementDate, referenceNumber, checkNumber);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        return new AutoMatchResult(
            statementLineId,
            statementAmount,
            bestCandidate,
            bestScore,
            GetConfidence(bestScore));
    }

    public IReadOnlyList<AutoMatchResult> MatchAll(
        IReadOnlyList<BankStatementLine> statementLines,
        IReadOnlyList<AutoMatchCandidate> candidates)
    {
        if (statementLines.Count == 0 || candidates.Count == 0)
            return [];

        var pool = new List<AutoMatchCandidate>(candidates);
        var results = new List<AutoMatchResult>();

        foreach (var line in statementLines)
        {
            var result = MatchLine(
                line.Id,
                line.Amount,
                line.TransactionDate,
                line.ReferenceNumber,
                line.CheckNumber,
                pool);

            if (result.Candidate != null && result.Confidence >= MatchConfidence.Probable)
            {
                pool.Remove(result.Candidate);
            }

            results.Add(result);
        }

        return results;
    }

    internal static int Score(
        AutoMatchCandidate candidate,
        decimal statementAmount,
        DateTimeOffset statementDate,
        string? referenceNumber,
        string? checkNumber)
    {
        var score = AmountScore(candidate.Amount, statementAmount);
        if (score == 0)
            return 0;

        var dayDiff = Math.Abs((candidate.Date.Date - statementDate.Date).TotalDays);
        if (dayDiff <= 3)
            score += DateWindowScore;

        if (ReferenceMatches(candidate, referenceNumber, checkNumber))
            score += ReferenceScore;

        return score;
    }

    internal static int AmountScore(decimal candidateAmount, decimal statementAmount)
    {
        var difference = Math.Abs(candidateAmount - statementAmount);
        if (difference == 0)
            return ExactAmountScore;
        if (difference <= 0.01m)
            return PennyToleranceScore;
        if (difference <= 1.00m)
            return DollarToleranceScore;
        return 0;
    }

    internal static bool ReferenceMatches(
        AutoMatchCandidate candidate,
        string? referenceNumber,
        string? checkNumber)
    {
        if (string.IsNullOrWhiteSpace(candidate.CheckNumber)
            && string.IsNullOrWhiteSpace(candidate.Reference))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(checkNumber)
            && !string.IsNullOrWhiteSpace(candidate.CheckNumber)
            && string.Equals(checkNumber, candidate.CheckNumber, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(referenceNumber)
            && !string.IsNullOrWhiteSpace(candidate.Reference))
        {
            return referenceNumber.Contains(candidate.Reference, StringComparison.OrdinalIgnoreCase)
                || candidate.Reference.Contains(referenceNumber, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    internal static MatchConfidence GetConfidence(int score)
    {
        if (score >= 105)
            return MatchConfidence.Exact;
        if (score >= 85)
            return MatchConfidence.Probable;
        if (score >= 60)
            return MatchConfidence.ManualReview;
        return MatchConfidence.None;
    }
}
