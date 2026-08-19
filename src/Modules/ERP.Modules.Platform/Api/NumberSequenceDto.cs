// <copyright file="NumberSequenceDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record NumberSequenceDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Prefix,
    int NextValue,
    int Increment,
    int MinValue,
    int MaxValue,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateNumberSequenceRequest(
    Guid CompanyId,
    string Name,
    string Prefix,
    int NextValue,
    int Increment,
    int MinValue,
    int MaxValue);

public record UpdateNumberSequenceRequest(
    string Name,
    string Prefix,
    int Increment,
    int MinValue,
    int MaxValue);
