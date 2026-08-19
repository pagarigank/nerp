// <copyright file="Account.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class Account : AuditableAggregateRoot
{
    protected Account() { }

    public Account(
        Guid companyId,
        string accountNumber,
        string description,
        AccountType accountType,
        NormalBalance normalBalance,
        bool isActive) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }

    public string AccountNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public AccountType AccountType { get; private set; }

    public NormalBalance NormalBalance { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string description, AccountType accountType, NormalBalance normalBalance, bool isActive)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsActive = isActive;
    }
}

public enum AccountType
{
    Asset = 0,
    Liability = 1,
    Equity = 2,
    Revenue = 3,
    Expense = 4
}

public enum NormalBalance
{
    Debit = 0,
    Credit = 1
}
