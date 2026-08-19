// <copyright file="JournalEntryLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class JournalEntryLine : Entity
{
    protected JournalEntryLine() { }

    internal JournalEntryLine(
        Guid journalBatchId,
        Guid accountId,
        decimal debit,
        decimal credit,
        string? reference,
        string? segmentsJson,
        Guid? currencyId = null,
        decimal? foreignDebit = null,
        decimal? foreignCredit = null,
        decimal exchangeRate = 1.0m) : base(Guid.NewGuid())
    {
        JournalBatchId = journalBatchId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Reference = reference;
        SegmentsJson = segmentsJson;
        CurrencyId = currencyId;
        ForeignDebit = foreignDebit;
        ForeignCredit = foreignCredit;
        ExchangeRate = exchangeRate;
    }

    public Guid JournalBatchId { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal Debit { get; private set; }

    public decimal Credit { get; private set; }

    public string? Reference { get; private set; }

    public string? SegmentsJson { get; private set; }

    public Guid? CurrencyId { get; private set; }

    public decimal? ForeignDebit { get; private set; }

    public decimal? ForeignCredit { get; private set; }

    public decimal ExchangeRate { get; private set; } = 1.0m;

    public bool IsForeignCurrency => CurrencyId.HasValue;

    public JournalBatch? JournalBatch { get; set; }

    public ERP.Modules.Platform.Domain.Entities.Account? Account { get; set; }

    public void UpdateForeignAmounts(decimal? foreignDebit, decimal? foreignCredit, decimal exchangeRate)
    {
        ForeignDebit = foreignDebit;
        ForeignCredit = foreignCredit;
        ExchangeRate = exchangeRate;
    }
}
