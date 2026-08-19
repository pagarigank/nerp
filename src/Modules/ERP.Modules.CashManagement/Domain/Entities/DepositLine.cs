// <copyright file="DepositLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.CashManagement.Domain.Entities;

public class DepositLine : Entity
{
    protected DepositLine() { }

    internal DepositLine(
        Guid depositId,
        DepositLineSource source,
        Guid? sourceReferenceId,
        decimal amount,
        string? description)
        : base(Guid.NewGuid())
    {
        DepositId = depositId;
        Source = source;
        SourceReferenceId = sourceReferenceId;
        Amount = amount;
        Description = description;
    }

    public Guid DepositId { get; private set; }

    public DepositLineSource Source { get; private set; }

    public Guid? SourceReferenceId { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }
}
