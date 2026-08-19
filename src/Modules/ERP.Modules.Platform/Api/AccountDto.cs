// <copyright file="AccountDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Api;

public record AccountDto(
    Guid Id,
    Guid CompanyId,
    string AccountNumber,
    string Description,
    AccountType AccountType,
    NormalBalance NormalBalance,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateAccountRequest(
    Guid CompanyId,
    string AccountNumber,
    string Description,
    AccountType AccountType,
    NormalBalance NormalBalance,
    bool IsActive);

public record UpdateAccountRequest(
    string Description,
    AccountType AccountType,
    NormalBalance NormalBalance,
    bool IsActive);
