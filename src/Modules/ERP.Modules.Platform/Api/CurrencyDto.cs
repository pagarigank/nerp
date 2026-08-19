// <copyright file="CurrencyDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateCurrencyRequest(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces);

public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    int DecimalPlaces);
