// <copyright file="FiscalPeriodDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Api;

public record FiscalPeriodDto(
    Guid Id,
    Guid FiscalYearId,
    Guid CompanyId,
    int PeriodNumber,
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    PeriodStatus Status,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateFiscalPeriodRequest(
    Guid FiscalYearId,
    Guid CompanyId,
    int PeriodNumber,
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate);

public record PeriodClosePreviewDto(
    Guid PeriodId,
    int PeriodNumber,
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int PendingGlBatches,
    IReadOnlyList<string> Warnings);
