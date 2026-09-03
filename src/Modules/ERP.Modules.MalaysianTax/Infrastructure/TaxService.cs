// <copyright file="TaxService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Tax service implementation for Malaysian SST compliance.
/// Determines tax codes and rates based on item classification and customer registration status.
/// </summary>
public class TaxService : ITaxService
{
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly ITaxCodeRepository _taxCodeRepository;
    private readonly ILogger<TaxService> _logger;

    public TaxService(
        ITaxRateRepository taxRateRepository,
        ITaxCodeRepository taxCodeRepository,
        ILogger<TaxService> logger)
    {
        _taxRateRepository = taxRateRepository;
        _taxCodeRepository = taxCodeRepository;
        _logger = logger;
    }

    public async Task<TaxDeterminationResult> DetermineTaxAsync(
        Guid itemId,
        Guid customerId,
        decimal amount,
        Guid companyId,
        CancellationToken ct = default)
    {
        // TODO: Get item classification and customer SST registration status from ERP databases
        // This is a simplified implementation - in production, this would:
        // 1. Look up item to get default classification code
        // 2. Check if customer is SST registered
        // 3. Apply place-of-supply rules
        // 4. Determine appropriate tax code

        try
        {
            // For now, use a default SST-SALES-10 code for demonstration
            var defaultTaxCode = await _taxCodeRepository.GetByCodeAsync("SST-SALES-10", ct);
            
            if (defaultTaxCode == null)
            {
                return new TaxDeterminationResult
                {
                    Success = false,
                    ErrorMessage = "Default tax code 'SST-SALES-10' not configured. Please set up tax codes."
                };
            }

            var activeRate = await _taxRateRepository.GetActiveRateByCodeAsync(defaultTaxCode.Id, ct: ct);
            
            if (activeRate == null)
            {
                return new TaxDeterminationResult
                {
                    Success = false,
                    ErrorMessage = $"No active tax rate found for code {defaultTaxCode.Code}"
                };
            }

            var taxAmount = Math.Round(amount * (activeRate.RatePercent / 100), 2, MidpointRounding.AwayFromZero);

            _logger.LogInformation("Tax determined: Code={Code}, Rate={Rate}%, Amount={Amount}, Tax={Tax}",
                defaultTaxCode.Code, activeRate.RatePercent, amount, taxAmount);

            return new TaxDeterminationResult
            {
                Success = true,
                TaxCodeId = defaultTaxCode.Id,
                TaxCode = defaultTaxCode.Code,
                LhdntTaxType = defaultTaxCode.LhdntTaxType,
                RatePercent = activeRate.RatePercent,
                TaxAmount = taxAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining tax for item {ItemId}, customer {CustomerId}", itemId, customerId);
            return new TaxDeterminationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}