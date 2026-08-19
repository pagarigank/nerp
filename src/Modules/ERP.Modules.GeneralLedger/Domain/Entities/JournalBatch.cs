// <copyright file="JournalBatch.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class JournalBatch : AuditableAggregateRoot
{
    private readonly List<JournalEntryLine> _lines = [];

    protected JournalBatch() { }

    public JournalBatch(
        Guid companyId,
        string batchNumber,
        string description,
        DateTimeOffset postingDate,
        Guid fiscalPeriodId,
        Guid? currencyId = null) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("Batch number is required.", nameof(batchNumber));

        CompanyId = companyId;
        BatchNumber = batchNumber;
        Description = description ?? string.Empty;
        PostingDate = postingDate;
        FiscalPeriodId = fiscalPeriodId;
        CurrencyId = currencyId;
        Status = JournalBatchStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public string BatchNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset PostingDate { get; private set; }

    public Guid FiscalPeriodId { get; private set; }

    public Guid? CurrencyId { get; private set; }

    public JournalBatchStatus Status { get; private set; }

    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    public void AddLine(Guid accountId, decimal? debit, decimal? credit, string? reference = null, string? segmentsJson = null, Guid? currencyId = null, decimal? foreignDebit = null, decimal? foreignCredit = null, decimal exchangeRate = 1.0m)
    {
        if (Status != JournalBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        if ((debit.HasValue && credit.HasValue) || (!debit.HasValue && !credit.HasValue))
            throw new ArgumentException("Each line must have either a debit OR a credit amount, not both.");

        if (debit.HasValue && debit <= 0)
            throw new ArgumentException("Debit amount must be positive.", nameof(debit));

        if (credit.HasValue && credit <= 0)
            throw new ArgumentException("Credit amount must be positive.", nameof(credit));

        var line = new JournalEntryLine(Id, accountId, debit ?? 0, credit ?? 0, reference, segmentsJson, currencyId, foreignDebit, foreignCredit, exchangeRate);
        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        if (Status != JournalBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new ArgumentException($"Line {lineId} not found in batch.");

        _lines.Remove(line);
    }

    public bool IsBalanced()
    {
        if (_lines.Count < 2)
            return false;

        var totalDebits = _lines.Sum(l => l.Debit);
        var totalCredits = _lines.Sum(l => l.Credit);

        return Math.Round(totalDebits, 2) == Math.Round(totalCredits, 2);
    }

    public void Release()
    {
        if (Status != JournalBatchStatus.Draft)
            throw new InvalidOperationException("Only a Draft batch can be released.");

        if (_lines.Count < 2)
            throw new InvalidOperationException("Batch must have at least two lines.");

        if (!IsBalanced())
            throw new InvalidOperationException("Batch must be balanced (debits = credits) before release.");

        if (_lines.Any(l => l.AccountId == Guid.Empty))
            throw new InvalidOperationException("All lines must reference a valid account.");

        Status = JournalBatchStatus.Balanced;
    }

    public void Post()
    {
        if (Status != JournalBatchStatus.Balanced)
            throw new InvalidOperationException("Only a Balanced batch can be posted.");

        Status = JournalBatchStatus.Posted;
        AddDomainEvent(new JournalBatchPostedEvent(
            Id,
            BatchNumber,
            CompanyId,
            FiscalPeriodId,
            PostingDate,
            [.. _lines.Select(l => new PostedLine(l.AccountId, l.Debit, l.Credit))]));
    }

    public JournalBatch Reverse(string reversedReason)
    {
        if (Status != JournalBatchStatus.Posted)
            throw new InvalidOperationException("Only a Posted batch can be reversed.");

        if (string.IsNullOrWhiteSpace(reversedReason))
            throw new ArgumentException("A reversal reason is required.", nameof(reversedReason));

        Status = JournalBatchStatus.Reversed;

        var reversal = new JournalBatch(
            CompanyId,
            $"REV-{BatchNumber}",
            $"Reversal: {Description} - {reversedReason}",
            DateTimeOffset.UtcNow,
            FiscalPeriodId,
            CurrencyId);

        foreach (var line in _lines)
        {
            reversal.AddLine(line.AccountId, line.Credit > 0 ? line.Credit : null, line.Debit > 0 ? line.Debit : null, $"Reversal of: {line.Reference ?? BatchNumber}");
        }

        return reversal;
    }

    public void UnbalancedDraft()
    {
        if (Status != JournalBatchStatus.Balanced)
            return;

        Status = JournalBatchStatus.Draft;
    }

    public void UpdateDescription(string description)
    {
        if (Status != JournalBatchStatus.Draft)
            throw new InvalidOperationException("Cannot modify a batch that is not in Draft status.");

        Description = description ?? string.Empty;
    }
}

public enum JournalBatchStatus
{
    Draft = 0,
    Balanced = 1,
    Posted = 2,
    Reversed = 3,
}

public record JournalBatchPostedEvent : DomainEvent
{
    public JournalBatchPostedEvent(
        Guid batchId,
        string batchNumber,
        Guid companyId,
        Guid fiscalPeriodId,
        DateTimeOffset postingDate,
        IReadOnlyList<PostedLine> lines)
    {
        BatchId = batchId;
        BatchNumber = batchNumber;
        CompanyId = companyId;
        FiscalPeriodId = fiscalPeriodId;
        PostingDate = postingDate;
        Lines = lines;
    }

    public Guid BatchId { get; }
    public string BatchNumber { get; }
    public Guid CompanyId { get; }
    public Guid FiscalPeriodId { get; }
    public DateTimeOffset PostingDate { get; }
    public IReadOnlyList<PostedLine> Lines { get; }

    public override string EventType => "JournalBatchPosted";
}

public record PostedLine : DomainEvent
{
    public PostedLine(Guid accountId, decimal debit, decimal credit)
    {
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
    }

    public Guid AccountId { get; }
    public decimal Debit { get; }
    public decimal Credit { get; }

    public override string EventType => "PostedLine";
}
