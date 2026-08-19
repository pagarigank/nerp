// <copyright file="BankFee.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class BankFee : AuditableAggregateRoot
{
    protected BankFee() { }

    public BankFee(
        Guid companyId,
        Guid bankAccountId,
        string feeNumber,
        BankFeeType feeType,
        decimal amount,
        DateTimeOffset feeDate,
        string? description)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(feeNumber))
            throw new ArgumentException("Fee number is required.", nameof(feeNumber));

        if (amount <= 0)
            throw new ArgumentException("Fee amount must be positive.", nameof(amount));

        CompanyId = companyId;
        BankAccountId = bankAccountId;
        FeeNumber = feeNumber;
        FeeType = feeType;
        Amount = amount;
        FeeDate = feeDate;
        Description = description;
        Status = BankFeeStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public string FeeNumber { get; private set; } = string.Empty;

    public BankFeeType FeeType { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset FeeDate { get; private set; }

    public string? Description { get; private set; }

    public Guid? GlJournalBatchId { get; private set; }

    public BankFeeStatus Status { get; private set; }

    public void Post()
    {
        if (Status != BankFeeStatus.Draft)
            throw new InvalidOperationException("Only a Draft bank fee can be posted.");

        Status = BankFeeStatus.Posted;
    }

    public void AttachGlJournal(Guid journalBatchId)
    {
        if (Status != BankFeeStatus.Draft)
            throw new InvalidOperationException("GL journal can only be attached to a Draft bank fee.");

        GlJournalBatchId = journalBatchId;
    }

    public void Void(string? reason)
    {
        if (Status == BankFeeStatus.Voided)
            throw new InvalidOperationException("Bank fee is already voided.");

        Status = BankFeeStatus.Voided;
        Description = string.IsNullOrWhiteSpace(reason) ? Description : $"{Description} [VOIDED: {reason}]".Trim();
    }
}

public enum BankFeeType
{
    ServiceCharge = 0,
    WireFee = 1,
    ACHFee = 2,
    OverdraftFee = 3,
    NsfFee = 4,
    CreditCardProcessing = 5,
    Other = 6,
}

public enum BankFeeStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2,
}
