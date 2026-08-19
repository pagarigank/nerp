// <copyright file="RecurringTemplateLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class RecurringTemplateLine : Entity
{
    protected RecurringTemplateLine() { }

    internal RecurringTemplateLine(
        Guid recurringTemplateId,
        Guid accountId,
        decimal fixedDebit,
        decimal fixedCredit,
        decimal? variablePct,
        string? reference) : base(Guid.NewGuid())
    {
        RecurringTemplateId = recurringTemplateId;
        AccountId = accountId;
        FixedDebit = fixedDebit;
        FixedCredit = fixedCredit;
        VariablePct = variablePct;
        Reference = reference;
    }

    public Guid RecurringTemplateId { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal FixedDebit { get; private set; }

    public decimal FixedCredit { get; private set; }

    public decimal? VariablePct { get; private set; }

    public string? Reference { get; private set; }
}
