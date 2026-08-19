// <copyright file="NsfRecord.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class NsfRecord : AuditableAggregateRoot
{
    protected NsfRecord() { }

    public NsfRecord(
        Guid companyId,
        Guid bankAccountId,
        Guid? cashReceiptId,
        Guid? customerId,
        string nsfNumber,
        decimal amount,
        DateTimeOffset returnedDate,
        string? bankReference,
        string? reason)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(nsfNumber))
            throw new ArgumentException("NSF number is required.", nameof(nsfNumber));

        if (amount <= 0)
            throw new ArgumentException("NSF amount must be positive.", nameof(amount));

        CompanyId = companyId;
        BankAccountId = bankAccountId;
        CashReceiptId = cashReceiptId;
        CustomerId = customerId;
        NsfNumber = nsfNumber;
        Amount = amount;
        ReturnedDate = returnedDate;
        BankReference = bankReference;
        Reason = reason;
        Status = NsfStatus.Processed;
    }

    public Guid CompanyId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public Guid? CashReceiptId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string NsfNumber { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateTimeOffset ReturnedDate { get; private set; }

    public string? BankReference { get; private set; }

    public string? Reason { get; private set; }

    public decimal? NsfFeeAmount { get; private set; }

    public Guid? NsfFeeId { get; private set; }

    public NsfStatus Status { get; private set; }

    public void AttachNsfFee(decimal feeAmount, Guid feeId)
    {
        if (feeAmount < 0)
            throw new ArgumentException("NSF fee amount cannot be negative.", nameof(feeAmount));

        NsfFeeAmount = feeAmount;
        NsfFeeId = feeId;
    }

    public void Void(string? reason)
    {
        if (Status != NsfStatus.Processed)
            throw new InvalidOperationException("Only a Processed NSF record can be voided.");

        Status = NsfStatus.Voided;
        Reason = string.IsNullOrWhiteSpace(reason) ? Reason : $"{Reason} [VOIDED: {reason}]".Trim();
    }
}

public enum NsfStatus
{
    Processed = 0,
    Voided = 1,
}
