// <copyright file="SegmentValueDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record SegmentValueDto(
    Guid Id,
    Guid SegmentTypeId,
    Guid CompanyId,
    string Value,
    string Description,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateSegmentValueRequest(
    Guid SegmentTypeId,
    Guid CompanyId,
    string Value,
    string Description,
    int DisplayOrder);

public record UpdateSegmentValueRequest(
    string Value,
    string Description,
    int DisplayOrder);
