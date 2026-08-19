// <copyright file="BudgetDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Api;

public record BudgetDto(
    Guid Id,
    Guid CompanyId,
    Guid FiscalYearId,
    string Name,
    string Description,
    BudgetType BudgetType,
    bool IsActive,
    decimal TotalAmount,
    IReadOnlyList<BudgetLineDto> Lines);

public record BudgetLineDto(
    Guid Id,
    Guid AccountId,
    int PeriodNumber,
    decimal Amount,
    Guid? ProjectId);

public record CreateBudgetRequest(
    Guid CompanyId,
    Guid FiscalYearId,
    string Name,
    string Description,
    BudgetType BudgetType);

public record AddBudgetLineRequest(
    Guid AccountId,
    int PeriodNumber,
    decimal Amount,
    Guid? ProjectId);
