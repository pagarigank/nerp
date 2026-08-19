// <copyright file="BankContact.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class BankContact : Entity
{
    protected BankContact() { }

    internal BankContact(
        Guid bankAccountId,
        string name,
        string? phone,
        string? email,
        string? title)
        : base(Guid.NewGuid())
    {
        BankAccountId = bankAccountId;
        Name = name;
        Phone = phone;
        Email = email;
        Title = title;
    }

    public Guid BankAccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Title { get; private set; }

    public void Update(string name, string? phone, string? email, string? title)
    {
        Name = name ?? throw new ArgumentException("Contact name is required.", nameof(name));
        Phone = phone;
        Email = email;
        Title = title;
    }
}
