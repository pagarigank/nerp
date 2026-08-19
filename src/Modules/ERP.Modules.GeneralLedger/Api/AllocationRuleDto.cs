// <copyright file="AllocationRuleDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Api;

public record AllocationRuleDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Description,
    Guid SourceAccountId,
    AllocationMethod Method,
    bool IsActive,
    IReadOnlyList<AllocationRuleLineDto> Lines);

public record AllocationRuleLineDto(
    Guid Id,
    Guid TargetAccountId,
    decimal Percentage,
    decimal? FixedAmount,
    string? Reference);

public record CreateAllocationRuleRequest(
    Guid CompanyId,
    string Name,
    string Description,
    Guid SourceAccountId,
    AllocationMethod Method,
    bool IsActive);

public record AddAllocationRuleLineRequest(
    Guid TargetAccountId,
    decimal Percentage,
    decimal? FixedAmount,
    string? Reference);

public record ExecuteAllocationRequest(
    string BatchNumber,
    decimal SourceAmount,
    Guid FiscalPeriodId,
    DateTimeOffset PostingDate);
