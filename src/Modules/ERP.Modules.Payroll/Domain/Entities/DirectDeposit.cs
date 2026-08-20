// <copyright file="DirectDeposit.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Employee direct-deposit bank account. One account may be marked remainder
/// (net pay after allocations), the rest allocate a fixed amount or percentage.
/// Bank details are PII — stored encrypted at rest per architecture.md §6.
/// </summary>
public class DirectDeposit : AuditableEntity
{
    protected DirectDeposit() { }

    public DirectDeposit(
        Guid companyId,
        Guid employeeId,
        string bankName,
        string routingNumber,
        string accountNumberEncrypted,
        string accountType,
        decimal? allocationPercentage,
        decimal? fixedAmount,
        bool isRemainder)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name is required.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(routingNumber))
            throw new ArgumentException("Routing number is required.", nameof(routingNumber));
        if (string.IsNullOrWhiteSpace(accountNumberEncrypted))
            throw new ArgumentException("Account number is required.", nameof(accountNumberEncrypted));

        CompanyId = companyId;
        EmployeeId = employeeId;
        BankName = bankName;
        RoutingNumber = routingNumber;
        AccountNumberEncrypted = accountNumberEncrypted;
        AccountType = accountType;
        AllocationPercentage = allocationPercentage;
        FixedAmount = fixedAmount;
        IsRemainder = isRemainder;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string BankName { get; private set; } = string.Empty;
    public string RoutingNumber { get; private set; } = string.Empty;
    public string AccountNumberEncrypted { get; private set; } = string.Empty;
    public string AccountType { get; private set; } = string.Empty;
    public decimal? AllocationPercentage { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public bool IsRemainder { get; private set; }

    public void Update(
        string bankName,
        string routingNumber,
        string accountNumberEncrypted,
        string accountType,
        decimal? allocationPercentage,
        decimal? fixedAmount,
        bool isRemainder)
    {
        BankName = bankName;
        RoutingNumber = routingNumber;
        AccountNumberEncrypted = accountNumberEncrypted;
        AccountType = accountType;
        AllocationPercentage = allocationPercentage;
        FixedAmount = fixedAmount;
        IsRemainder = isRemainder;
    }
}
