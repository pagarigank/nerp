// <copyright file="CompanyDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Api;

public record CompanyDto(
    Guid Id,
    string Name,
    string LegalName,
    string BaseCurrency,
    string? TaxId,
    string? Address,
    Guid? ParentCompanyId,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateCompanyRequest(
    string Name,
    string LegalName,
    string BaseCurrency,
    string? TaxId,
    string? Address,
    Guid? ParentCompanyId);

public record UpdateCompanyRequest(
    string Name,
    string LegalName,
    string BaseCurrency,
    string? TaxId,
    string? Address,
    Guid? ParentCompanyId);
