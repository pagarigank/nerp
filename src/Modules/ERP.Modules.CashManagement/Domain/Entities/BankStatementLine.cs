// <copyright file="BankStatementLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class BankStatementLine : Entity
{
    protected BankStatementLine() { }

    internal BankStatementLine(
        Guid bankStatementId,
        DateTimeOffset transactionDate,
        decimal amount,
        string? description,
        string? referenceNumber,
        string? checkNumber,
        decimal balance)
        : base(Guid.NewGuid())
    {
        BankStatementId = bankStatementId;
        TransactionDate = transactionDate;
        Amount = amount;
        Description = description ?? string.Empty;
        ReferenceNumber = referenceNumber ?? string.Empty;
        CheckNumber = checkNumber ?? string.Empty;
        Balance = balance;
        Status = BankStatementLineStatus.Unreconciled;
    }

    public Guid BankStatementId { get; private set; }

    public DateTimeOffset TransactionDate { get; private set; }

    /// <summary>
    /// Gets the signed amount. Positive represents a deposit/credit, negative represents a withdrawal/debit.
    /// </summary>
    public decimal Amount { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string ReferenceNumber { get; private set; } = string.Empty;

    public string CheckNumber { get; private set; } = string.Empty;

    public decimal Balance { get; private set; }

    public BankStatementLineStatus Status { get; private set; }

    public Guid? MatchedTransactionId { get; private set; }

    public BankMatchSource? MatchedSource { get; private set; }

    public void MarkMatched(Guid matchedTransactionId, BankMatchSource source)
    {
        if (Status == BankStatementLineStatus.Locked)
            throw new InvalidOperationException("Cannot modify a locked statement line.");

        Status = BankStatementLineStatus.Matched;
        MatchedTransactionId = matchedTransactionId;
        MatchedSource = source;
    }

    public void MarkCleared()
    {
        if (Status == BankStatementLineStatus.Locked)
            throw new InvalidOperationException("Cannot modify a locked statement line.");

        Status = BankStatementLineStatus.Cleared;
    }

    public void MarkUnmatched()
    {
        if (Status == BankStatementLineStatus.Locked)
            throw new InvalidOperationException("Cannot modify a locked statement line.");

        Status = BankStatementLineStatus.Unreconciled;
        MatchedTransactionId = null;
        MatchedSource = null;
    }

    public void Lock()
    {
        Status = BankStatementLineStatus.Locked;
    }
}

public enum BankStatementLineStatus
{
    Unreconciled = 0,
    Matched = 1,
    Cleared = 2,
    Locked = 3,
}

public enum BankMatchSource
{
    ApPayment = 0,
    ArCashReceipt = 1,
    Deposit = 2,
    BankTransfer = 3,
    BankAdjustment = 4,
}
