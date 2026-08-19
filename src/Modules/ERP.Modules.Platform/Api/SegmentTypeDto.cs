// <copyright file="SegmentTypeDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record SegmentTypeDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Code,
    int DisplayOrder,
    bool IsRequired,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateSegmentTypeRequest(
    Guid CompanyId,
    string Name,
    string Code,
    int DisplayOrder,
    bool IsRequired);

public record UpdateSegmentTypeRequest(
    string Name,
    string Code,
    int DisplayOrder,
    bool IsRequired);
