// <copyright file="ExchangeRateDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record ExchangeRateDto(
    Guid Id,
    Guid CompanyId,
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTimeOffset EffectiveDate,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateExchangeRateRequest(
    Guid CompanyId,
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTimeOffset EffectiveDate);

public record UpdateExchangeRateRequest(
    decimal Rate,
    DateTimeOffset EffectiveDate);
