// <copyright file="RecurringTemplateDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Api;

public record RecurringTemplateDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Description,
    RecurringFrequency Frequency,
    DateTimeOffset NextRunDate,
    DateTimeOffset? LastRunDate,
    bool IsActive,
    IReadOnlyList<RecurringTemplateLineDto> Lines);

public record RecurringTemplateLineDto(
    Guid Id,
    Guid AccountId,
    decimal FixedDebit,
    decimal FixedCredit,
    decimal? VariablePct,
    string? Reference);

public record CreateRecurringTemplateRequest(
    Guid CompanyId,
    string Name,
    string Description,
    RecurringFrequency Frequency,
    DateTimeOffset NextRunDate,
    bool IsActive);

public record AddRecurringTemplateLineRequest(
    Guid AccountId,
    decimal? FixedDebit,
    decimal? FixedCredit,
    decimal? VariablePct,
    string? Reference);
