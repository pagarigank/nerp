// <copyright file="ExchangeRate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class ExchangeRate : AuditableAggregateRoot
{
    protected ExchangeRate() { }

    public ExchangeRate(
        Guid companyId,
        string fromCurrency,
        string toCurrency,
        decimal rate,
        DateTimeOffset effectiveDate) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        FromCurrency = fromCurrency?.ToUpperInvariant().Trim() ?? throw new ArgumentNullException(nameof(fromCurrency));
        ToCurrency = toCurrency?.ToUpperInvariant().Trim() ?? throw new ArgumentNullException(nameof(toCurrency));
        Rate = rate;
        EffectiveDate = effectiveDate;
    }

    public Guid CompanyId { get; private set; }

    public string FromCurrency { get; private set; } = string.Empty;

    public string ToCurrency { get; private set; } = string.Empty;

    public decimal Rate { get; private set; }

    public DateTimeOffset EffectiveDate { get; private set; }

    public void Update(decimal rate, DateTimeOffset effectiveDate)
    {
        Rate = rate;
        EffectiveDate = effectiveDate;
    }
}
