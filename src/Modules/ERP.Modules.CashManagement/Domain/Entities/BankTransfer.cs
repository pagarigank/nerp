// <copyright file="BankTransfer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class BankTransfer : AuditableAggregateRoot
{
    protected BankTransfer() { }

    public BankTransfer(
        Guid companyId,
        Guid fromBankAccountId,
        Guid toBankAccountId,
        string transferNumber,
        decimal amount,
        DateTimeOffset transferDate,
        string? reference)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(transferNumber))
            throw new ArgumentException("Transfer number is required.", nameof(transferNumber));

        if (fromBankAccountId == toBankAccountId)
            throw new ArgumentException("Source and destination bank accounts must differ.");

        if (amount <= 0)
            throw new ArgumentException("Transfer amount must be positive.", nameof(amount));

        CompanyId = companyId;
        FromBankAccountId = fromBankAccountId;
        ToBankAccountId = toBankAccountId;
        TransferNumber = transferNumber;
        Amount = amount;
        TransferDate = transferDate;
        Reference = reference;
        Status = BankTransferStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public Guid FromBankAccountId { get; private set; }

    public Guid ToBankAccountId { get; private set; }

    public string TransferNumber { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateTimeOffset TransferDate { get; private set; }

    public string? Reference { get; private set; }

    public BankTransferStatus Status { get; private set; }

    public void Confirm()
    {
        if (Status != BankTransferStatus.Draft)
            throw new InvalidOperationException("Only a Draft transfer can be confirmed.");

        Status = BankTransferStatus.InTransit;
    }

    public void Complete()
    {
        if (Status != BankTransferStatus.InTransit)
            throw new InvalidOperationException("Only an InTransit transfer can be completed.");

        Status = BankTransferStatus.Completed;
    }

    public void Void(string? reason)
    {
        if (Status == BankTransferStatus.Completed)
            throw new InvalidOperationException("A completed transfer cannot be voided; post a reversing transfer instead.");

        Status = BankTransferStatus.Voided;
        Reference = string.IsNullOrWhiteSpace(reason) ? Reference : $"{Reference} [VOIDED: {reason}]".Trim();
    }
}

public enum BankTransferStatus
{
    Draft = 0,
    InTransit = 1,
    Completed = 2,
    Voided = 3,
}
