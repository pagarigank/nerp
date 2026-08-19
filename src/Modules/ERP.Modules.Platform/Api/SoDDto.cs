// <copyright file="SoDDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record SoDRuleDto(
    Guid Id,
    string Module,
    string ActionA,
    string ActionB,
    string Description,
    string? DocumentType,
    bool IsActive,
    decimal? ThresholdAmount,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record SoDConflictDto(
    Guid Id,
    Guid RuleId,
    string UserId,
    string Module,
    string DocumentType,
    Guid DocumentId,
    string ConflictType,
    DateTimeOffset DetectedOn,
    bool Resolved,
    string? Resolution,
    string? ResolvedBy,
    DateTimeOffset? ResolvedOn);

public record CreateSoDRuleRequest(
    string Module,
    string ActionA,
    string ActionB,
    string Description,
    string? DocumentType,
    decimal? ThresholdAmount);

public record UpdateSoDRuleRequest(
    string Module,
    string ActionA,
    string ActionB,
    string Description,
    string? DocumentType,
    decimal? ThresholdAmount);

public record ResolveConflictRequest(
    string Resolution,
    string ResolvedBy);

public record CheckConflictRequest(
    string Module,
    string DocumentType,
    string UserId,
    string Action,
    decimal Amount = 0);
