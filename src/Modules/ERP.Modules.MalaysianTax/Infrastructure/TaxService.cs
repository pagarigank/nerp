// <copyright file="TaxService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Tax service implementation for Malaysian SST compliance.
/// Determines tax codes and rates using configurable TaxDeterminationRules.
/// Rules are matched most-specific-first: exact matches beat wildcards.
/// </summary>
public class TaxService : ITaxService
{
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly ITaxCodeRepository _taxCodeRepository;
    private readonly ITaxDeterminationRuleRepository _ruleRepository;
    private readonly ILogger<TaxService> _logger;

    public TaxService(
        ITaxRateRepository taxRateRepository,
        ITaxCodeRepository taxCodeRepository,
        ITaxDeterminationRuleRepository ruleRepository,
        ILogger<TaxService> logger)
    {
        _taxRateRepository = taxRateRepository;
        _taxCodeRepository = taxCodeRepository;
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public TaxCalculationResult CalculateTax(
        decimal amount,
        decimal ratePercent,
        PricingMode pricingMode)
    {
        // RMCD rounding: round to 2 decimal places, half-up
        if (ratePercent == 0)
        {
            return new TaxCalculationResult
            {
                NetAmount = amount,
                TaxAmount = 0m,
                GrossAmount = amount,
                RatePercent = 0m,
            };
        }

        if (pricingMode == PricingMode.Inclusive)
        {
            // amount is the gross (tax-inclusive) price
            // net = amount / (1 + rate/100)
            // tax = amount - net
            var divisor = 1 + (ratePercent / 100);
            var netAmount = Math.Round(amount / divisor, 2, MidpointRounding.AwayFromZero);
            var taxAmount = Math.Round(amount - netAmount, 2, MidpointRounding.AwayFromZero);

            return new TaxCalculationResult
            {
                NetAmount = netAmount,
                TaxAmount = taxAmount,
                GrossAmount = amount,
                RatePercent = ratePercent,
            };
        }

        // Exclusive: amount is the net (tax-exclusive) price
        // tax = amount * rate/100
        // gross = amount + tax
        var tax = Math.Round(amount * (ratePercent / 100), 2, MidpointRounding.AwayFromZero);
        var gross = Math.Round(amount + tax, 2, MidpointRounding.AwayFromZero);

        return new TaxCalculationResult
        {
            NetAmount = amount,
            TaxAmount = tax,
            GrossAmount = gross,
            RatePercent = ratePercent,
        };
    }

    /// <inheritdoc/>
    public async Task<TaxDeterminationResult> DetermineTaxAsync(
        Guid itemId,
        Guid customerId,
        decimal amount,
        Guid companyId,
        PricingMode pricingMode = PricingMode.Exclusive,
        CancellationToken ct = default)
    {
        try
        {
            // Step 1: Find matching rules for this context
            // The caller should supply itemCategory, customerType, placeOfSupply
            // from the ERP's item/customer master data. For backward compatibility
            // with the existing interface (which only passes IDs), we default to
            // wildcard lookups — the real integration will resolve these IDs to
            // categories before calling DetermineTaxAsync.
            var rules = await _ruleRepository.FindMatchingRulesAsync(
                companyId,
                itemCategory: "*", // TODO: resolve from itemId via Item master
                customerType: "*", // TODO: resolve from customerId via Customer master
                placeOfSupply: "DOMESTIC",
                asOfDate: DateTime.UtcNow,
                ct: ct);

            var ruleList = rules.ToList();

            TaxDeterminationRule matchedRule;
            if (ruleList.Count > 0)
            {
                matchedRule = ruleList[0];
                _logger.LogDebug(
                    "Tax rule matched: RuleId={RuleId}, Priority={Priority}, Specificity={Specificity}, " +
                    "Item={Item}, Customer={Customer}, Place={Place} → TaxCodeId={TaxCodeId}",
                    matchedRule.Id, matchedRule.Priority, matchedRule.SpecificityScore(),
                    matchedRule.ItemCategory, matchedRule.CustomerType, matchedRule.PlaceOfSupply,
                    matchedRule.TaxCodeId);
            }
            else
            {
                // No rules found — fall back to system default SST-SALES-10
                _logger.LogWarning(
                    "No tax determination rule found for CompanyId={CompanyId}. Falling back to default SST-SALES-10.",
                    companyId);

                var fallbackCode = await _taxCodeRepository.GetByCodeAsync("SST-SALES-10", ct);
                if (fallbackCode == null)
                {
                    return new TaxDeterminationResult
                    {
                        Success = false,
                        ErrorMessage = "No tax determination rules configured and default tax code 'SST-SALES-10' not found."
                    };
                }

                var fallbackRate = await _taxRateRepository.GetActiveRateByCodeAsync(fallbackCode.Id, ct: ct);
                if (fallbackRate == null)
                {
                    return new TaxDeterminationResult
                    {
                        Success = false,
                        ErrorMessage = $"No active tax rate found for fallback code {fallbackCode.Code}"
                    };
                }

                var fallbackCalc = CalculateTax(amount, fallbackRate.RatePercent, pricingMode);

                return new TaxDeterminationResult
                {
                    Success = true,
                    TaxCodeId = fallbackCode.Id,
                    TaxCode = fallbackCode.Code,
                    LhdntTaxType = fallbackCode.LhdntTaxType,
                    RatePercent = fallbackRate.RatePercent,
                    TaxAmount = fallbackCalc.TaxAmount,
                    NetAmount = fallbackCalc.NetAmount,
                    GrossAmount = fallbackCalc.GrossAmount,
                };
            }

            // Step 2: Look up the tax code for the matched rule
            var taxCode = await _taxCodeRepository.GetByIdAsync(matchedRule.TaxCodeId, ct);
            if (taxCode == null)
            {
                return new TaxDeterminationResult
                {
                    Success = false,
                    ErrorMessage = $"Tax code ID {matchedRule.TaxCodeId} referenced by rule {matchedRule.Id} not found."
                };
            }

            // Step 3: Get the active rate for this tax code
            var activeRate = await _taxRateRepository.GetActiveRateByCodeAsync(taxCode.Id, ct: ct);
            if (activeRate == null)
            {
                return new TaxDeterminationResult
                {
                    Success = false,
                    ErrorMessage = $"No active tax rate found for code {taxCode.Code} (rule {matchedRule.Id})."
                };
            }

            // Step 4: Calculate tax amount using pricing mode
            var calc = CalculateTax(amount, activeRate.RatePercent, pricingMode);

            _logger.LogInformation(
                "Tax determined: Code={Code}, Rate={Rate}%, Mode={Mode}, " +
                "Amount={Amount}, Net={Net}, Tax={Tax}, Gross={Gross}, RuleId={RuleId}",
                taxCode.Code, activeRate.RatePercent, pricingMode, amount,
                calc.NetAmount, calc.TaxAmount, calc.GrossAmount, matchedRule.Id);

            return new TaxDeterminationResult
            {
                Success = true,
                TaxCodeId = taxCode.Id,
                TaxCode = taxCode.Code,
                LhdntTaxType = taxCode.LhdntTaxType,
                RatePercent = activeRate.RatePercent,
                TaxAmount = calc.TaxAmount,
                NetAmount = calc.NetAmount,
                GrossAmount = calc.GrossAmount,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining tax for item {ItemId}, customer {CustomerId}, company {CompanyId}",
                itemId, customerId, companyId);
            return new TaxDeterminationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}