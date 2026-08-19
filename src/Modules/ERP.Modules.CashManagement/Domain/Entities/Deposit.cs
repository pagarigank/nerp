// <copyright file="Deposit.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class Deposit : AuditableAggregateRoot
{
    private readonly List<DepositLine> _lines = [];

    protected Deposit() { }

    public Deposit(
        Guid companyId,
        Guid bankAccountId,
        string depositNumber,
        DateTimeOffset depositDate,
        string? reference)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(depositNumber))
            throw new ArgumentException("Deposit number is required.", nameof(depositNumber));

        CompanyId = companyId;
        BankAccountId = bankAccountId;
        DepositNumber = depositNumber;
        DepositDate = depositDate;
        Reference = reference;
        Status = DepositStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public string DepositNumber { get; private set; } = string.Empty;

    public DateTimeOffset DepositDate { get; private set; }

    public string? Reference { get; private set; }

    public DepositStatus Status { get; private set; }

    public decimal TotalAmount => _lines.Sum(l => l.Amount);

    public IReadOnlyList<DepositLine> Lines => _lines.AsReadOnly();

    public DepositLine AddLine(DepositLineSource source, Guid? sourceReferenceId, decimal amount, string? description)
    {
        if (Status != DepositStatus.Draft)
            throw new InvalidOperationException("Cannot modify a deposit that is not in Draft status.");

        if (amount <= 0)
            throw new ArgumentException("Line amount must be positive.", nameof(amount));

        var line = new DepositLine(Id, source, sourceReferenceId, amount, description);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        if (Status != DepositStatus.Draft)
            throw new InvalidOperationException("Cannot modify a deposit that is not in Draft status.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new ArgumentException($"Line {lineId} not found in deposit.");

        _lines.Remove(line);
    }

    public void Confirm()
    {
        if (Status != DepositStatus.Draft)
            throw new InvalidOperationException("Only a Draft deposit can be confirmed.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Deposit must have at least one line.");

        Status = DepositStatus.Confirmed;
    }

    public void Clear()
    {
        if (Status != DepositStatus.Confirmed)
            throw new InvalidOperationException("Only a Confirmed deposit can be cleared.");

        Status = DepositStatus.Cleared;
    }
}

public enum DepositStatus
{
    Draft = 0,
    Confirmed = 1,
    Cleared = 2,
    Voided = 3,
}

public enum DepositLineSource
{
    Manual = 0,
    ArCashReceipt = 1,
}

public record DepositConfirmedEvent : DomainEvent
{
    public DepositConfirmedEvent(Guid depositId, string depositNumber, Guid companyId, Guid bankAccountId, decimal totalAmount, DateTimeOffset depositDate)
    {
        DepositId = depositId;
        DepositNumber = depositNumber;
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        TotalAmount = totalAmount;
        DepositDate = depositDate;
    }

    public Guid DepositId { get; }
    public string DepositNumber { get; }
    public Guid CompanyId { get; }
    public Guid BankAccountId { get; }
    public decimal TotalAmount { get; }
    public DateTimeOffset DepositDate { get; }

    public override string EventType => "DepositConfirmed";
}
