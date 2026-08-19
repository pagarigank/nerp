// <copyright file="BankStatement.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class BankStatement : AuditableAggregateRoot
{
    private readonly List<BankStatementLine> _lines = [];

    protected BankStatement() { }

    public BankStatement(
        Guid companyId,
        Guid bankAccountId,
        string statementNumber,
        DateTimeOffset statementDate,
        decimal beginningBalance,
        decimal endingBalance,
        string? fileName,
        BankStatementFormat format)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BankAccountId = bankAccountId;
        StatementNumber = statementNumber ?? string.Empty;
        StatementDate = statementDate;
        BeginningBalance = beginningBalance;
        EndingBalance = endingBalance;
        FileName = fileName;
        Format = format;
        Status = BankStatementStatus.Imported;
    }

    public Guid CompanyId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public string StatementNumber { get; private set; } = string.Empty;

    public DateTimeOffset StatementDate { get; private set; }

    public decimal BeginningBalance { get; private set; }

    public decimal EndingBalance { get; private set; }

    public string? FileName { get; private set; }

    public BankStatementFormat Format { get; private set; }

    public BankStatementStatus Status { get; private set; }

    public IReadOnlyList<BankStatementLine> Lines => _lines.AsReadOnly();

    public void AddLine(
        DateTimeOffset transactionDate,
        decimal amount,
        string? description,
        string? referenceNumber,
        string? checkNumber,
        decimal balance)
    {
        var line = new BankStatementLine(
            Id,
            transactionDate,
            amount,
            description,
            referenceNumber,
            checkNumber,
            balance);
        _lines.Add(line);
    }

    public void MarkValidated()
    {
        if (Status != BankStatementStatus.Imported)
            throw new InvalidOperationException("Only an Imported statement can be validated.");

        Status = BankStatementStatus.Validated;
    }

    public void MarkReconciled()
    {
        Status = BankStatementStatus.Reconciled;
    }

    public void MarkLocked()
    {
        if (Status != BankStatementStatus.Reconciled)
            throw new InvalidOperationException("Only a Reconciled statement can be locked.");

        Status = BankStatementStatus.Locked;
    }
}

public enum BankStatementStatus
{
    Imported = 0,
    Validated = 1,
    Reconciled = 2,
    Locked = 3,
}

public enum BankStatementFormat
{
    Csv = 0,
    Ofx = 1,
    Bai2 = 2,
    Qbo = 3,
}
