// <copyright file="Phase5Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

/// <summary>
/// Maps a bank account to a GL cash account so deposits, fees, transfers, and
/// variance postings hit the right GL account per company. [GAP-2026-08-18]
/// </summary>
public class BankGlMapping : AuditableAggregateRoot
{
    protected BankGlMapping() { }

    public BankGlMapping(Guid companyId, Guid bankAccountId, Guid glAccountId, bool isDefault)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        GlAccountId = glAccountId;
        IsDefault = isDefault;
    }

    public Guid CompanyId { get; private set; }
    public Guid BankAccountId { get; private set; }
    public Guid GlAccountId { get; private set; }
    public bool IsDefault { get; private set; }

    public void Update(Guid glAccountId, bool isDefault)
    {
        GlAccountId = glAccountId;
        IsDefault = isDefault;
    }
}

/// <summary>
/// Lockbox / remote-deposit-capture import: a feed file that auto-creates AR cash receipts. [GAP-2026-08-18]
/// </summary>
public class LockboxBatch : AuditableAggregateRoot
{
    private readonly List<LockboxItem> _items = [];

    protected LockboxBatch() { }

    public LockboxBatch(Guid companyId, string batchNumber, string fileName, string format)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("Batch number is required.", nameof(batchNumber));

        CompanyId = companyId;
        BatchNumber = batchNumber;
        FileName = fileName ?? string.Empty;
        Format = format ?? "CSV";
        ImportedOn = DateTimeOffset.UtcNow;
        Status = LockboxBatchStatus.Imported;
    }

    public Guid CompanyId { get; private set; }
    public string BatchNumber { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string Format { get; private set; } = "CSV";
    public DateTimeOffset ImportedOn { get; private set; }
    public LockboxBatchStatus Status { get; private set; }
    public int TotalItems => _items.Count;
    public decimal TotalAmount => _items.Sum(i => i.Amount);

    public IReadOnlyList<LockboxItem> Items => _items.AsReadOnly();

    public void AddItem(string referenceNumber, Guid? customerId, string customerName, decimal amount, DateTimeOffset? remittanceDate, string? invoiceNumber)
    {
        _items.Add(new LockboxItem(Id, referenceNumber, customerId, customerName, amount, remittanceDate, invoiceNumber));
    }

    public void Post()
    {
        if (Status == LockboxBatchStatus.Posted)
            throw new InvalidOperationException("Lockbox batch is already posted.");
        Status = LockboxBatchStatus.Posted;
    }
}

public class LockboxItem : Entity
{
    protected LockboxItem() { }

    internal LockboxItem(Guid lockboxBatchId, string referenceNumber, Guid? customerId, string customerName, decimal amount, DateTimeOffset? remittanceDate, string? invoiceNumber)
        : base(Guid.NewGuid())
    {
        LockboxBatchId = lockboxBatchId;
        ReferenceNumber = referenceNumber ?? string.Empty;
        CustomerId = customerId;
        CustomerName = customerName ?? string.Empty;
        Amount = amount;
        RemittanceDate = remittanceDate;
        InvoiceNumber = invoiceNumber;
    }

    public Guid LockboxBatchId { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset? RemittanceDate { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public bool ReceiptCreated { get; private set; }

    public void MarkReceiptCreated()
    {
        ReceiptCreated = true;
    }
}

public enum LockboxBatchStatus
{
    Imported = 0,
    Posted = 1,
    Reversed = 2,
}

/// <summary>
/// Stale-dated check handling + escheatment workflow. [GAP-2026-08-18]
/// </summary>
public class StaleCheckEscheatment : AuditableAggregateRoot
{
    protected StaleCheckEscheatment() { }

    public StaleCheckEscheatment(
        Guid companyId,
        Guid bankAccountId,
        Guid? checkId,
        string checkNumber,
        decimal amount,
        DateTimeOffset issueDate,
        string payee,
        string state)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        CheckId = checkId;
        CheckNumber = checkNumber ?? string.Empty;
        Amount = amount;
        IssueDate = issueDate;
        Payee = payee ?? string.Empty;
        State = state ?? string.Empty;
        Status = StaleCheckEscheatmentStatus.Identified;
    }

    public Guid CompanyId { get; private set; }
    public Guid BankAccountId { get; private set; }
    public Guid? CheckId { get; private set; }
    public string CheckNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset IssueDate { get; private set; }
    public string Payee { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public StaleCheckEscheatmentStatus Status { get; private set; }
    public DateTimeOffset? EscheatedOn { get; private set; }
    public DateTimeOffset? ReissuedOn { get; private set; }

    public void Escheat(DateTimeOffset escheatedOn)
    {
        if (Status != StaleCheckEscheatmentStatus.Identified)
            throw new InvalidOperationException($"Cannot escheat a check in status {Status}.");
        Status = StaleCheckEscheatmentStatus.Escheated;
        EscheatedOn = escheatedOn;
    }

    public void Reissue(DateTimeOffset reissuedOn)
    {
        if (Status != StaleCheckEscheatmentStatus.Identified && Status != StaleCheckEscheatmentStatus.Escheated)
            throw new InvalidOperationException($"Cannot reissue a check in status {Status}.");
        Status = StaleCheckEscheatmentStatus.Reissued;
        ReissuedOn = reissuedOn;
    }
}

public enum StaleCheckEscheatmentStatus
{
    Identified = 0,
    Escheated = 1,
    Reissued = 2,
}

/// <summary>
/// Positive pay exception handling: bank returns an unmatched item; decide pay/no-pay. [GAP-2026-08-18]
/// </summary>
public class PositivePayDiscrepancy : AuditableAggregateRoot
{
    protected PositivePayDiscrepancy() { }

    public PositivePayDiscrepancy(Guid companyId, Guid bankAccountId, string checkNumber, decimal amount, DateTimeOffset issueDate, string decisionReason)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        CheckNumber = checkNumber ?? string.Empty;
        Amount = amount;
        IssueDate = issueDate;
        Decision = PositivePayDecision.Pending;
        DecisionReason = decisionReason ?? string.Empty;
        ReceivedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid BankAccountId { get; private set; }
    public string CheckNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset IssueDate { get; private set; }
    public PositivePayDecision Decision { get; private set; }
    public string DecisionReason { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedOn { get; private set; }
    public DateTimeOffset? DecidedOn { get; private set; }

    public void Decide(PositivePayDecision decision, string decisionReason)
    {
        if (Decision != PositivePayDecision.Pending)
            throw new InvalidOperationException($"Exception already decided ({Decision}).");
        Decision = decision;
        DecisionReason = decisionReason ?? string.Empty;
        DecidedOn = DateTimeOffset.UtcNow;
    }
}

public enum PositivePayDecision
{
    Pending = 0,
    Pay = 1,
    NoPay = 2,
}

/// <summary>
/// Duplicate bank line detection across imports: same check # + amount + date. [GAP-2026-08-18]
/// </summary>
public class BankDuplicateLine : AuditableAggregateRoot
{
    protected BankDuplicateLine() { }

    public BankDuplicateLine(Guid companyId, Guid bankAccountId, string checkNumber, decimal amount, DateTimeOffset transactionDate, Guid statementLineId, Guid statementId)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        CheckNumber = checkNumber ?? string.Empty;
        Amount = amount;
        TransactionDate = transactionDate;
        StatementLineId = statementLineId;
        StatementId = statementId;
        DetectedOn = DateTimeOffset.UtcNow;
        Resolved = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid BankAccountId { get; private set; }
    public string CheckNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset TransactionDate { get; private set; }
    public Guid StatementLineId { get; private set; }
    public Guid StatementId { get; private set; }
    public DateTimeOffset DetectedOn { get; private set; }
    public bool Resolved { get; private set; }

    public void MarkResolved()
    {
        Resolved = true;
    }
}

/// <summary>
/// Bank fee analysis aggregate: precomputed fee totals by type / account / month. [GAP-2026-08-18]
/// </summary>
public class BankFeeAnalysis : AuditableAggregateRoot
{
    private readonly List<BankFeeAnalysisLine> _lines = [];

    protected BankFeeAnalysis() { }

    public BankFeeAnalysis(Guid companyId, int year, int month)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Year = year;
        Month = month;
        GeneratedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateTimeOffset GeneratedOn { get; private set; }
    public decimal TotalFees => _lines.Sum(l => l.Amount);

    public IReadOnlyList<BankFeeAnalysisLine> Lines => _lines.AsReadOnly();

    public void AddLine(string feeType, Guid? bankAccountId, decimal amount, int count)
    {
        _lines.Add(new BankFeeAnalysisLine(Id, feeType, bankAccountId, amount, count));
    }
}

public class BankFeeAnalysisLine : Entity
{
    protected BankFeeAnalysisLine() { }

    internal BankFeeAnalysisLine(Guid analysisId, string feeType, Guid? bankAccountId, decimal amount, int count)
        : base(Guid.NewGuid())
    {
        AnalysisId = analysisId;
        FeeType = feeType ?? string.Empty;
        BankAccountId = bankAccountId;
        Amount = amount;
        Count = count;
    }

    public Guid AnalysisId { get; private set; }
    public string FeeType { get; private set; } = string.Empty;
    public Guid? BankAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public int Count { get; private set; }
}
