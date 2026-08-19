// <copyright file="FiscalYearDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Api;

public record FiscalYearDto(
    Guid Id,
    Guid CompanyId,
    int Year,
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsClosed,
    FiscalCalendarType CalendarType,
    FiscalYearEndType YearEndType,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateFiscalYearRequest(
    Guid CompanyId,
    int Year,
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    FiscalCalendarType CalendarType = FiscalCalendarType.Standard,
    FiscalYearEndType YearEndType = FiscalYearEndType.Calendar);

public record UpdateFiscalYearRequest(
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    FiscalCalendarType CalendarType = FiscalCalendarType.Standard,
    FiscalYearEndType YearEndType = FiscalYearEndType.Calendar);
