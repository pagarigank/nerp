// <copyright file="ITaxpayerProfileService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;

namespace ERP.Modules.EInvoice.Application;

public interface ITaxpayerProfileService
{
    Task<TaxpayerProfileDto?> GetByCompanyIdAsync(Guid companyId);
    Task<OperationResult<TaxpayerProfileDto>> UpdateAsync(TaxpayerProfileUpdateRequest request);
    Task<TinValidationResult> ValidateTinAsync(string tin);
}

public class TaxpayerProfileDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TIN { get; set; } = string.Empty;
    public string BRN { get; set; } = string.Empty;
    public string? SalesTaxNumber { get; set; }
    public string? ServiceTaxNumber { get; set; }
    public string? MSICCode { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TaxpayerProfileUpdateRequest
{
    public Guid CompanyId { get; set; }
    public string TIN { get; set; } = string.Empty;
    public string BRN { get; set; } = string.Empty;
    public string? SalesTaxNumber { get; set; }
    public string? ServiceTaxNumber { get; set; }
    public string? MSICCode { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
}

public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public T? Value { get; set; }
}

public class TinValidationResult
{
    public string TIN { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string BusinessDescription { get; set; } = string.Empty;
    public bool IsAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ValidationDate { get; set; }
}