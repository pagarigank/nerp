// <copyright file="VendorBankAccount.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class VendorBankAccount : Entity
{
    protected VendorBankAccount() { }

    internal VendorBankAccount(
        Guid vendorId,
        string bankName,
        string accountNumber,
        string? routingNumber,
        bool isDefault)
        : base(Guid.NewGuid())
    {
        VendorId = vendorId;
        BankName = bankName ?? throw new ArgumentNullException(nameof(bankName));
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        RoutingNumber = routingNumber;
        IsDefault = isDefault;
    }

    public Guid VendorId { get; private set; }

    public string BankName { get; private set; } = string.Empty;

    public string AccountNumber { get; private set; } = string.Empty;

    public string? RoutingNumber { get; private set; }

    public bool IsDefault { get; private set; }

    internal void ClearDefault() => IsDefault = false;
}
