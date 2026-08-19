// <copyright file="BankAccount.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class BankAccount : AuditableAggregateRoot
{
    private readonly List<BankContact> _contacts = [];

    protected BankAccount() { }

    public BankAccount(
        Guid companyId,
        string accountCode,
        string accountName,
        string accountNumber,
        string? routingNumber,
        string? bankName,
        string? currencyCode,
        BankAccountType accountType,
        decimal openingBalance,
        Guid? glAccountId)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new ArgumentException("Account code is required.", nameof(accountCode));
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));

        CompanyId = companyId;
        AccountCode = accountCode;
        AccountName = accountName;
        AccountNumber = accountNumber;
        RoutingNumber = routingNumber ?? string.Empty;
        BankName = bankName ?? string.Empty;
        CurrencyCode = currencyCode ?? "USD";
        AccountType = accountType;
        OpeningBalance = openingBalance;
        CurrentBalance = openingBalance;
        GlAccountId = glAccountId;
        Status = BankAccountStatus.Active;
    }

    public Guid CompanyId { get; private set; }

    public string AccountCode { get; private set; } = string.Empty;

    public string AccountName { get; private set; } = string.Empty;

    public string AccountNumber { get; private set; } = string.Empty;

    public string RoutingNumber { get; private set; } = string.Empty;

    public string BankName { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = "USD";

    public BankAccountType AccountType { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public decimal CurrentBalance { get; private set; }

    public Guid? GlAccountId { get; private set; }

    public BankAccountStatus Status { get; private set; }

    public IReadOnlyList<BankContact> Contacts => _contacts.AsReadOnly();

    public void Update(
        string accountName,
        string accountNumber,
        string? routingNumber,
        string? bankName,
        string? currencyCode,
        BankAccountType accountType,
        Guid? glAccountId)
    {
        if (Status == BankAccountStatus.Closed)
            throw new InvalidOperationException("Cannot update a closed bank account.");

        AccountName = accountName ?? throw new ArgumentException("Account name is required.", nameof(accountName));
        AccountNumber = accountNumber ?? throw new ArgumentException("Account number is required.", nameof(accountNumber));
        RoutingNumber = routingNumber ?? string.Empty;
        BankName = bankName ?? string.Empty;
        CurrencyCode = currencyCode ?? "USD";
        AccountType = accountType;
        GlAccountId = glAccountId;
    }

    public void AddContact(string name, string? phone, string? email, string? title)
    {
        if (Status == BankAccountStatus.Closed)
            throw new InvalidOperationException("Cannot add a contact to a closed bank account.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Contact name is required.", nameof(name));

        _contacts.Add(new BankContact(Id, name, phone, email, title));
    }

    public void Activate()
    {
        if (Status == BankAccountStatus.Closed)
            throw new InvalidOperationException("Cannot activate a closed bank account.");

        Status = BankAccountStatus.Active;
    }

    public void Deactivate()
    {
        if (Status == BankAccountStatus.Closed)
            throw new InvalidOperationException("Cannot deactivate a closed bank account.");

        Status = BankAccountStatus.Inactive;
    }

    public void Close()
    {
        if (Status == BankAccountStatus.Closed)
            throw new InvalidOperationException("Bank account is already closed.");

        Status = BankAccountStatus.Closed;
    }

    public void AdjustBalance(decimal amount)
    {
        if (Status == BankAccountStatus.Closed)
            throw new InvalidOperationException("Cannot adjust a closed bank account.");

        CurrentBalance += amount;
    }
}

public enum BankAccountType
{
    Checking = 0,
    Savings = 1,
    MoneyMarket = 2,
    PettyCash = 3,
    Investment = 4,
}

public enum BankAccountStatus
{
    Active = 0,
    Inactive = 1,
    Closed = 2,
}
