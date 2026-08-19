// <copyright file="Statement.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

public class Statement : AuditableAggregateRoot
{
    protected Statement() { }

    public Statement(
        Guid companyId,
        Guid customerId,
        DateTimeOffset asOfDate,
        string statementNumber)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(statementNumber))
            throw new ArgumentException("Statement number is required.", nameof(statementNumber));

        CompanyId = companyId;
        CustomerId = customerId;
        AsOfDate = asOfDate;
        StatementNumber = statementNumber;
        Status = StatementStatus.Generated;
    }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTimeOffset AsOfDate { get; private set; }

    public string StatementNumber { get; private set; } = string.Empty;

    public StatementStatus Status { get; private set; }

    public void MarkDelivered() => Status = StatementStatus.Delivered;
}

public enum StatementStatus
{
    Generated = 0,
    Delivered = 1,
}
