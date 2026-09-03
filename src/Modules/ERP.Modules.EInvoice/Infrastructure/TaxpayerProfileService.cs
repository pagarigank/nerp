// <copyright file="TaxpayerProfileService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Service for managing company taxpayer profiles for e-Invoice (MyInvois) compliance.
/// Handles CRUD operations and TIN validation against the MyInvois API.
/// </summary>
public class TaxpayerProfileService : ITaxpayerProfileService
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly IMyInvoisApiClient _myInvoisClient;
    private readonly ILogger<TaxpayerProfileService> _logger;

    public TaxpayerProfileService(
        EInvoiceDbContext dbContext,
        IMyInvoisApiClient myInvoisClient,
        ILogger<TaxpayerProfileService> logger)
    {
        _dbContext = dbContext;
        _myInvoisClient = myInvoisClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TaxpayerProfileDto?> GetByCompanyIdAsync(Guid companyId)
    {
        var profile = await _dbContext.TaxpayerProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == companyId);

        if (profile == null)
            return null;

        return new TaxpayerProfileDto
        {
            Id = profile.Id,
            CompanyId = profile.CompanyId,
            TIN = profile.TaxIdentificationNumber,
            BRN = profile.BusinessRegistrationNumber ?? string.Empty,
            SalesTaxNumber = profile.SalesTaxNumber,
            ServiceTaxNumber = profile.ServiceTaxNumber,
            MSICCode = profile.MsicCode,
            Address = profile.Address,
            City = profile.City,
            State = profile.State,
            PostCode = profile.PostCode,
            CreatedAt = profile.CreatedAt.UtcDateTime,
            UpdatedAt = profile.UpdatedAt?.UtcDateTime ?? profile.CreatedAt.UtcDateTime
        };
    }

    /// <inheritdoc/>
    public async Task<OperationResult<TaxpayerProfileDto>> UpdateAsync(TaxpayerProfileUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TIN))
        {
            return new OperationResult<TaxpayerProfileDto>
            {
                IsSuccess = false,
                ErrorMessage = "TIN (Tax Identification Number) is required."
            };
        }

        if (string.IsNullOrWhiteSpace(request.BRN))
        {
            return new OperationResult<TaxpayerProfileDto>
            {
                IsSuccess = false,
                ErrorMessage = "BRN (Business Registration Number) is required."
            };
        }

        var existing = await _dbContext.TaxpayerProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId);

        if (existing == null)
        {
            // Create new profile
            var newProfile = new TaxpayerProfile(request.CompanyId, request.TIN, request.BRN);
            newProfile.UpdateProfile(
                request.TIN,
                request.BRN,
                request.SalesTaxNumber,
                request.ServiceTaxNumber,
                request.MSICCode,
                request.Address,
                request.City,
                request.State,
                request.PostCode);

            _dbContext.TaxpayerProfiles.Add(newProfile);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Taxpayer profile created: CompanyId={CompanyId}, TIN={TIN}", request.CompanyId, request.TIN);

            return new OperationResult<TaxpayerProfileDto>
            {
                IsSuccess = true,
                Value = MapToDto(newProfile)
            };
        }

        // Update existing profile
        existing.UpdateProfile(
            request.TIN,
            request.BRN,
            request.SalesTaxNumber,
            request.ServiceTaxNumber,
            request.MSICCode,
            request.Address,
            request.City,
            request.State,
            request.PostCode);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Taxpayer profile updated: CompanyId={CompanyId}, TIN={TIN}", request.CompanyId, request.TIN);

        return new OperationResult<TaxpayerProfileDto>
        {
            IsSuccess = true,
            Value = MapToDto(existing)
        };
    }

    /// <inheritdoc/>
    public async Task<TinValidationResult> ValidateTinAsync(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
        {
            return new TinValidationResult
            {
                TIN = tin,
                IsValid = false,
                Success = false,
                ErrorMessage = "TIN cannot be empty."
            };
        }

        try
        {
            // Delegate to MyInvois API for TIN validation
            var authRequest = new AuthenticationRequest(ClientId: string.Empty, ClientSecret: string.Empty, Environment: "production");

            var authResponse = await _myInvoisClient.AuthenticateAsync(authRequest);
            var response = await _myInvoisClient.ValidateTaxpayerTinAsync(tin, authResponse.AccessToken);

            return new TinValidationResult
            {
                TIN = tin,
                IsValid = response.IsValid,
                EntityName = response.EntityName ?? string.Empty,
                BusinessDescription = response.BusinessName ?? string.Empty,
                IsAgent = false, // MyInvois API doesn't directly expose this
                Success = true,
                ValidationDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TIN validation failed for TIN: {TIN}", tin);
            return new TinValidationResult
            {
                TIN = tin,
                IsValid = false,
                Success = false,
                ErrorMessage = $"TIN validation failed: {ex.Message}"
            };
        }
    }

    private static TaxpayerProfileDto MapToDto(TaxpayerProfile entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        TIN = entity.TaxIdentificationNumber,
        BRN = entity.BusinessRegistrationNumber ?? string.Empty,
        SalesTaxNumber = entity.SalesTaxNumber,
        ServiceTaxNumber = entity.ServiceTaxNumber,
        MSICCode = entity.MsicCode,
        Address = entity.Address,
        City = entity.City,
        State = entity.State,
        PostCode = entity.PostCode,
        CreatedAt = entity.CreatedAt.UtcDateTime,
        UpdatedAt = entity.UpdatedAt?.UtcDateTime ?? entity.CreatedAt.UtcDateTime
    };
}
