// <copyright file="IntercompanyMapping.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class IntercompanyMapping : AuditableAggregateRoot
{
    protected IntercompanyMapping() { }

    public IntercompanyMapping(
        Guid fromCompanyId,
        Guid toCompanyId,
        string fromAccountNumber,
        string toAccountNumber,
        string description)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(fromAccountNumber))
            throw new ArgumentException("From account number is required.", nameof(fromAccountNumber));

        if (string.IsNullOrWhiteSpace(toAccountNumber))
            throw new ArgumentException("To account number is required.", nameof(toAccountNumber));

        FromCompanyId = fromCompanyId;
        ToCompanyId = toCompanyId;
        FromAccountNumber = fromAccountNumber;
        ToAccountNumber = toAccountNumber;
        Description = description ?? string.Empty;
        IsActive = true;
    }

    public Guid FromCompanyId { get; private set; }

    public Guid ToCompanyId { get; private set; }

    public string FromAccountNumber { get; private set; } = string.Empty;

    public string ToAccountNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void Update(string fromAccountNumber, string toAccountNumber, string description)
    {
        if (string.IsNullOrWhiteSpace(fromAccountNumber))
            throw new ArgumentException("From account number is required.", nameof(fromAccountNumber));

        if (string.IsNullOrWhiteSpace(toAccountNumber))
            throw new ArgumentException("To account number is required.", nameof(toAccountNumber));

        FromAccountNumber = fromAccountNumber;
        ToAccountNumber = toAccountNumber;
        Description = description ?? string.Empty;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}