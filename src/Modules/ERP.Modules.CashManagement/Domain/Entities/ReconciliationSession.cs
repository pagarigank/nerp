// <copyright file="ReconciliationSession.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class ReconciliationSession : AuditableAggregateRoot
{
    protected ReconciliationSession() { }

    public ReconciliationSession(
        Guid companyId,
        Guid bankAccountId,
        Guid bankStatementId,
        string sessionNumber,
        DateTimeOffset statementDate,
        decimal beginningBalance,
        decimal endingBalance)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(sessionNumber))
            throw new ArgumentException("Session number is required.", nameof(sessionNumber));

        CompanyId = companyId;
        BankAccountId = bankAccountId;
        BankStatementId = bankStatementId;
        SessionNumber = sessionNumber;
        StatementDate = statementDate;
        BeginningBalance = beginningBalance;
        EndingBalance = endingBalance;
        Status = ReconciliationStatus.InProgress;
    }

    public Guid CompanyId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public Guid BankStatementId { get; private set; }

    public string SessionNumber { get; private set; } = string.Empty;

    public DateTimeOffset StatementDate { get; private set; }

    public decimal BeginningBalance { get; private set; }

    public decimal EndingBalance { get; private set; }

    public decimal? Variance { get; private set; }

    public Guid? GlJournalBatchId { get; private set; }

    public DateTimeOffset? LockedOn { get; private set; }

    public string? LockedBy { get; private set; }

    public ReconciliationStatus Status { get; private set; }

    public void RecordVariance(decimal variance)
    {
        Variance = variance;
    }

    public void Lock(string lockedBy)
    {
        if (Status != ReconciliationStatus.InProgress)
            throw new InvalidOperationException("Only an in-progress reconciliation can be locked.");

        Status = ReconciliationStatus.Locked;
        LockedOn = DateTimeOffset.UtcNow;
        LockedBy = lockedBy;
        AddDomainEvent(new BankReconciledEvent(
            Id,
            SessionNumber,
            CompanyId,
            BankAccountId,
            BankStatementId,
            Variance,
            GlJournalBatchId));
    }

    public void AttachGlJournal(Guid journalBatchId)
    {
        if (Status != ReconciliationStatus.InProgress)
            throw new InvalidOperationException("Cannot attach GL journal to a locked reconciliation.");

        GlJournalBatchId = journalBatchId;
    }
}

public enum ReconciliationStatus
{
    InProgress = 0,
    Locked = 1,
}

public record BankReconciledEvent : DomainEvent
{
    public BankReconciledEvent(
        Guid sessionId,
        string sessionNumber,
        Guid companyId,
        Guid bankAccountId,
        Guid bankStatementId,
        decimal? variance,
        Guid? glJournalBatchId)
    {
        SessionId = sessionId;
        SessionNumber = sessionNumber;
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        BankStatementId = bankStatementId;
        Variance = variance;
        GlJournalBatchId = glJournalBatchId;
    }

    public Guid SessionId { get; }
    public string SessionNumber { get; }
    public Guid CompanyId { get; }
    public Guid BankAccountId { get; }
    public Guid BankStatementId { get; }
    public decimal? Variance { get; }
    public Guid? GlJournalBatchId { get; }

    public override string EventType => "BankReconciled";
}
