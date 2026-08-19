// <copyright file="ReportDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record CompanySetupReportDto(
    CompanyDto Company,
    IReadOnlyList<FiscalYearDto> FiscalYears,
    IReadOnlyList<SegmentTypeDto> SegmentTypes,
    IReadOnlyList<CurrencyDto> Currencies,
    IReadOnlyList<NumberSequenceDto> NumberSequences,
    IReadOnlyList<UserDto> Users,
    IReadOnlyList<RoleDto> Roles,
    DateTimeOffset GeneratedOn);

public record ChartOfAccountsReportDto(
    Guid CompanyId,
    string CompanyName,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<SegmentTypeDto> SegmentTypes,
    DateTimeOffset GeneratedOn);

public record FiscalCalendarReportDto(
    Guid CompanyId,
    string CompanyName,
    IReadOnlyList<FiscalYearWithPeriodsDto> FiscalYears,
    DateTimeOffset GeneratedOn);

public record FiscalYearWithPeriodsDto(
    FiscalYearDto FiscalYear,
    IReadOnlyList<FiscalPeriodDto> Periods);

public record SecurityMatrixReportDto(
    Guid CompanyId,
    string CompanyName,
    IReadOnlyList<RoleDto> Roles,
    IReadOnlyList<UserDto> Users,
    IReadOnlyList<PermissionDto> Permissions,
    IReadOnlyList<RolePermissionDto> RolePermissions,
    IReadOnlyList<UserRoleDto> UserRoles,
    DateTimeOffset GeneratedOn);

public record AuditTrailReportDto(
    Guid CompanyId,
    string CompanyName,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<AuditLogDto> Entries,
    DateTimeOffset GeneratedOn);